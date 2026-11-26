using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OpenBargain.Modules.Accounts.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNormalizedDisplayName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NormalizedDisplayName",
                schema: "accounts",
                table: "Users",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_Users_NormalizedDisplayName",
                schema: "accounts",
                table: "Users",
                column: "NormalizedDisplayName",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Users_NormalizedDisplayName",
                schema: "accounts",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "NormalizedDisplayName",
                schema: "accounts",
                table: "Users");
        }
    }
}
