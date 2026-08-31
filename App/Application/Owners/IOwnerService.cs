using IssueSense.Domain.Entities;

namespace IssueSense.Application.Owners;

public interface IOwnerService
{
    Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Owner?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Owner> CreateAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>Returns null if no owner with the given id exists.</summary>
    Task<Owner?> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);

    /// <summary>Returns false if no owner with the given id exists.</summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
