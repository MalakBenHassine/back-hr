using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Back_HR.Migrations
{
    /// <inheritdoc />
    public partial class AddSurveyResponses : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "SubmittedAt",
                table: "SurveyResponses",
                newName: "RespondedAt");

            migrationBuilder.RenameColumn(
                name: "Answers",
                table: "SurveyResponses",
                newName: "Answer");

            migrationBuilder.AddColumn<Guid>(
                name: "QuestionId",
                table: "SurveyResponses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_SurveyResponses_QuestionId",
                table: "SurveyResponses",
                column: "QuestionId");

            migrationBuilder.AddForeignKey(
                name: "FK_SurveyResponses_SurveyQuestions_QuestionId",
                table: "SurveyResponses",
                column: "QuestionId",
                principalTable: "SurveyQuestions",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SurveyResponses_SurveyQuestions_QuestionId",
                table: "SurveyResponses");

            migrationBuilder.DropIndex(
                name: "IX_SurveyResponses_QuestionId",
                table: "SurveyResponses");

            migrationBuilder.DropColumn(
                name: "QuestionId",
                table: "SurveyResponses");

            migrationBuilder.RenameColumn(
                name: "RespondedAt",
                table: "SurveyResponses",
                newName: "SubmittedAt");

            migrationBuilder.RenameColumn(
                name: "Answer",
                table: "SurveyResponses",
                newName: "Answers");
        }
    }
}
