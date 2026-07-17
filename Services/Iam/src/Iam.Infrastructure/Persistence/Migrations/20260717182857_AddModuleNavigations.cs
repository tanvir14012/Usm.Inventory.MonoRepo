using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddModuleNavigations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "module_navigations",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockType = table.Column<int>(type: "integer", nullable: false),
                    SystemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MenuId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    MaterialIconName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_module_navigations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sidebar_menu_items",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleNavigationId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentSidebarMenuItemId = table.Column<Guid>(type: "uuid", nullable: true),
                    SystemName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    MenuId = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    LocalizedName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DisplayOrder = table.Column<int>(type: "integer", nullable: false),
                    MaterialIconName = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sidebar_menu_items", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sidebar_menu_items_module_navigations_ModuleNavigationId",
                        column: x => x.ModuleNavigationId,
                        principalSchema: "iam",
                        principalTable: "module_navigations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_module_navigations_BuildingBlockType_MenuId",
                schema: "iam",
                table: "module_navigations",
                columns: new[] { "BuildingBlockType", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sidebar_menu_items_ModuleNavigationId_MenuId",
                schema: "iam",
                table: "sidebar_menu_items",
                columns: new[] { "ModuleNavigationId", "MenuId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sidebar_menu_items_ParentSidebarMenuItemId",
                schema: "iam",
                table: "sidebar_menu_items",
                column: "ParentSidebarMenuItemId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sidebar_menu_items",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "module_navigations",
                schema: "iam");
        }
    }
}
