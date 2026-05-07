using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCM.Api.Migrations
{
    /// <inheritdoc />
    public partial class ChangeProjectParserToString : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TestCaseParser",
                table: "Projects");

            migrationBuilder.AddColumn<string>(
                name: "ParserName",
                table: "Projects",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ParserName",
                table: "Projects");

            migrationBuilder.AddColumn<int>(
                name: "TestCaseParser",
                table: "Projects",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
