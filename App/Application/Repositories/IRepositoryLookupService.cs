using IssueSense.Domain.Entities;

namespace IssueSense.Application.Repositories;

/// <summary>Read-only lookups over previously-imported repositories, for UI/autocomplete use.</summary>
public interface IRepositoryLookupService
{
    Task<IReadOnlyList<Repository>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default);
}
