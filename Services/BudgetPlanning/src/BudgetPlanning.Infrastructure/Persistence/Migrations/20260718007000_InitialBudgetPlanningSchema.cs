using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetPlanning.Infrastructure.Persistence.Migrations
{
    [Migration("20260718007000_InitialBudgetPlanningSchema")]
    public partial class InitialBudgetPlanningSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "budgets",
                schema: "budgetplanning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "jsonb", nullable: false),
                    FiscalYear = table.Column<int>(type: "integer", nullable: false),
                    DepartmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    TotalAllocated = table.Column<decimal>(type: "numeric", nullable: false),
                    TotalSpent = table.Column<decimal>(type: "numeric", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_budgets", x => x.Id));

            migrationBuilder.CreateTable(
                name: "budget_items",
                schema: "budgetplanning",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BudgetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Description = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: false),
                    AllocatedAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    SpentAmount = table.Column<decimal>(type: "numeric", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_budget_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_budget_items_budgets_BudgetId",
                        column: x => x.BudgetId,
                        principalSchema: "budgetplanning",
                        principalTable: "budgets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_budget_items_BudgetId",
                schema: "budgetplanning",
                table: "budget_items",
                column: "BudgetId");

            migrationBuilder.CreateIndex(
                name: "IX_budgets_DepartmentId_FiscalYear",
                schema: "budgetplanning",
                table: "budgets",
                columns: new[] { "DepartmentId", "FiscalYear" });

            migrationBuilder.CreateIndex(
                name: "IX_budgets_Status",
                schema: "budgetplanning",
                table: "budgets",
                column: "Status");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "budget_items", schema: "budgetplanning");
            migrationBuilder.DropTable(name: "budgets", schema: "budgetplanning");
        }
    }
}
