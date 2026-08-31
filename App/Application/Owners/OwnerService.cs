using IssueSense.Application.Persistence;
using IssueSense.Domain.Entities;

namespace IssueSense.Application.Owners;

public sealed class OwnerService(IOwnerRepository ownerRepository, IUnitOfWork unitOfWork) : IOwnerService
{
    public Task<IReadOnlyList<Owner>> GetAllAsync(CancellationToken cancellationToken = default) =>
        ownerRepository.GetAllAsync(cancellationToken);

    public Task<Owner?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        ownerRepository.GetByIdAsync(id, cancellationToken);

    public async Task<Owner> CreateAsync(string name, CancellationToken cancellationToken = default)
    {
        var owner = Owner.Create(name, UtcNowUnspecified());

        ownerRepository.Add(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner;
    }

    public async Task<Owner?> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
    {
        var owner = await ownerRepository.GetByIdAsync(id, cancellationToken);
        if (owner is null)
            return null;

        owner.Rename(name, UtcNowUnspecified());
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return owner;
    }

    // created_date/changed_date are "timestamp without time zone" columns; Npgsql requires
    // Kind=Unspecified for those (Kind=Utc is rejected outright, to avoid silently mixing
    // offset-aware and offset-naive values). The value is still UTC, just not tagged as such.
    private static DateTime UtcNowUnspecified() => DateTime.SpecifyKind(DateTime.UtcNow, DateTimeKind.Unspecified);

    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var owner = await ownerRepository.GetByIdAsync(id, cancellationToken);
        if (owner is null)
            return false;

        ownerRepository.Remove(owner);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return true;
    }
}
