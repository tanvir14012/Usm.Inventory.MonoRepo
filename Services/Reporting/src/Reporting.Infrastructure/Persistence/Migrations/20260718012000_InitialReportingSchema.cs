using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Reporting.Infrastructure.Persistence;

#nullable disable

namespace Reporting.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ReportingDbContext))]
    [Migration("20260718012000_InitialReportingSchema")]
    public partial class InitialReportingSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "reports",
                schema: "reporting",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "jsonb", nullable: false),
                    ReportType = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    GeneratedById = table.Column<Guid>(type: "uuid", nullable: false),
                    Parameters = table.Column<string>(type: "jsonb", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    OutputPath = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    GeneratedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_reports", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_reports_GeneratedById",
                schema: "reporting",
                table: "reports",
                column: "GeneratedById");

            migrationBuilder.CreateIndex(
                name: "IX_reports_Status",
                schema: "reporting",
                table: "reports",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "reports", schema: "reporting");
        }
    }
}
