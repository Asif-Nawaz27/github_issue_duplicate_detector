using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class RepositoryRepository(IssueSenseDbContext dbContext) : IRepositoryRepository
{
    public Task<Repository?> GetByOwnerAndNameAsync(string owner, string name, CancellationToken cancellationToken = default) =>
        dbContext.Repositories.SingleOrDefaultAsync(r => r.Owner == owner && r.Name == name, cancellationToken);

    public void Add(Repository repository) => dbContext.Repositories.Add(repository);
}
