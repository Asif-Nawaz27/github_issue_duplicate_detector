# Contributing to IssueSense

Thanks for considering a contribution. This project is small and early —
issues and PRs of all sizes are welcome, from typo fixes to new features.

## Before you start

- For anything beyond a small fix (a new feature, a behavior change, a
  refactor touching multiple layers), please open an issue first to discuss
  the approach. It saves rework on both sides.
- Check open issues and PRs so you're not duplicating in-progress work —
  which would be a bit embarrassing, given the project.
- Full local setup instructions live in
  [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md). This document covers workflow
  and expectations, not setup steps.

## Development workflow

1. Fork the repository and create a branch off `main`.
2. Make your change.
3. Run the full test suite (`dotnet test`) — see
   [docs/DEVELOPMENT.md](docs/DEVELOPMENT.md#running-tests). All four test
   projects should pass; `Infrastructure.Tests` needs Docker.
4. Make sure `dotnet build` produces no new warnings.
5. Open a pull request against `main` using the PR template. Describe *why*
   the change is needed, not just what changed — the diff already shows
   what changed.

## Architecture rules

This project follows Clean Architecture with a strict dependency direction:
`Domain <- Application <- Infrastructure`, with `Api` depending on
`Application` and `Infrastructure`. Concretely:

- **`Data/Domain`** must not reference any other project or any third-party
  package. Entities validate their own invariants; keep framework concerns
  (EF Core, JSON, HTTP) out of this layer entirely.
- **`App/Application`** defines use cases and the interfaces
  (`IGitHubService`, `IEmbeddingService`, repository interfaces, etc.) that
  Infrastructure implements. It may depend on Domain, but never on
  Infrastructure or Api.
- **`Data/Infrastructure`** implements Application's interfaces. This is
  where EF Core, HTTP clients, and ONNX inference live.
- **`App/Api`** is the only project allowed to know about HTTP concerns
  (controllers, status codes, request/response DTOs). It should not contain
  business logic — if a controller action is doing more than
  calling an Application service and mapping the result, that logic
  probably belongs one layer down.

If a change seems to require breaking this direction (e.g. Domain needing
to call something in Infrastructure), that's usually a sign the abstraction
needs rethinking, not a reason to add a reference — raise it in an issue.

## Coding conventions

- Match the existing style: nullable reference types on, no unused usings,
  standard C# naming conventions (enforced by `.editorconfig`).
- Prefer the same patterns already used nearby (e.g. private constructor +
  static `Create` factory with validation for Domain entities;
  `[LoggerMessage]` source-generated logging, not direct `ILogger.LogX`
  calls) over introducing a new pattern for the same problem.
- Keep abstractions to what's actually needed today. This codebase
  deliberately avoids speculative generality — don't add a new interface,
  config flag, or extensibility point for a use case that doesn't exist yet.
- No code comments explaining *what* code does (the code should be
  readable on its own) — comments are reserved for a non-obvious *why*
  (a workaround, a constraint, a subtle invariant).

## Tests

- New behavior needs test coverage at the appropriate layer: Domain
  invariants in `Tests/Domain.Tests`, use-case logic against fakes in
  `Tests/Application.Tests`, real persistence/HTTP behavior in
  `Tests/Infrastructure.Tests`, controller/webhook HTTP behavior in
  `Tests/WebApi.Tests`.
- Integration tests use a real PostgreSQL via Testcontainers, not a mocked
  database — please follow that pattern for anything touching persistence
  rather than mocking `DbContext`.
- Test method names follow `MethodName_Scenario_ExpectedResult`.

## Commit messages and PRs

- Write commit messages that explain *why*, not just *what* — the diff
  already shows what changed.
- Keep PRs focused. A large PR mixing an unrelated refactor with a feature
  is harder to review and more likely to get stuck.
- CI (when configured) and a green `dotnet test` locally are expected before
  review.

## Reporting bugs / requesting features

Use the issue templates. For security vulnerabilities, see
[SECURITY.md](SECURITY.md) instead of opening a public issue.

## Code of Conduct

This project follows the [Code of Conduct](CODE_OF_CONDUCT.md). By
participating, you're expected to uphold it.
