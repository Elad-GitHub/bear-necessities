using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddEditSuggestions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EditSuggestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetType = table.Column<int>(type: "int", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SuggestedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProposedChanges = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    ReviewedBy = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EditSuggestions", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EditSuggestions_Status",
                table: "EditSuggestions",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_EditSuggestions_TargetType_TargetId",
                table: "EditSuggestions",
                columns: new[] { "TargetType", "TargetId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EditSuggestions");
        }
    }
}
