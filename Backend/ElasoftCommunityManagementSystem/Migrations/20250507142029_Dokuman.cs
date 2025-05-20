using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ElasoftCommunityManagementSystem.Migrations
{
    /// <inheritdoc />
    public partial class Dokuman : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "DokumanUrl",
                table: "ClubExpenses",
                type: "nvarchar(max)",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DokumanUrl",
                table: "ClubExpenses");
        }
    }
}
