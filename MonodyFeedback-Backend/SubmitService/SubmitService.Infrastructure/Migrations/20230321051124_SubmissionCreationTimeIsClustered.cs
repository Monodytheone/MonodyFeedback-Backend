using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubmitService.Infrastructure.Migrations
{
    public partial class SubmissionCreationTimeIsClustered : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions",
                column: "LastInteractionTime")
                .Annotation("SqlServer:Clustered", true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions");

            migrationBuilder.CreateIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions",
                column: "LastInteractionTime");
        }
    }
}
