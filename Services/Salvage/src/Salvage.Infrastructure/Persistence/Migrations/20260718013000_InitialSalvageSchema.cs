using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Salvage.Infrastructure.Persistence.Migrations
{
    [Migration("20260718013000_InitialSalvageSchema")]
    public partial class InitialSalvageSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "salvage_records",
                schema: "salvage",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RecordNumber = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    Reason = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    SalvageDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ApprovedBy = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_salvage_records", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_salvage_records_RecordNumber",
                schema: "salvage",
                table: "salvage_records",
                column: "RecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_salvage_records_Status",
                schema: "salvage",
                table: "salvage_records",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "salvage_records", schema: "salvage");
        }
    }
}
