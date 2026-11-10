using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BearNecessities.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddMembershipApproval : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MembershipStatus",
                table: "Users",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "SocialProfileUrl",
                table: "Users",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_MembershipStatus",
                table: "Users",
                column: "MembershipStatus");

            // Existing accounts were created before gated membership — grandfather them in as Approved
            migrationBuilder.Sql("UPDATE Users SET MembershipStatus = 1");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_MembershipStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "MembershipStatus",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "SocialProfileUrl",
                table: "Users");
        }
    }
}
