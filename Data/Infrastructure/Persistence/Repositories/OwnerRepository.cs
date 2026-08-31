using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace IssueSense.Infrastructure.Persistence.Repositories;

internal sealed class OwnerRepository(IssueSenseDbContext dbContext) : IOwnerRepository
{
    public Task<Owner?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        dbContext.Owners.SingleOrDefaultAsync(o => o.Id == id, cancellationToken);

    public Task<Owner?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        dbContext.Owners.SingleOrDefaultAsync(o => o.Name == name, cancellationToken);

    public async Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await dbContext.Owners.OrderBy(o => o.Id).ToListAsync(cancellationToken);

    public void Add(Owner owner) => dbContext.Owners.Add(owner);

    public void Remove(Owner owner) => dbContext.Owners.Remove(owner);
}
