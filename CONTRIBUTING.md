# Contributing to IssueSense

First off, thanks for even considering this. IssueSense is small and still
young, which is actually a great time to contribute — there's plenty of room
to shape it, and no backlog of stale conventions to untangle. Typo fixes,
new features, better tests, docs corrections — all of it counts.

## Before you dive in

- If you're planning something bigger than a small fix — a new feature, a
  behavior change, a refactor that crosses layers — open an issue first and
  describe what you're thinking. It's a five-minute conversation that can
  save you from building something that gets rethought in review.
- Take a quick look through open issues and PRs so you're not duplicating
  work someone's already halfway through. Slightly embarrassing for
  everyone involved otherwise.
- Everything you need for local setup lives in
  [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md). This document is about how we
  like to work, not how to get the project running.

## The actual workflow

1. Fork the repo, branch off `main`.
2. Make your change.
3. Run the test suite (`dotnet test` — see
   [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md#running-tests) for the details).
   All four test projects should be green; `Infrastructure.Tests` needs
   Docker running.
4. `dotnet build` shouldn't produce any new warnings.
5. Open a PR against `main` using the PR template, and explain *why* you
   made the change, not just what changed — the diff already tells us that
   part.

## How the architecture is organized (and why it matters for your PR)

This project follows Clean Architecture with one strict rule: dependencies
only point inward. `Domain <- Application <- Infrastructure`, and `Api`
depends on both `Application` and `Infrastructure`. In practice:

- **`Data/Domain`** can't reference any other project or third-party
  package — full stop. Entities validate their own rules; keep EF Core,
  JSON, HTTP, all of it, out of this layer entirely.
- **`App/Application`** defines the use cases and the interfaces
  (`IGitHubService`, `IEmbeddingService`, the repository interfaces, and so
  on) that Infrastructure will implement. It can lean on Domain, but never
  reaches into Infrastructure or Api.
- **`Data/Infrastructure`** is where those interfaces actually get
  implemented — EF Core, HTTP clients, ONNX inference, all of it lives here.
- **`App/Api`** is the only place that's allowed to know HTTP exists —
  controllers, status codes, request/response DTOs. It shouldn't contain
  business logic. If a controller action is doing more than calling an
  Application service and shaping the result for the response, that logic
  probably belongs a layer down.

If you find yourself wanting to break this direction — say, Domain needing
to call something in Infrastructure — that's usually a sign the abstraction
needs a rethink, not a reason to just add the reference. Raise it in an
issue and let's figure it out together.

## Writing code that fits in

- Match what's already there: nullable reference types on, no unused
  usings, standard C# naming (`.editorconfig` enforces this so you don't
  have to think about it too hard).
- If there's already a pattern for the kind of thing you're doing, use it.
  Domain entities use a private constructor plus a static `Create` factory
  that validates everything; logging uses source-generated
  `[LoggerMessage]` methods instead of calling `ILogger.LogX` directly.
  Reach for the existing pattern before inventing a new one.
- We keep abstractions to what's actually needed *today*. This codebase
  deliberately avoids speculative generality — please don't add a new
  interface, config flag, or extensibility point for a use case that
  doesn't exist yet. It's tempting, we get it, but it usually just adds
  weight nobody asked for.
- Skip comments that explain *what* the code does — good naming should
  handle that. Save comments for the non-obvious *why*: a workaround, a
  constraint, an invariant that isn't visible from the code alone.

## Tests

- New behavior needs coverage at the right layer: Domain invariants go in
  `Tests/Domain.Tests`, use-case logic against fakes goes in
  `Tests/Application.Tests`, real persistence/HTTP behavior goes in
  `Tests/Infrastructure.Tests`, and controller/webhook HTTP behavior goes in
  `Tests/WebApi.Tests`.
- Integration tests run against a real PostgreSQL via Testcontainers, not a
  mocked `DbContext` — please follow that same pattern for anything that
  touches persistence. We learned the hard way that mocked-DB tests can
  pass while the real thing breaks.
- Test names follow `MethodName_Scenario_ExpectedResult`. It reads like a
  sentence and makes failures easy to scan.

## Commit messages and PRs

- Explain *why* in the commit message, not just *what* — again, the diff
  already covers the what.
- Keep PRs focused on one thing. A refactor bundled in with an unrelated
  feature is harder to review and more likely to stall.
- A green `dotnet test` locally (and CI, once it's wired up) is expected
  before review.

## Found a bug or have an idea?

Use the issue templates — they'll prompt you for the details that make a
report actually actionable. If what you've found is a security
vulnerability, please don't open a public issue for it; see
[SECURITY.md](SECURITY.md) for how to report it privately instead.

## Code of Conduct

Everyone participating here is expected to follow our
[Code of Conduct](CODE_OF_CONDUCT.md). It's short, it's reasonable, and it's
really just "be a decent person" written out formally.
