# IssueSense

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/)

IssueSense finds GitHub issues that are probably duplicates of each other, using
semantic (meaning-based) similarity instead of exact text matching. It's a
self-hosted service you run against your own repository — there's no hosted
version and it doesn't touch your repo unless you explicitly wire it up.

> **Status:** early / actively developed. The core pipeline (import → embed →
> detect → suggest) works end-to-end and is covered by tests, but this hasn't
> run against a large real-world repository yet. See [Current limitations](#current-limitations).

## Table of contents

- [What problem this solves](#what-problem-this-solves)
- [How it works](#how-it-works)
- [Architecture](#architecture)
- [Technology stack](#technology-stack)
- [Running it locally](#running-it-locally)
- [Configuring GitHub integration](#configuring-github-integration)
- [How duplicate detection works](#how-duplicate-detection-works)
- [API overview](#api-overview)
- [Current limitations](#current-limitations)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)

## What problem this solves

Active repositories accumulate duplicate issues: the same bug reported with
different wording, the same feature requested in different words. Keyword
search misses most of these because reporters rarely use the same phrasing.
Maintainers end up doing this triage by memory, or not at all.

IssueSense compares issues by *meaning* rather than exact words, so "app
crashes on startup" and "application crashes right after launching" are
recognized as likely describing the same problem even though they share only
a couple of words. It surfaces likely duplicates as suggestions for a human
(or a bot comment) to confirm — it never closes or edits issues on its own.

## How it works

1. **Import** — issues from a GitHub repository are pulled in via the REST
   API and stored locally (title, body, state, labels, timestamps).
2. **Embed** — each issue's title and body are converted into a numeric
   vector (an "embedding") using a local sentence-embedding model. Issues
   with similar meaning end up with vectors that point in similar
   directions.
3. **Detect** — when a new issue comes in (via webhook, or a direct API
   call), its text is embedded the same way and compared against every
   existing issue's embedding using cosine similarity, via a vector index in
   PostgreSQL (pgvector). The closest matches above a configurable threshold
   are returned, each classified as **High confidence**, **Possible**, or
   below the minimum threshold (not returned at all).
4. **Suggest** — for a webhook-triggered check that clears the high-confidence
   threshold, IssueSense posts one comment on the new issue linking to the
   likely original and asking the reporter to confirm. It never closes,
   labels, or edits issues automatically.

Every step up to and including "detect" is read-only. The only write
IssueSense ever performs against GitHub is that one optional comment, and
only when explicitly enabled.

## Architecture

Clean Architecture with four projects, dependencies pointing inward:

```
Data/Domain          Entities and value objects (Repository, Issue, IssueEmbedding,
                      DuplicateCandidate). No framework dependencies.
        ^
App/Application       Use cases and ports (IGitHubService, IEmbeddingService,
                      IDuplicateDetectionService, repository interfaces). Depends
                      only on Domain.
        ^
Data/Infrastructure   Implements the Application ports: EF Core + pgvector
                      persistence, GitHub REST client, local ONNX embedding
                      model, webhook signature verification.
        ^
App/Api               ASP.NET Core Web API — controllers, DTOs, DI wiring.
                      The only project that knows about HTTP.
```

`Tools/Evaluation` is a separate console app for measuring detection accuracy
against a labeled dataset (see [its README](Tools/Evaluation/README.md)); it
depends on Application and Infrastructure but isn't part of the running
service.

For entity relationships, the request/webhook flows, and the reasoning
behind key decisions (why pgvector, why a local embedding model, why cosine
similarity thresholds instead of a fixed top-1 match), see
**[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**.

## Technology stack

| Concern | Choice |
|---|---|
| Runtime / language | .NET 10, C# 13 |
| API | ASP.NET Core Web API, OpenAPI via Scalar |
| Database | PostgreSQL 17 + [pgvector](https://github.com/pgvector/pgvector) (cosine similarity, HNSW index) |
| ORM | EF Core 10 (Npgsql provider) |
| Embeddings | [`all-MiniLM-L6-v2`](https://huggingface.co/Xenova/all-MiniLM-L6-v2) (384-dim), run locally via ONNX Runtime — no external API calls, no per-request cost |
| GitHub access | GitHub REST API v3 over `HttpClientFactory`, with resilience/retry via `Microsoft.Extensions.Http.Resilience` |
| Webhooks | HMAC-SHA256 signature verification (`X-Hub-Signature-256`) |
| Tests | xUnit; unit tests with in-memory fakes, integration tests against a real PostgreSQL via Testcontainers |
| Local dev | Docker Compose (Postgres + pgvector only — the API runs natively) |
| Web dashboard | React 19 + TypeScript, Vite (optional — see [App/Web](App/Web/README.md)) |

## Running it locally

Short version — see **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** for full
details, troubleshooting, and how to run tests.

```bash
# 1. Start PostgreSQL (with pgvector)
docker compose up -d

# 2. Apply database migrations happens automatically on first run in
#    Development, or manually:
dotnet ef database update --project Data/Infrastructure --startup-project App/Api

# 3. Set your GitHub credentials as user secrets (never commit these)
cd App/Api
dotnet user-secrets set "GitHub:AccessToken" "<your-token>"
dotnet user-secrets set "GitHub:WebhookSecret" "<your-webhook-secret>"
cd ../..

# 4. Run the API
dotnet run --project App/Api
```

The API listens on `http://localhost:5100` by default, with the Scalar
OpenAPI UI at `http://localhost:5100/scalar/v1` in Development.

Optionally, run the [web dashboard](App/Web/README.md) — a small React app
for triggering import/embed/check-duplicate by hand and watching an
activity feed as you go:

```bash
cd App/Web
npm install
npm run dev
```

## Configuring GitHub integration

IssueSense needs two things from GitHub:

1. **A personal access token** with `repo` scope (classic) or, for a
   fine-grained token, read access to Issues and Metadata plus write access
   to Issues (needed only to post the duplicate-warning comment). Generate
   one at **GitHub → Settings → Developer settings → Personal access
   tokens**. Set it via `dotnet user-secrets` (local dev) or an environment
   variable in any other environment — never in `appsettings.json`.
2. **A webhook**, if you want new issues detected automatically rather than
   checked on demand via the API. On the target repository: **Settings →
   Webhooks → Add webhook**, payload URL `https://<your-host>/api/webhooks/github`,
   content type `application/json`, a secret of your choosing (set the same
   value as `GitHub:WebhookSecret`), and subscribe to the **Issues** event.

Without a webhook configured, you can still check for duplicates on demand
via `POST /api/repositories/{owner}/{repository}/check-duplicate` — see
[API overview](#api-overview).

## How duplicate detection works

- Each issue's `title` + `body` is embedded into a 384-dimension vector by a
  small local sentence-transformer model (`all-MiniLM-L6-v2`), run via ONNX
  Runtime. The model runs entirely on your machine — no text is sent to any
  external embedding API.
- Similarity between two issues is **cosine similarity** between their
  vectors, computed by PostgreSQL/pgvector (`vector_cosine_ops`, HNSW
  index), not in application code.
- Three configurable thresholds (`DuplicateDetectionOptions`, defaults
  shown) turn a raw similarity score into a label:

  | Threshold | Default | Meaning |
  |---|---|---|
  | `MinimumSimilarityThreshold` | 0.50 | Below this, a candidate isn't returned at all. |
  | `PossibleDuplicateThreshold` | 0.75 | At or above this: classified **Possible**. |
  | `HighConfidenceThreshold` | 0.90 | At or above this: classified **High confidence** — the only tier that triggers an automatic GitHub comment. |

- Results are always a ranked list of candidates with their similarity
  score, not a single yes/no verdict — the caller (or the human reading the
  bot comment) decides what to do with it.
- This is a **similarity heuristic**, not a guarantee. It's good at finding
  paraphrases of a problem it has seen before; it will miss duplicates that
  share no real semantic overlap in their text (e.g. a duplicate reported
  with a screenshot and almost no description) and can flag genuinely
  related-but-different issues as candidates. Thresholds are configurable
  per repository's tolerance for false positives vs. false negatives.
- Accuracy at different thresholds can be measured against a labeled dataset
  using the [evaluation tool](Tools/Evaluation/README.md) — use it before
  trusting a threshold change, rather than tuning by feel.

## API overview

Full request/response schemas are in the Scalar UI (`/scalar/v1`) when
running in Development. Summary:

| Endpoint | Purpose |
|---|---|
| `POST /api/repositories/{owner}/{repository}/import` | Pull all issues from GitHub into the local database. Run once before duplicate detection is useful for a repository. |
| `POST /api/repositories/{owner}/{repository}/generate-embeddings` | Generate embeddings for imported issues that don't have one yet. |
| `POST /api/repositories/{owner}/{repository}/check-duplicate` | Check a candidate title/body against a repository's existing issues. Read-only; returns ranked candidates. |
| `POST /api/webhooks/github` | GitHub webhook receiver for the `issues` event. Verifies the HMAC signature, runs the same duplicate check for newly-opened issues, and posts a comment for high-confidence matches. |

## Current limitations

Stated plainly, so expectations are set correctly:

- **No background scheduler.** Import and embedding generation are
  triggered manually via API call; there's no polling or periodic sync yet.
  A repository's data goes stale unless you re-import it.
- **No authentication on the API itself.** These endpoints are meant to run
  behind your own network boundary or a reverse proxy — they don't
  currently check who's calling them.
- **One embedding model, not pluggable at runtime.** `IEmbeddingService` is
  an abstraction so a different model/provider can be swapped in, but doing
  so today means writing a new implementation, not flipping a config value.
- **Not validated against a large, real-world issue tracker.** The
  duplicate-detection logic is tested against unit/integration test cases
  and a small (~30-case) labeled evaluation dataset — see
  [Tools/Evaluation](Tools/Evaluation/README.md). It has not yet been run
  against a repository with thousands of issues, and no accuracy numbers
  from real-world usage are published (or should be assumed).
- **English-oriented model.** `all-MiniLM-L6-v2` was trained primarily on
  English text; similarity quality for other languages is unverified.
- **Comments are the only write action**, and only for high-confidence
  matches on newly-opened issues via webhook. There's no auto-labeling,
  auto-closing, or auto-linking.

## Roadmap

Roughly in order of what's most likely to be worked on next — not a
commitment or a timeline:

- [ ] Background job to keep imported issues and embeddings in sync
      automatically (rather than manual trigger endpoints).
- [ ] API authentication (API key or similar) for anything beyond local use.
- [ ] Run the evaluation tool against a larger, more diverse dataset and
      publish the results.
- [ ] GitHub App support as an alternative to a personal access token.
- [ ] Configurable/pluggable embedding providers without a code change.
- [ ] Optional auto-labeling of likely duplicates (still no auto-close).

Suggestions and PRs against this list are welcome — see below.

## Contributing

Contributions are welcome. See **[CONTRIBUTING.md](CONTRIBUTING.md)** for
the development workflow, coding conventions, and how to submit a PR, and
**[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** for the full local setup
including running tests.

Please also read the **[Code of Conduct](CODE_OF_CONDUCT.md)**, and report
security issues per **[SECURITY.md](SECURITY.md)** rather than as a public
issue.

## License

[MIT](LICENSE)
