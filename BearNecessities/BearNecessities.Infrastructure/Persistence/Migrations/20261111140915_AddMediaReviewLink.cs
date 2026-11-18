using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMediaReviewLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ReviewId",
                table: "Media",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Media_ReviewId",
                table: "Media",
                column: "ReviewId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Media_ReviewId",
                table: "Media");

            migrationBuilder.DropColumn(
                name: "ReviewId",
                table: "Media");
        }
    }
}
