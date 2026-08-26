using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

internal sealed class InMemoryRepositoryRepository : IRepositoryRepository
{
    public List<Repository> Repositories { get; } = [];

    public Task<Repository?> GetByOwnerAndNameAsync(string owner, string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Repositories.SingleOrDefault(r => r.Owner == owner && r.Name == name));

    public void Add(Repository repository) => Repositories.Add(repository);
}
