using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubmitService.Infrastructure.Migrations
{
    public partial class SubmissionsSetIndex : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_T_Pictures",
                table: "T_Pictures");

            migrationBuilder.AddPrimaryKey(
                name: "PK_T_Pictures",
                table: "T_Pictures",
                column: "Id")
                .Annotation("SqlServer:Clustered", false);

            migrationBuilder.CreateIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions",
                column: "LastInteractionTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_T_Submissions_LastInteractionTime",
                table: "T_Submissions");

            migrationBuilder.DropPrimaryKey(
                name: "PK_T_Pictures",
                table: "T_Pictures");

            migrationBuilder.AddPrimaryKey(
                name: "PK_T_Pictures",
                table: "T_Pictures",
                column: "Id");
        }
    }
}
