using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacePhotoSearchedAt : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "PhotoSearchedAt",
                table: "Places",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoSearchedAt",
                table: "Places");
        }
    }
}
