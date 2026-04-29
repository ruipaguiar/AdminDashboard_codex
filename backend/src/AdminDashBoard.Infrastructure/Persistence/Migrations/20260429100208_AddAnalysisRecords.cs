using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminDashBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAnalysisRecords : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "analysis_records",
                schema: "admin_dashboard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoinId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    Model = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    RiskLevel = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ResponseJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_analysis_records", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_analysis_records_CoinId_Currency_Days_CreatedAtUtc",
                schema: "admin_dashboard",
                table: "analysis_records",
                columns: new[] { "CoinId", "Currency", "Days", "CreatedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "analysis_records",
                schema: "admin_dashboard");
        }
    }
}
