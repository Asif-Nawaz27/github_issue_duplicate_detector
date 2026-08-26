using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Pgvector;

#nullable disable

namespace IssueSense.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:vector", ",,");

            migrationBuilder.CreateTable(
                name: "repositories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubRepositoryId = table.Column<long>(type: "bigint", nullable: false),
                    Owner = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_repositories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "issues",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RepositoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    GitHubIssueId = table.Column<long>(type: "bigint", nullable: false),
                    GitHubIssueNumber = table.Column<int>(type: "integer", nullable: false),
                    Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Body = table.Column<string>(type: "text", nullable: true),
                    State = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ClosedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Labels = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issues", x => x.Id);
                    table.ForeignKey(
                        name: "FK_issues_repositories_RepositoryId",
                        column: x => x.RepositoryId,
                        principalTable: "repositories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "duplicate_candidates",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    NewIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExistingIssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Score = table.Column<double>(type: "double precision", nullable: false),
                    DetectedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_duplicate_candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_duplicate_candidates_issues_ExistingIssueId",
                        column: x => x.ExistingIssueId,
                        principalTable: "issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_duplicate_candidates_issues_NewIssueId",
                        column: x => x.NewIssueId,
                        principalTable: "issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "issue_embeddings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IssueId = table.Column<Guid>(type: "uuid", nullable: false),
                    Vector = table.Column<Vector>(type: "vector(1536)", nullable: false),
                    ModelName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_issue_embeddings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_issue_embeddings_issues_IssueId",
                        column: x => x.IssueId,
                        principalTable: "issues",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_duplicate_candidates_ExistingIssueId",
                table: "duplicate_candidates",
                column: "ExistingIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_duplicate_candidates_NewIssueId",
                table: "duplicate_candidates",
                column: "NewIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_duplicate_candidates_NewIssueId_ExistingIssueId",
                table: "duplicate_candidates",
                columns: new[] { "NewIssueId", "ExistingIssueId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issue_embeddings_IssueId",
                table: "issue_embeddings",
                column: "IssueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issue_embeddings_Vector",
                table: "issue_embeddings",
                column: "Vector")
                .Annotation("Npgsql:IndexMethod", "hnsw")
                .Annotation("Npgsql:IndexOperators", new[] { "vector_cosine_ops" });

            migrationBuilder.CreateIndex(
                name: "IX_issues_GitHubIssueId",
                table: "issues",
                column: "GitHubIssueId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issues_RepositoryId_GitHubIssueNumber",
                table: "issues",
                columns: new[] { "RepositoryId", "GitHubIssueNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_issues_State",
                table: "issues",
                column: "State");

            migrationBuilder.CreateIndex(
                name: "IX_repositories_GitHubRepositoryId",
                table: "repositories",
                column: "GitHubRepositoryId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_repositories_Owner_Name",
                table: "repositories",
                columns: new[] { "Owner", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "duplicate_candidates");

            migrationBuilder.DropTable(
                name: "issue_embeddings");

            migrationBuilder.DropTable(
                name: "issues");

            migrationBuilder.DropTable(
                name: "repositories");
        }
    }
}
