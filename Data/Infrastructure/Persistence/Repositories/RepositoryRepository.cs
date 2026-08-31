using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class RepositoryRepository(IssueSenseDbContext dbContext) : IRepositoryRepository
{
    public async Task<Repository?> GetByOwnerAndNameAsync(string owner, string name, CancellationToken cancellationToken = default)
    {
        var ownerId = await GetOwnerIdAsync(owner, cancellationToken);
        return ownerId is null
            ? null
            : await dbContext.Repositories.SingleOrDefaultAsync(r => r.OwnerId == ownerId && r.Name == name, cancellationToken);
    }

    public async Task<IReadOnlyList<Repository>> GetAllByOwnerAsync(string owner, CancellationToken cancellationToken = default)
    {
        var ownerId = await GetOwnerIdAsync(owner, cancellationToken);
        return ownerId is null
            ? []
            : await dbContext.Repositories.Where(r => r.OwnerId == ownerId).OrderBy(r => r.Name).ToListAsync(cancellationToken);
    }

    public void Add(Repository repository) => dbContext.Repositories.Add(repository);

    private Task<int?> GetOwnerIdAsync(string owner, CancellationToken cancellationToken) =>
        dbContext.Owners.Where(o => o.Name == owner).Select(o => (int?)o.Id).SingleOrDefaultAsync(cancellationToken);
}
