using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AdminDashBoard.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "admin_dashboard");

            migrationBuilder.CreateTable(
                name: "market_data_snapshots",
                schema: "admin_dashboard",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CoinId = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Days = table.Column<int>(type: "integer", nullable: false),
                    RetrievedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    PayloadJson = table.Column<string>(type: "jsonb", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_market_data_snapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_market_data_snapshots_CoinId_Currency_Days_RetrievedAtUtc",
                schema: "admin_dashboard",
                table: "market_data_snapshots",
                columns: new[] { "CoinId", "Currency", "Days", "RetrievedAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "market_data_snapshots",
                schema: "admin_dashboard");
        }
    }
}
