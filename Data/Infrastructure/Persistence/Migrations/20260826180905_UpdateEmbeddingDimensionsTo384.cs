using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace IssueSense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class UpdateEmbeddingDimensionsTo384 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Vector",
                table: "issue_embeddings",
                type: "vector(384)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(1536)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<Vector>(
                name: "Vector",
                table: "issue_embeddings",
                type: "vector(1536)",
                nullable: false,
                oldClrType: typeof(Vector),
                oldType: "vector(384)");
        }
    }
}
