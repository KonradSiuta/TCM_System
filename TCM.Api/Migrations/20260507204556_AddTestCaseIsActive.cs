using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCaseIsActive : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "TestCases",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "TestCases");
        }
    }
}
