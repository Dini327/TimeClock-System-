using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TimeClock.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ReplaceAutoCloseWithManualClose : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "IsAutoClosed",
                table: "AttendanceLogs",
                newName: "IsManuallyClosed");

            migrationBuilder.AddColumn<string>(
                name: "ManualCloseReason",
                table: "AttendanceLogs",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ManualCloseReason",
                table: "AttendanceLogs");

            migrationBuilder.RenameColumn(
                name: "IsManuallyClosed",
                table: "AttendanceLogs",
                newName: "IsAutoClosed");
        }
    }
}
