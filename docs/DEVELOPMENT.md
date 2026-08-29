# Local development

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Docker](https://www.docker.com/) (for the local PostgreSQL + pgvector container)
- A GitHub account and a personal access token (only needed once you want to
  actually import a repository or receive webhooks — everything else builds
  and tests without one)

Verify the SDK version:

```bash
dotnet --version   # should report a 10.x SDK
```

## 1. Clone and restore

```bash
git clone https://github.com/Asif-Nawaz27/github_issue_duplicate_detector.git
cd github_issue_duplicate_detector
dotnet restore
```

## 2. Start PostgreSQL

```bash
docker compose up -d
```

This starts `pgvector/pgvector:pg17` on `localhost:5433` (not the default
5432 — see the comment in `docker-compose.yml`; this avoids clashing with a
locally installed PostgreSQL service), with database `IssueSense`, user
`postgres`, password `admin`. These are local-only development defaults,
matched by the connection string already committed in
`App/Api/appsettings.json` — nothing to change here for local dev.

Confirm it's healthy:

```bash
docker compose ps
```

## 3. Apply database migrations

The API applies pending migrations automatically on startup when
`ASPNETCORE_ENVIRONMENT=Development` (the default from `launchSettings.json`),
so for most local development you can skip this step. To apply them
explicitly (e.g. before running integration tests manually, or in a
non-Development environment):

```bash
dotnet tool install --global dotnet-ef   # once, if you don't have it
dotnet ef database update --project Data/Infrastructure --startup-project App/Api
```

## 4. Configure GitHub credentials (secrets — never commit these)

`App/Api/appsettings.json` is committed with empty placeholders for
`GitHub:AccessToken` and `GitHub:WebhookSecret`. Set the real values with
[.NET User Secrets](https://learn.microsoft.com/aspnet/core/security/app-secrets),
which stores them outside the repo entirely:

```bash
cd App/Api
dotnet user-secrets set "GitHub:AccessToken" "<your-personal-access-token>"
dotnet user-secrets set "GitHub:WebhookSecret" "<a-secret-you-choose>"
cd ../..
```

See the README's [Configuring GitHub integration](../README.md#configuring-github-integration)
for what token scope you need and how to register the webhook.

If you only want to run unit tests or work on code that doesn't touch
GitHub or the database, you can skip steps 2–4 entirely.

## 5. Run the API

```bash
dotnet run --project App/Api
```

- HTTP: `http://localhost:5100`
- Scalar OpenAPI UI (Development only): `http://localhost:5100/scalar/v1`

A quick end-to-end smoke test once it's running (replace with a real repo
you have access to):

```bash
curl -X POST http://localhost:5100/api/repositories/{owner}/{repo}/import
curl -X POST http://localhost:5100/api/repositories/{owner}/{repo}/generate-embeddings
curl -X POST http://localhost:5100/api/repositories/{owner}/{repo}/check-duplicate \
  -H "Content-Type: application/json" \
  -d '{"title":"App crashes on startup","body":"Crashes immediately after launch"}'
```

## 6. (Optional) Run the web dashboard

[App/Web](../App/Web/README.md) is a small React + TypeScript app for
triggering the three API actions by hand and watching an activity feed. It's
a plain Node project, not part of the .NET solution:

```bash
cd App/Web
npm install
npm run dev
```

Opens on `http://localhost:5173` (or the next free port) and proxies `/api/*`
requests to the API on `http://localhost:5100` — no CORS setup needed. The
API from step 5 must already be running.

## Running tests

```bash
dotnet test
```

This runs four test projects:

| Project | What it covers | Needs Docker? |
|---|---|---|
| `Tests/Domain.Tests` | Entity/value-object invariants | No |
| `Tests/Application.Tests` | Use-case logic against in-memory fakes | No |
| `Tests/Infrastructure.Tests` | EF Core/pgvector persistence, GitHub client, embedding generation | Yes — spins up a real PostgreSQL via [Testcontainers](https://testcontainers.com/) automatically |
| `Tests/WebApi.Tests` | HTTP-level controller/webhook behavior via `WebApplicationFactory` | No |

`Infrastructure.Tests` needs Docker running (it starts its own disposable
Postgres container per test run — it does **not** use the `docker compose`
instance from step 2). If Docker isn't available, run the other three
projects individually:

```bash
dotnet test Tests/Domain.Tests
dotnet test Tests/Application.Tests
dotnet test Tests/WebApi.Tests
```

The first test run (and the first `dotnet run`) that touches embeddings
downloads the `all-MiniLM-L6-v2` ONNX model (~90 MB) into
`%LOCALAPPDATA%/IssueSense/embedding-models` (Linux/macOS:
`~/.local/share/IssueSense/embedding-models`). Subsequent runs use the
cached copy and need no network access.

## Building a single project

`dotnet build` with more than one project path fails
(`MSB1008: Only one project can be specified`) — build one project or the
whole solution:

```bash
dotnet build                                    # whole solution
dotnet build App/Api/IssueSense.Api.csproj      # one project
```

## Running the evaluation tool

To check duplicate-detection accuracy against the labeled dataset (no
database or GitHub credentials required — it only needs the embedding
model):

```bash
dotnet run --project Tools/Evaluation/IssueSense.Evaluation.csproj
```

See [Tools/Evaluation/README.md](../Tools/Evaluation/README.md) for details,
including why this dataset should never be tuned against.

## Adding a database migration

After changing an entity or an EF Core configuration in
`Data/Infrastructure/Persistence`:

```bash
dotnet ef migrations add <Name> --project Data/Infrastructure --startup-project App/Api
```

Review the generated migration before committing it — EF Core's diff isn't
always what you want, especially around index/column type changes involving
`vector` columns.

## Code style

- Nullable reference types, implicit usings, and analyzers
  (`AnalysisLevel=latest-recommended`) are enabled repo-wide via
  `Directory.Build.props` — don't add these settings to individual
  `.csproj` files.
- Naming conventions are enforced via `.editorconfig`.
- Logging uses source-generated `[LoggerMessage]` methods, not
  `ILogger.LogInformation(...)` calls directly (keeps CA1848 clean and
  avoids allocating when a log level is disabled).
- See [CONTRIBUTING.md](../CONTRIBUTING.md) for the full workflow and PR
  expectations.
