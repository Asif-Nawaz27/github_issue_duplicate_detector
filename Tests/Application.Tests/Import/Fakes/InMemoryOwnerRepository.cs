using System.Reflection;
using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Tests.Import.Fakes;

// Owner.Id is DB-generated (identity) with a private setter; this fake simulates that
// assignment via reflection on Add(), the same way a real SaveChangesAsync would.
internal sealed class InMemoryOwnerRepository : IOwnerRepository
{
    private static readonly PropertyInfo IdProperty = typeof(Owner).GetProperty(nameof(Owner.Id))!;

    private int _nextId = 1;

    public List<Owner> Owners { get; } = [];

    public Task<Owner?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        Task.FromResult(Owners.SingleOrDefault(o => o.Id == id));

    public Task<Owner?> GetByNameAsync(string name, CancellationToken cancellationToken = default) =>
        Task.FromResult(Owners.SingleOrDefault(o => o.Name == name));

    public Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Owner>>(Owners.OrderBy(o => o.Id).ToList());

    public void Add(Owner owner)
    {
        IdProperty.SetValue(owner, _nextId++);
        Owners.Add(owner);
    }

    public void Remove(Owner owner) => Owners.Remove(owner);
}
