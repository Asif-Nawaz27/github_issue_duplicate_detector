using IssueSense.Application.Persistence;

namespace IssueSense.Infrastructure.Persistence;

internal sealed class UnitOfWork(IssueSenseDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
