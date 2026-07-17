using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Iam.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganogramRbac : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "iam");

            migrationBuilder.CreateTable(
                name: "organogram_instances",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InstanceCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organogram_instances", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "organogram_templates",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Version = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FirstInstantiatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organogram_templates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "super_admin_assignments",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_super_admin_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "user_organogram_assignments",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    OrganizationalUnitId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_user_organogram_assignments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "instance_approval_paths",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FromDepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ToDepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ToBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ApprovalRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    CrossDepartment = table.Column<bool>(type: "boolean", nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_approval_paths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_approval_paths_organogram_instances_OrganogramInst~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_building_blocks",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ParentBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_building_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_building_blocks_organogram_instances_OrganogramIns~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resource = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_permissions_organogram_instances_OrganogramInstanc~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_position_allocations",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MilitaryCount = table.Column<int>(type: "integer", nullable: false),
                    CivilianCount = table.Column<int>(type: "integer", nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_position_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_position_allocations_organogram_instances_Organogr~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_role_hierarchies",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChildRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RankOrder = table.Column<int>(type: "integer", nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_role_hierarchies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_role_hierarchies_organogram_instances_OrganogramIn~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_role_permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_role_permissions_organogram_instances_OrganogramIn~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "instance_roles",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_instance_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_instance_roles_organogram_instances_OrganogramInstanceId",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "organizational_units",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    InstanceId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentUnitId = table.Column<Guid>(type: "uuid", nullable: true),
                    UnitKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    DepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    OrganogramInstanceId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_organizational_units", x => x.Id);
                    table.ForeignKey(
                        name: "FK_organizational_units_organogram_instances_OrganogramInstanc~",
                        column: x => x.OrganogramInstanceId,
                        principalSchema: "iam",
                        principalTable: "organogram_instances",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "template_approval_paths",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    WorkflowCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    FromDepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    FromBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ToDepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ToBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ApprovalRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    StepOrder = table.Column<int>(type: "integer", nullable: false),
                    CrossDepartment = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_approval_paths", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_approval_paths_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_building_blocks",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ParentBuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_building_blocks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_building_blocks_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_departments",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    DepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    ParentDepartmentCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_departments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_departments_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Resource = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_permissions_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_position_allocations",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    MilitaryCount = table.Column<int>(type: "integer", nullable: false),
                    CivilianCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_position_allocations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_position_allocations_organogram_templates_Template~",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_role_hierarchies",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    ParentRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    ChildRoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    RankOrder = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_role_hierarchies", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_role_hierarchies_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_role_permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    PermissionCode = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_role_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_role_permissions_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "template_roles",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TemplateId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Description = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: false),
                    BuildingBlockCode = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_template_roles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_template_roles_organogram_templates_TemplateId",
                        column: x => x.TemplateId,
                        principalSchema: "iam",
                        principalTable: "organogram_templates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "permissions",
                schema: "iam",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    Resource = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: false),
                    Action = table.Column<string>(type: "character varying(128)", maxLength: 128, nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_permissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_permissions_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "iam",
                        principalTable: "roles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_instance_approval_paths_OrganogramInstanceId",
                schema: "iam",
                table: "instance_approval_paths",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_building_blocks_InstanceId_BuildingBlockCode",
                schema: "iam",
                table: "instance_building_blocks",
                columns: new[] { "InstanceId", "BuildingBlockCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_building_blocks_OrganogramInstanceId",
                schema: "iam",
                table: "instance_building_blocks",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_permissions_InstanceId_PermissionCode",
                schema: "iam",
                table: "instance_permissions",
                columns: new[] { "InstanceId", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_permissions_OrganogramInstanceId",
                schema: "iam",
                table: "instance_permissions",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_position_allocations_InstanceId_BuildingBlockCode_~",
                schema: "iam",
                table: "instance_position_allocations",
                columns: new[] { "InstanceId", "BuildingBlockCode", "RoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_position_allocations_OrganogramInstanceId",
                schema: "iam",
                table: "instance_position_allocations",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_role_hierarchies_InstanceId_ParentRoleCode_ChildRo~",
                schema: "iam",
                table: "instance_role_hierarchies",
                columns: new[] { "InstanceId", "ParentRoleCode", "ChildRoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_role_hierarchies_OrganogramInstanceId",
                schema: "iam",
                table: "instance_role_hierarchies",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_role_permissions_InstanceId_RoleCode_PermissionCode",
                schema: "iam",
                table: "instance_role_permissions",
                columns: new[] { "InstanceId", "RoleCode", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_role_permissions_OrganogramInstanceId",
                schema: "iam",
                table: "instance_role_permissions",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_instance_roles_InstanceId_RoleCode",
                schema: "iam",
                table: "instance_roles",
                columns: new[] { "InstanceId", "RoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_instance_roles_OrganogramInstanceId",
                schema: "iam",
                table: "instance_roles",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_organizational_units_InstanceId_UnitKey",
                schema: "iam",
                table: "organizational_units",
                columns: new[] { "InstanceId", "UnitKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organizational_units_OrganogramInstanceId",
                schema: "iam",
                table: "organizational_units",
                column: "OrganogramInstanceId");

            migrationBuilder.CreateIndex(
                name: "IX_organogram_instances_InstanceCode",
                schema: "iam",
                table: "organogram_instances",
                column: "InstanceCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_organogram_templates_Name_Version",
                schema: "iam",
                table: "organogram_templates",
                columns: new[] { "Name", "Version" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_permissions_RoleId",
                schema: "iam",
                table: "permissions",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_super_admin_assignments_UserId",
                schema: "iam",
                table: "super_admin_assignments",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_approval_paths_TemplateId",
                schema: "iam",
                table: "template_approval_paths",
                column: "TemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_template_building_blocks_TemplateId_BuildingBlockCode",
                schema: "iam",
                table: "template_building_blocks",
                columns: new[] { "TemplateId", "BuildingBlockCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_departments_TemplateId_DepartmentCode",
                schema: "iam",
                table: "template_departments",
                columns: new[] { "TemplateId", "DepartmentCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_permissions_TemplateId_PermissionCode",
                schema: "iam",
                table: "template_permissions",
                columns: new[] { "TemplateId", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_position_allocations_TemplateId_BuildingBlockCode_~",
                schema: "iam",
                table: "template_position_allocations",
                columns: new[] { "TemplateId", "BuildingBlockCode", "RoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_role_hierarchies_TemplateId_ParentRoleCode_ChildRo~",
                schema: "iam",
                table: "template_role_hierarchies",
                columns: new[] { "TemplateId", "ParentRoleCode", "ChildRoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_role_permissions_TemplateId_RoleCode_PermissionCode",
                schema: "iam",
                table: "template_role_permissions",
                columns: new[] { "TemplateId", "RoleCode", "PermissionCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_template_roles_TemplateId_RoleCode",
                schema: "iam",
                table: "template_roles",
                columns: new[] { "TemplateId", "RoleCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_user_organogram_assignments_UserId_InstanceId_RoleCode_Orga~",
                schema: "iam",
                table: "user_organogram_assignments",
                columns: new[] { "UserId", "InstanceId", "RoleCode", "OrganizationalUnitId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "instance_approval_paths",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_building_blocks",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_position_allocations",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_role_hierarchies",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_role_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "instance_roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "organizational_units",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "super_admin_assignments",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_approval_paths",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_building_blocks",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_departments",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_position_allocations",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_role_hierarchies",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_role_permissions",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "template_roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "user_organogram_assignments",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "organogram_instances",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "iam");

            migrationBuilder.DropTable(
                name: "organogram_templates",
                schema: "iam");
        }
    }
}
