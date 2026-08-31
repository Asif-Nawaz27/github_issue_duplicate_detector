# IssueSense

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4.svg)](https://dotnet.microsoft.com/)

You know the drill: someone opens an issue titled "app dies on launch," and
somewhere three months back there's already a closed issue called
"application crashes immediately after starting." Same bug, zero words in
common that a keyword search would ever match on. Multiply that by a busy
repo and triage starts eating a real chunk of a maintainer's week.

IssueSense looks at what an issue actually *means*, not just the words in
it, and tells you when a new one is probably the same problem as one you've
already seen. It's something you run yourself, against your own repo — there's
no hosted version, no third party seeing your issues, and it never touches
GitHub except to read issues and, optionally, leave one comment.

> **Where this stands today:** early and still moving fast. The whole
> pipeline — import, embed, detect, suggest — works end to end and has real
> test coverage, but it hasn't been let loose on a big, messy, real-world
> repo yet. Read [Current limitations](#current-limitations) before you trust
> it with anything important. We'd rather you know exactly what you're
> getting than be surprised later.

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

Any repo that's been active for a while accumulates duplicates. The same
crash, the same missing feature, reported over and over in slightly
different words, because nobody's going to scroll through 400 closed issues
before opening a new one. Keyword search doesn't help much here — "crashes
on startup" and "won't launch, just dies" describe the exact same bug but
share almost no vocabulary.

IssueSense compares issues by meaning instead of exact wording, so those two
reports get recognized as likely the same thing even though a `grep` never
would. It doesn't act on that judgment by itself, though — it surfaces a
suggestion (as an API response, or as a comment on the new issue) and leaves
the actual "is this really a duplicate" call to a person. It will never
close or edit an issue on its own. That's a deliberate line we're not
planning to cross.

## How it works

1. **Import** — pull a repo's issues in over the GitHub REST API and store
   them locally: title, body, state, labels, timestamps.
2. **Embed** — turn each issue's title and body into a vector using a local
   sentence-embedding model. Issues that mean similar things end up with
   vectors that point in similar directions — that's the whole trick.
3. **Detect** — when a new issue shows up (via webhook, or a direct API
   call), embed it the same way and compare against every existing issue's
   vector using cosine similarity, computed inside PostgreSQL via pgvector.
   Whatever clears a configurable threshold comes back ranked, labeled
   **High confidence**, **Possible**, or dropped entirely if it's below the
   floor.
4. **Suggest** — if a webhook-triggered check turns up a high-confidence
   match, IssueSense posts one comment on the new issue linking to what it
   thinks is the original, and asks the reporter to confirm. That's the only
   write it ever makes.

Everything through "detect" never touches GitHub except to read. The one
optional comment in step 4 is the only exception, and only when you've
turned it on.

## Architecture

Clean Architecture, four projects, dependencies pointing one direction:

```
Data/Domain          Entities and value objects (Owner, Repository, Issue,
                      IssueEmbedding, DuplicateCandidate). No framework
                      dependencies — this layer doesn't even know EF Core
                      exists.
        ^
App/Application       Use cases and the ports they need (IGitHubService,
                      IEmbeddingService, IDuplicateDetectionService,
                      repository interfaces). Depends only on Domain.
        ^
Data/Infrastructure   Implements those ports: EF Core + pgvector for
                      persistence, a GitHub REST client, a local ONNX
                      embedding model, webhook signature verification.
        ^
App/Api               ASP.NET Core Web API — controllers, DTOs, DI wiring.
                      The only project that knows HTTP exists.
```

`Tools/Evaluation` is a separate console app for measuring detection
accuracy against a labeled dataset (see [its README](Tools/Evaluation/README.md));
`App/Web` is an optional React dashboard for poking at the API by hand
without curl (see [its README](App/Web/README.md)). Neither is part of the
running service — both just talk to it.

Want the deeper version — entity relationships, the request/webhook flows,
and the reasoning behind decisions like "why pgvector" and "why a local
embedding model"? That's all in
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
| Local dev | Docker Compose (Postgres + pgvector only — the API itself runs natively) |
| Web dashboard | React 19 + TypeScript, Vite (optional — see [App/Web](App/Web/README.md)) |

## Running it locally

This is the quick version. For the full walkthrough — including
troubleshooting and how to run the tests — see
**[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)**.

```bash
# 1. Start PostgreSQL (with pgvector)
docker compose up -d

# 2. Migrations apply automatically on first run in Development, or do it
#    yourself:
dotnet ef database update --project Data/Infrastructure

# 3. Set your GitHub credentials as user secrets (never commit these)
cd App/Api
dotnet user-secrets set "GitHub:AccessToken" "<your-token>"
dotnet user-secrets set "GitHub:WebhookSecret" "<your-webhook-secret>"
cd ../..

# 4. Run the API — the https profile listens on both :5100 and :7094 at
#    once, which the web dashboard below needs
dotnet run --project App/Api --launch-profile https
```

That's `http://localhost:5100` and `https://localhost:7094`, with the
Scalar OpenAPI UI at `http://localhost:5100/scalar/v1` while you're in
Development.

If you'd rather click around than write curl commands, there's a small
[web dashboard](App/Web/README.md) too — pick an owner from a searchable
dropdown (or add a new one right there), trigger import/embed/check-duplicate,
and watch a live activity feed as things happen:

```bash
cd App/Web
npm install
npm run dev
```

## Configuring GitHub integration

Two things, and that's it:

1. **A personal access token**, scoped to `repo` (classic) or, for a
   fine-grained token, read access to Issues and Metadata plus write access
   to Issues (only needed if you want the duplicate-warning comment).
   Generate one under **GitHub → Settings → Developer settings → Personal
   access tokens**. Set it with `dotnet user-secrets` locally, or an
   environment variable anywhere else — it should never end up in
   `appsettings.json`.
2. **A webhook**, if you want new issues checked automatically instead of on
   demand. On the target repo: **Settings → Webhooks → Add webhook**,
   payload URL `https://<your-host>/api/webhooks/github`, content type
   `application/json`, a secret you make up (matching `GitHub:WebhookSecret`),
   and subscribe to the **Issues** event.

No webhook yet? You can still check for duplicates on demand with
`POST /api/repositories/{owner}/{repository}/check-duplicate` — see the
[API overview](#api-overview) below.

## How duplicate detection works

- Each issue's `title` + `body` gets embedded into a 384-dimension vector by
  a small local sentence-transformer (`all-MiniLM-L6-v2`), run via ONNX
  Runtime, entirely on your own machine. No text ever goes to an external
  embedding API.
- Similarity is **cosine similarity** between two vectors, computed by
  PostgreSQL/pgvector (`vector_cosine_ops`, HNSW index) — not something our
  application code calculates by hand.
- Three thresholds turn a raw similarity number into a label you can act on:

  | Threshold | Default | Meaning |
  |---|---|---|
  | `MinimumSimilarityThreshold` | 0.50 | Below this, we don't even bother returning the candidate. |
  | `PossibleDuplicateThreshold` | 0.75 | At or above this: **Possible**. |
  | `HighConfidenceThreshold` | 0.90 | At or above this: **High confidence** — the only tier that ever triggers an automatic comment. |

- You always get a ranked list of candidates with their scores, never a bare
  yes/no. What to do with that is up to whoever's calling the API — or
  whoever reads the bot's comment.
- This is a similarity heuristic, not a promise. It's genuinely good at
  spotting paraphrases of something it's already seen; it'll miss
  duplicates that share no real semantic overlap in the text (think: a bug
  reported with a screenshot and three words of description), and it can
  flag issues that are related but actually different. That's why the
  thresholds are configurable — different repos have different appetites
  for false positives versus false negatives.
- Before you tune a threshold "by feel," there's a proper way to check your
  work: the [evaluation tool](Tools/Evaluation/README.md) measures accuracy
  against a labeled dataset so you're looking at real numbers.

## API overview

Full request/response schemas live in the Scalar UI (`/scalar/v1`) when
you're running in Development. The short version:

| Endpoint | Purpose |
|---|---|
| `POST /api/repositories/{owner}/{repository}/import` | Pull a repo's issues from GitHub into the local database. Do this once before duplicate detection has anything to compare against. |
| `POST /api/repositories/{owner}/{repository}/generate-embeddings` | Embed any imported issues that don't have a vector yet. |
| `POST /api/repositories/{owner}/{repository}/check-duplicate` | Check a candidate title/body against a repo's existing issues. Read-only, ranked candidates back. |
| `GET /api/repositories/{owner}` | List the repositories already imported for an owner (what powers the dashboard's autocomplete). |
| `POST /api/webhooks/github` | GitHub webhook receiver for the `issues` event — verifies the signature, runs the same duplicate check for newly-opened issues, comments on high-confidence matches. |
| `GET/POST/PUT/DELETE /api/owners` | Basic CRUD for the GitHub owners IssueSense knows about. New owners are also created automatically the first time you import a repo for them. |

## Current limitations

We'd rather tell you this up front than have you find out the hard way:

- **No background scheduler.** Import and embedding generation are things
  you trigger, not things that happen on a timer. A repo's data goes stale
  the moment new issues show up unless you re-import.
- **No authentication on the API itself.** This is meant to sit behind your
  own network boundary or a reverse proxy — it doesn't check who's calling
  it.
- **One embedding model, and it's not swappable at runtime.**
  `IEmbeddingService` is an abstraction specifically so a different
  model/provider *can* be swapped in later, but today that means writing a
  new implementation, not flipping a setting.
- **Not proven on a big, messy, real-world tracker yet.** The
  duplicate-detection logic has solid unit/integration coverage and a small
  labeled evaluation set (~30 cases — see
  [Tools/Evaluation](Tools/Evaluation/README.md)), but nobody's run it
  against a repo with thousands of issues and published the results. Don't
  assume real-world accuracy numbers that don't exist yet.
- **English-leaning model.** `all-MiniLM-L6-v2` was trained mostly on
  English text; how well it does on other languages is genuinely unknown.
- **The only write is that one comment**, and only for high-confidence
  matches on freshly-opened issues via webhook. No auto-labeling, no
  auto-closing, no auto-linking.

## Roadmap

Roughly the order we'd tackle these in — not a promise, not a timeline:

- [ ] A background job to keep imported issues and embeddings fresh
      automatically, instead of manual trigger endpoints.
- [ ] Actual API authentication for anything beyond local/behind-a-proxy use.
- [ ] Run the evaluation tool against a bigger, more varied dataset and
      publish what we find.
- [ ] GitHub App support as an alternative to a personal access token.
- [ ] Pluggable embedding providers without touching code.
- [ ] Optional auto-labeling of likely duplicates (still never auto-close).

If any of this sounds fun to work on, or you think something's missing from
the list, that's exactly what issues and PRs are for.

## Contributing

We'd genuinely love the help. **[CONTRIBUTING.md](CONTRIBUTING.md)** covers
the workflow, coding conventions, and how to open a PR that's easy to
review; **[docs/DEVELOPMENT.md](docs/DEVELOPMENT.md)** walks through the
full local setup, tests included.

Please give the **[Code of Conduct](CODE_OF_CONDUCT.md)** a read, and if you
find a security issue, report it the way **[SECURITY.md](SECURITY.md)**
describes rather than opening a public issue for it.

And hey — if this project is useful to you, a star helps other people find
it too. It also just makes our day.

## License

[MIT](LICENSE)
