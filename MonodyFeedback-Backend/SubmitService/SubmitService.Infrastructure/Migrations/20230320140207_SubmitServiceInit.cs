using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubmitService.Infrastructure.Migrations
{
    public partial class SubmitServiceInit : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "T_Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmitterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmitterName = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    ProcessorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    SubmissionStatus = table.Column<string>(type: "varchar(16)", unicode: false, maxLength: 16, nullable: false),
                    SubmitterTelNumber = table.Column<string>(type: "varchar(15)", unicode: false, maxLength: 15, nullable: true),
                    SubmitterEmail = table.Column<string>(type: "varchar(320)", unicode: false, maxLength: 320, nullable: true),
                    Evaluation_IsSolved = table.Column<bool>(type: "bit", nullable: true),
                    Evaluation_Grade = table.Column<byte>(type: "tinyint", nullable: true),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LastInteractionTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ClosingTime = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Submissions", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                });

            migrationBuilder.CreateTable(
                name: "T_Paragraphs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SequenceInSubmission = table.Column<int>(type: "int", nullable: false),
                    CreationTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Sender = table.Column<string>(type: "varchar(9)", unicode: false, maxLength: 9, nullable: false),
                    TextContent = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Paragraphs", x => x.Id)
                        .Annotation("SqlServer:Clustered", false);
                    table.ForeignKey(
                        name: "FK_T_Paragraphs_T_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "T_Submissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "T_Pictures",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParagraphId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Bucket = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Region = table.Column<string>(type: "varchar(max)", unicode: false, nullable: false),
                    FullObjectKey = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_T_Pictures", x => x.Id);
                    table.ForeignKey(
                        name: "FK_T_Pictures_T_Paragraphs_ParagraphId",
                        column: x => x.ParagraphId,
                        principalTable: "T_Paragraphs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_T_Paragraphs_SubmissionId",
                table: "T_Paragraphs",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_T_Pictures_ParagraphId",
                table: "T_Pictures",
                column: "ParagraphId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "T_Pictures");

            migrationBuilder.DropTable(
                name: "T_Paragraphs");

            migrationBuilder.DropTable(
                name: "T_Submissions");
        }
    }
}
