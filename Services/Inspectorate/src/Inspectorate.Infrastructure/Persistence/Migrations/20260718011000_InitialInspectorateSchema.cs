using System;
using Inspectorate.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Inspectorate.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(InspectorateDbContext))]
    [Migration("20260718011000_InitialInspectorateSchema")]
    public partial class InitialInspectorateSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "inspections",
                schema: "inspectorate",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InspectionNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    SubjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    SubjectType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InspectorId = table.Column<Guid>(type: "uuid", nullable: false),
                    ScheduledDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ConductedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    Findings = table.Column<string>(type: "character varying(2048)", maxLength: 2048, nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_inspections", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_inspections_InspectionNumber",
                schema: "inspectorate",
                table: "inspections",
                column: "InspectionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_inspections_ScheduledDate",
                schema: "inspectorate",
                table: "inspections",
                column: "ScheduledDate");

            migrationBuilder.CreateIndex(
                name: "IX_inspections_Status",
                schema: "inspectorate",
                table: "inspections",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "inspections", schema: "inspectorate");
        }
    }
}
