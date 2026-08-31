using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryRepositoryRepository(InMemoryOwnerRepository ownerRepository) : IRepositoryRepository
{
    public List<Repository> Repositories { get; } = [];

    public Task<Repository?> GetByOwnerAndNameAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        var ownerId = ownerRepository.Owners.SingleOrDefault(o => o.Name == owner)?.Id;
        return Task.FromResult(Repositories.SingleOrDefault(r => r.OwnerId == ownerId && r.Name == name));
    }

    public Task<IReadOnlyList<Repository>> GetAllByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        var ownerId = ownerRepository.Owners.SingleOrDefault(o => o.Name == owner)?.Id;
        return Task.FromResult<IReadOnlyList<Repository>>(Repositories.Where(r => r.OwnerId == ownerId).OrderBy(r => r.Name).ToList());
    }

    public void Add(Repository repository) => Repositories.Add(repository);
}
