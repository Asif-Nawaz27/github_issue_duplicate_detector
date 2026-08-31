using IssueSense.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace IssueSense.Infrastructure.Persistence.Configurations;

internal sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    // Table name matches the existing "onwers" table exactly (a pre-existing typo in the
    // database, not introduced here) - see docs/DEVELOPMENT.md for how this table is managed.
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("onwers");

        builder.HasKey(o => o.Id);
        builder.Property(o => o.Id).HasColumnName("id").ValueGeneratedOnAdd();

        builder.Property(o => o.Name).IsRequired().HasMaxLength(256).HasColumnName("name");

        // Explicit column type: the existing "onwers" table uses "timestamp without time zone",
        // not Npgsql's default "timestamp with time zone" mapping for DateTime.
        builder.Property(o => o.CreatedDate).HasColumnName("created_date").HasColumnType("timestamp without time zone");
        builder.Property(o => o.ChangedDate).HasColumnName("changed_date").HasColumnType("timestamp without time zone");

        // Required for GetByNameAsync (used to resolve Repository.OwnerId) to be safe as a
        // SingleOrDefault lookup, and to reflect that an owner name identifies a distinct GitHub
        // account/org.
        builder.HasIndex(o => o.Name).IsUnique();
    }
}
