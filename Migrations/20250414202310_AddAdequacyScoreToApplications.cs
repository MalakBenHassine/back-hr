using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_HR.Migrations
{
    /// <inheritdoc />
    public partial class AddAdequacyScoreToApplications : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "AdequacyScore",
                table: "Applications",
                type: "float",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn("AdequacyScore", "Applications");
        }
    }
}
