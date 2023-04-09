using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SubmitService.Infrastructure.Migrations
{
    public partial class AddPictureSequence : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte>(
                name: "Sequence",
                table: "T_Pictures",
                type: "tinyint",
                nullable: false,
                defaultValue: (byte)0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Sequence",
                table: "T_Pictures");
        }
    }
}
