namespace IssueSense.Domain.Entities;

// Not derived from Entity: the "onwers" table uses a DB-generated int identity key, not the
// client-generated Guid every other entity in this domain uses.
public sealed class Owner
{
    public int Id { get; private set; }

    public string Name { get; private set; }

    public DateTime? CreatedDate { get; private set; }

    public DateTime? ChangedDate { get; private set; }

    private Owner(string name, DateTime createdDate)
    {
        Name = name;
        CreatedDate = createdDate;
    }

    public static Owner Create(string name, DateTime createdDate)
    {
        ValidateName(name);
        return new Owner(name.Trim(), createdDate);
    }

#pragma warning disable CS8618 // Required by EF Core for materialization; properties are set via reflection.
    private Owner()
    {
    }
#pragma warning restore CS8618

    public void Rename(string name, DateTime changedDate)
    {
        ValidateName(name);
        Name = name.Trim();
        ChangedDate = changedDate;
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Owner name is required.", nameof(name));
        if (name.Length > 256)
            throw new ArgumentException("Owner name cannot exceed 256 characters.", nameof(name));
    }
}
