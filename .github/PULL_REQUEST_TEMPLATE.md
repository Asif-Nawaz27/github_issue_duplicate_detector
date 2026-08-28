## What and why

What does this change do, and why is it needed? (Link the issue it
addresses, if any: `Closes #123`.)

## How it was tested

- [ ] `dotnet test` passes locally (all four test projects, including
      `Infrastructure.Tests` if this touches persistence/GitHub/embeddings)
- [ ] `dotnet build` produces no new warnings
- [ ] New/changed behavior has test coverage at the appropriate layer
      (Domain / Application / Infrastructure / WebApi — see
      [CONTRIBUTING.md](../CONTRIBUTING.md#tests))
- [ ] Manually verified against a running instance, if applicable (describe
      how below)

## Checklist

- [ ] Follows the dependency direction in
      [CONTRIBUTING.md](../CONTRIBUTING.md#architecture-rules)
      (Domain ← Application ← Infrastructure/Api)
- [ ] No secrets, tokens, or real credentials in the diff
- [ ] Docs updated if this changes setup steps, config, the API surface, or
      a documented limitation

## Additional context

Anything a reviewer needs to know that isn't obvious from the diff.
