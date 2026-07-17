using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RepairMaintenance.Infrastructure.Persistence.Migrations
{
    public partial class InitialRepairMaintenanceSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "repair_orders",
                schema: "repairmaintenance",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OrderNumber = table.Column<string>(type: "text", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    AssignedTechnicianId = table.Column<Guid>(type: "uuid", nullable: true),
                    ReportedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_repair_orders", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_repair_orders_OrderNumber",
                schema: "repairmaintenance",
                table: "repair_orders",
                column: "OrderNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "repair_orders", schema: "repairmaintenance");
        }
    }
}
