using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPlacePhoto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PhotoCredit",
                table: "Places",
                type: "nvarchar(300)",
                maxLength: 300,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoCreditUrl",
                table: "Places",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhotoUrl",
                table: "Places",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhotoCredit",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "PhotoCreditUrl",
                table: "Places");

            migrationBuilder.DropColumn(
                name: "PhotoUrl",
                table: "Places");
        }
    }
}
