using System;
using IssueReceipt.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace IssueReceipt.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(IssueReceiptDbContext))]
    [Migration("20260718004000_InitialIssueReceiptSchema")]
    public partial class InitialIssueReceiptSchema : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "issuereceipt");

            migrationBuilder.CreateTable(
                name: "issue_transactions",
                schema: "issuereceipt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "text", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    IssuedTo = table.Column<string>(type: "text", nullable: false),
                    IssuedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Purpose = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_issue_transactions", x => x.Id));

            migrationBuilder.CreateTable(
                name: "receipt_transactions",
                schema: "issuereceipt",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionNumber = table.Column<string>(type: "text", nullable: false),
                    InventoryItemId = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseId = table.Column<Guid>(type: "uuid", nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric", nullable: false),
                    ReceivedFrom = table.Column<string>(type: "text", nullable: false),
                    ReceivedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_receipt_transactions", x => x.Id));

            migrationBuilder.CreateIndex(
                name: "IX_issue_transactions_TransactionNumber",
                schema: "issuereceipt",
                table: "issue_transactions",
                column: "TransactionNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_receipt_transactions_TransactionNumber",
                schema: "issuereceipt",
                table: "receipt_transactions",
                column: "TransactionNumber",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "issue_transactions", schema: "issuereceipt");
            migrationBuilder.DropTable(name: "receipt_transactions", schema: "issuereceipt");
        }
    }
}
