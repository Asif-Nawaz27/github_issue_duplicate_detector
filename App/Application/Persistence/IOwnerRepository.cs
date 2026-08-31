using IssueSense.Domain.Entities;

namespace IssueSense.Application.Persistence;

public interface IOwnerRepository
{
    Task<Owner?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Owner?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken = default);

    void Add(Owner owner);

    void Remove(Owner owner);
}
