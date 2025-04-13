using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_HR.Migrations
{
    /// <inheritdoc />
    public partial class UpdateSurveyResponse : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Response",
                table: "SurveyResponses",
                newName: "Answers");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Answers",
                table: "SurveyResponses",
                newName: "Response");
        }
    }
}
