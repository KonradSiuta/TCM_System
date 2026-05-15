using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TCM.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddTestCasePriority : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Requirements_TestCases_TestCaseId",
                table: "Requirements");

            migrationBuilder.DropIndex(
                name: "IX_Requirements_TestCaseId",
                table: "Requirements");

            migrationBuilder.DropColumn(
                name: "TestCaseId",
                table: "Requirements");

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "TestCases",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Priority",
                table: "TestCases");

            migrationBuilder.AddColumn<int>(
                name: "TestCaseId",
                table: "Requirements",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Requirements_TestCaseId",
                table: "Requirements",
                column: "TestCaseId");

            migrationBuilder.AddForeignKey(
                name: "FK_Requirements_TestCases_TestCaseId",
                table: "Requirements",
                column: "TestCaseId",
                principalTable: "TestCases",
                principalColumn: "Id");
        }
    }
}
