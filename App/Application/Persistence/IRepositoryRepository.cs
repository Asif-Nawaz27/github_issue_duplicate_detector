using IssueSense.Domain.Entities;

namespace IssueSense.Application.Persistence;

public interface IRepositoryRepository
{
    Task<Repository?> GetByOwnerAndNameAsync(string owner, string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Repository>> GetAllByOwnerAsync(string owner, CancellationToken cancellationToken = default);

    void Add(Repository repository);
}
