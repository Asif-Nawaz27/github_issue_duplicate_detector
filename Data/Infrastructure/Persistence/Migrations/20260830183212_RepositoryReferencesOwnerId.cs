using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueSense.Infrastructure.Persistence.Migrations
{
    /// <summary>
    /// Replaces repositories."Owner" (a plain string) with repositories."OwnerId", a foreign key
    /// into onwers. Hand-written rather than the raw EF scaffold: the scaffolded version dropped
    /// "Owner" before backfilling and defaulted the new column to 0, which would both lose data
    /// and violate the new FK constraint against any existing repository rows.
    /// </summary>
    public partial class RepositoryReferencesOwnerId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "OwnerId",
                table: "repositories",
                type: "integer",
                nullable: true);

            // Backfill: ensure an onwers row exists for every distinct existing repositories."Owner"
            // value, then point OwnerId at it.
            migrationBuilder.Sql("""
                INSERT INTO onwers (name, created_date)
                SELECT DISTINCT r."Owner", (now() AT TIME ZONE 'utc')
                FROM repositories r
                WHERE NOT EXISTS (SELECT 1 FROM onwers o WHERE o.name = r."Owner");
                """);

            migrationBuilder.Sql("""
                UPDATE repositories r
                SET "OwnerId" = o.id
                FROM onwers o
                WHERE o.name = r."Owner";
                """);

            migrationBuilder.DropIndex(
                name: "IX_repositories_Owner_Name",
                table: "repositories");

            migrationBuilder.DropColumn(
                name: "Owner",
                table: "repositories");

            migrationBuilder.AlterColumn<int>(
                name: "OwnerId",
                table: "repositories",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_repositories_OwnerId_Name",
                table: "repositories",
                columns: new[] { "OwnerId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_onwers_name",
                table: "onwers",
                column: "name",
                unique: true);

            // Restrict, not the EF default of Cascade: deleting an owner should never silently
            // delete every repository (and, in turn, every issue) it has.
            migrationBuilder.AddForeignKey(
                name: "FK_repositories_onwers_OwnerId",
                table: "repositories",
                column: "OwnerId",
                principalTable: "onwers",
                principalColumn: "id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Owner",
                table: "repositories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE repositories r
                SET "Owner" = o.name
                FROM onwers o
                WHERE o.id = r."OwnerId";
                """);

            migrationBuilder.DropForeignKey(
                name: "FK_repositories_onwers_OwnerId",
                table: "repositories");

            migrationBuilder.DropIndex(
                name: "IX_repositories_OwnerId_Name",
                table: "repositories");

            migrationBuilder.DropIndex(
                name: "IX_onwers_name",
                table: "onwers");

            migrationBuilder.DropColumn(
                name: "OwnerId",
                table: "repositories");

            migrationBuilder.AlterColumn<string>(
                name: "Owner",
                table: "repositories",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(200)",
                oldMaxLength: 200,
                oldNullable: true,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_repositories_Owner_Name",
                table: "repositories",
                columns: new[] { "Owner", "Name" },
                unique: true);
        }
    }
}
