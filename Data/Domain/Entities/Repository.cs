using IssueSense.Domain.Common;

namespace IssueSense.Domain.Entities;

public sealed class Repository : Entity
{
    public long GitHubRepositoryId { get; }

    public string Owner { get; }

    public string Name { get; }

    public bool IsActive { get; private set; }

    public DateTimeOffset CreatedAt { get; }

    public string FullName => $"{Owner}/{Name}";

    private Repository(Guid id, long gitHubRepositoryId, string owner, string name, DateTimeOffset createdAt)
        : base(id)
    {
        GitHubRepositoryId = gitHubRepositoryId;
        Owner = owner;
        Name = name;
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Repository Create(long gitHubRepositoryId, string owner, string name, DateTimeOffset createdAt)
    {
        if (gitHubRepositoryId <= 0)
            throw new ArgumentOutOfRangeException(nameof(gitHubRepositoryId), gitHubRepositoryId, "GitHub repository id must be positive.");
        if (string.IsNullOrWhiteSpace(owner))
            throw new ArgumentException("Repository owner is required.", nameof(owner));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Repository name is required.", nameof(name));

        return new Repository(Guid.NewGuid(), gitHubRepositoryId, owner.Trim(), name.Trim(), createdAt);
    }

#pragma warning disable CS8618 // Required by EF Core for materialization; properties are set via reflection.
    private Repository()
    {
    }
#pragma warning restore CS8618

    public void Activate() => IsActive = true;

    public void Deactivate() => IsActive = false;
}
