using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RunnersPal.Core.Migrations
{
    /// <inheritdoc />
    public partial class RouteShareLink : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ShareLink",
                table: "Route",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ShareLink",
                table: "Route");
        }
    }
}
