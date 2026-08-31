using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Repositories;

public sealed class RepositoryLookupService(IRepositoryRepository repositoryRepository) : IRepositoryLookupService
{
    public Task<IReadOnlyList<Repository>> GetByOwnerAsync(string owner, CancellationToken cancellationToken = default) =>
        repositoryRepository.GetAllByOwnerAsync(owner, cancellationToken);
}
