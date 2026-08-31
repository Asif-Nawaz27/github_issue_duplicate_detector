using IssueSense.Domain.Common;

namespace IssueSense.Domain.Entities;

// References Owner by id only, not a navigation property: Repository and Owner are separate
// aggregate roots, each persisted and loaded independently (same convention as Issue -> Repository).
public sealed class Repository : Entity
{
    public long GitHubRepositoryId { get; }

    public int OwnerId { get; }

    public string Name { get; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    private Repository(Guid id, long gitHubRepositoryId, int ownerId, string name, DateTimeOffset createdAt)
        : base(id)
    {
        GitHubRepositoryId = gitHubRepositoryId;
        OwnerId = ownerId;
        Name = name;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Repository Create(long gitHubRepositoryId, int ownerId, string name, DateTimeOffset createdAt)
    {
        if (gitHubRepositoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(gitHubRepositoryId), gitHubRepositoryId, "GitHub repository id must be positive.");
        if (ownerId <= 0)
            throw new ArgumentOutOfRangeException(nameof(ownerId), ownerId, "Owner id must be positive.");
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name is required.", nameof(name));

        return new Repository(Guid.NewGuid(), gitHubRepositoryId, ownerId, name.Trim(), createdAt);
    }

#pragma warning disable CS8618 // Required by EF Core for materialization; properties are set via reflection.
    private Repository()
    {
    }
#pragma warning restore CS8618

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
