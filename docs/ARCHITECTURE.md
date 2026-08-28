# Architecture

This document goes one level deeper than the README's overview: the project
layout, how the four layers depend on each other, the data model, and the
two main request flows (on-demand check, webhook-triggered check).

## Layers and dependency direction

```
Data/Domain  <---  App/Application  <---  Data/Infrastructure
                          ^
                          |
                       App/Api
```

Arrows point from "depends on" to "depended on". Nothing in an inner layer
references an outer one.

- **`Data/Domain`** (`IssueSense.Domain`) — entities (`Repository`, `Issue`,
  `IssueEmbedding`, `DuplicateCandidate`) and value objects
  (`EmbeddingVector`, `SimilarityScore`). Plain C#, no EF Core, no ASP.NET
  Core, no third-party packages. Entities validate their own invariants in
  factory methods (e.g. `Issue.Create` rejects an empty title).
- **`App/Application`** (`IssueSense.Application`) — use cases
  (`IssueImportService`, `EmbeddingGenerationService`,
  `DuplicateDetectionService`, `GitHubIssueWebhookHandler`) and the *ports*
  they depend on, expressed as interfaces: `IGitHubService`,
  `IEmbeddingService`, `IRepositoryRepository`/`IIssueRepository`/
  `IIssueEmbeddingRepository`/`IUnitOfWork`, `IDuplicateNotifier`. This
  layer defines *what* the system does and *what it needs*, not how those
  needs are met.
- **`Data/Infrastructure`** (`IssueSense.Infrastructure`) — implements every
  port from Application: `GitHubService` (REST client), `LocalEmbeddingService`
  (ONNX inference), EF Core `DbContext` + repositories (Postgres/pgvector),
  `GitHubWebhookSignatureVerifier`, `GitHubCommentDuplicateNotifier`. This is
  the only layer that knows about HTTP clients, SQL, or ONNX.
- **`App/Api`** — ASP.NET Core host. Controllers translate HTTP requests
  into Application calls and map results to DTOs (`App/Api/Contracts/...`).
  No business logic lives here.

This split means the duplicate-detection algorithm, the import pipeline, and
the webhook handling logic are all testable without a database, an HTTP
server, or a real GitHub account — see the fakes under `Tests/*.Tests`.

## Data model

Four entities, stored via EF Core in PostgreSQL:

| Entity | Key fields | Notes |
|---|---|---|
| `Repository` | `GitHubRepositoryId`, `Owner`, `Name`, `IsActive` | One row per imported GitHub repo. `FullName` = `Owner/Name`. |
| `Issue` | `RepositoryId`, `GitHubIssueId`, `GitHubIssueNumber`, `Title`, `Body`, `State`, `Labels`, `Url` | Belongs to a `Repository` by id (not a navigation property — they're separate aggregate roots, loaded independently). `GitHubIssueId` is globally unique. |
| `IssueEmbedding` | `IssueId`, `Vector` (`vector(384)`), `ModelName` | One embedding per issue. `ModelName` is stored alongside the vector so a future model change can't silently compare embeddings produced by different models. |
| `DuplicateCandidate` | (see `Data/Domain/Entities/DuplicateCandidate.cs`) | Represents a detected candidate relationship; primarily used to shape detection results, not as a durable audit log today. |

The `Vector` column uses [pgvector](https://github.com/pgvector/pgvector)'s
`vector(384)` type with an HNSW index and `vector_cosine_ops`, so nearest-
neighbor search runs inside PostgreSQL rather than by pulling every
embedding into application memory.

`EmbeddingVector` (a Domain value object, not a raw `float[]`) has no LINQ-
translatable `CosineDistance()` method through EF Core's value converter, so
similarity search is issued as a raw SQL query via `DbContext.Database
.SqlQuery<T>()` (see `IssueEmbeddingRepository`), not composed via LINQ.

## Request flow: on-demand check (`POST /check-duplicate`)

```
Client
  |
  v
RepositoriesController.CheckDuplicate
  |
  v
IDuplicateDetectionService.FindDuplicatesAsync(owner, repo, title, body)
  |-- IRepositoryRepository: look up the repository (404 if never imported)
  |-- IEmbeddingService: embed the candidate title+body
  |-- IIssueEmbeddingRepository.FindSimilarAsync: pgvector cosine search,
  |     bounded by TopN and MinimumSimilarityThreshold
  |
  v
Candidates classified (High confidence / Possible) and returned, ranked
```

Entirely read-only. Nothing is written to the database or to GitHub.

## Request flow: webhook-triggered check (`POST /api/webhooks/github`)

```
GitHub  --(issues: opened)-->  WebhooksController.ReceiveGitHubWebhook
  |
  |-- Read raw request body bytes (signature is verified against the exact
  |   bytes GitHub sent, before any JSON parsing)
  |-- IGitHubWebhookSignatureVerifier.IsValid: HMAC-SHA256 over the raw
  |   body using GitHub:WebhookSecret, compared with
  |   CryptographicOperations.FixedTimeEquals. Invalid/missing -> 401.
  |-- Only "issues" events with action "opened" proceed; everything else
  |   (pull requests, other actions, ping) is acknowledged and ignored.
  |
  v
GitHubIssueWebhookHandler.HandleAsync
  |-- Same duplicate-detection call as the on-demand path
  |-- If the strongest match is High confidence:
  |     IDuplicateNotifier -> GitHubCommentDuplicateNotifier posts one
  |     comment on the new issue (via IGitHubService.PostIssueCommentAsync),
  |     linking the likely original and asking the reporter to confirm
  |-- Idempotency: before posting, existing comments on the issue are
  |     checked for an embedded marker so a redelivered webhook (GitHub
  |     retries failed deliveries) never posts a second comment
  |
  v
200 OK { processed, reason, duplicatesFound }
```

The issue itself is never closed, labeled, or edited — the only GitHub
write in this whole flow is that one optional comment.

## Why these choices

- **pgvector over a separate vector database** — one fewer moving part to
  run and back up; Postgres already holds the relational data (issues,
  repositories), and pgvector's HNSW index is fast enough at the scale this
  project targets (a single repository's issue history, not a
  multi-tenant SaaS corpus).
- **A local ONNX embedding model over a hosted embeddings API** — no
  per-request cost, no external network dependency at inference time, no
  issue text leaving the machine running IssueSense. The trade-off is a
  smaller, less powerful model than the largest hosted options; the
  [evaluation tool](../Tools/Evaluation/README.md) exists to make that
  trade-off measurable instead of assumed.
- **Three thresholds instead of one cutoff** — "duplicate or not" is rarely
  binary in practice. Separating "don't bother returning this" from
  "possible, worth a look" from "confident enough to comment
  automatically" lets each be tuned independently for a repository's
  tolerance for false positives vs. false negatives.
- **Ports defined in Application, implemented in Infrastructure** — swapping
  the embedding model, the GitHub client, or the persistence layer never
  requires touching Domain or Application; only a new Infrastructure
  implementation and a DI registration change.
