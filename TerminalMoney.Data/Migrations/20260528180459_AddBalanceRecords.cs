using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TerminalMoney.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBalanceRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BalanceRecords",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserProfileId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetKind = table.Column<string>(type: "TEXT", maxLength: 30, nullable: false),
                    TargetId = table.Column<int>(type: "INTEGER", nullable: false),
                    TargetName = table.Column<string>(type: "TEXT", maxLength: 150, nullable: false),
                    CategoryName = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                    PreviousBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    NewBalance = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    ChangeAmount = table.Column<decimal>(type: "TEXT", precision: 18, scale: 2, nullable: false),
                    RecordedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BalanceRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BalanceRecords_UserProfiles_UserProfileId",
                        column: x => x.UserProfileId,
                        principalTable: "UserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceRecords_TargetKind_TargetId_RecordedAt",
                table: "BalanceRecords",
                columns: new[] { "TargetKind", "TargetId", "RecordedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_BalanceRecords_UserProfileId_RecordedAt",
                table: "BalanceRecords",
                columns: new[] { "UserProfileId", "RecordedAt" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BalanceRecords");
        }
    }
}
