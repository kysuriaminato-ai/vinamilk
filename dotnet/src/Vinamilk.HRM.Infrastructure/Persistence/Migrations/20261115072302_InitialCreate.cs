using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "system");

            migrationBuilder.EnsureSchema(
                name: "talent");

            migrationBuilder.EnsureSchema(
                name: "time");

            migrationBuilder.EnsureSchema(
                name: "core");

            migrationBuilder.EnsureSchema(
                name: "identity");

            migrationBuilder.EnsureSchema(
                name: "payroll");

            migrationBuilder.CreateTable(
                name: "Assets",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    AssetName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    AssetCategory = table.Column<string>(type: "varchar(50)", nullable: false),
                    SerialNumber = table.Column<string>(type: "varchar(100)", nullable: true),
                    PurchaseValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PurchaseDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Available"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: true),
                    Username = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ModuleName = table.Column<string>(type: "varchar(50)", nullable: false),
                    ActionType = table.Column<string>(type: "varchar(30)", nullable: false),
                    EntityName = table.Column<string>(type: "varchar(100)", nullable: false),
                    EntityId = table.Column<string>(type: "varchar(50)", nullable: false),
                    OldValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NewValuesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AffectedColumns = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IpAddress = table.Column<string>(type: "varchar(64)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EducationLevels",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LevelCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    LevelName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    RankOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EducationLevels", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EvaluationCycles",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CycleCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    CycleName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CycleType = table.Column<string>(type: "varchar(20)", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PeriodNumber = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    SelfEvalDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    ManagerEvalDeadline = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Closed"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EvaluationCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Holidays",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    HolidayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    HolidayName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    IsPaidHoliday = table.Column<bool>(type: "bit", nullable: false),
                    OvertimeMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Holidays", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LeaveTypes",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LeaveTypeCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    LeaveTypeName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    IsPaidByCompany = table.Column<bool>(type: "bit", nullable: false),
                    IsPaidBySocialInsurance = table.Column<bool>(type: "bit", nullable: false),
                    MaxDaysPerYear = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    AllowCarryForward = table.Column<bool>(type: "bit", nullable: false),
                    AttendanceSymbol = table.Column<string>(type: "varchar(10)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveTypes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItems",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    MenuCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Icon = table.Column<string>(type: "varchar(50)", nullable: true),
                    RouteUrl = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    RequiredPermissionCode = table.Column<string>(type: "varchar(100)", nullable: true),
                    ParentId = table.Column<int>(type: "int", nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnboardingTemplates",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TemplateCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ProcessType = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Onboarding"),
                    AssignedDepartmentRole = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "HR"),
                    DueDaysOffset = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnboardingTemplates", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OrganizationUnits",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UnitCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    UnitName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    UnitType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Department"),
                    UnitLevel = table.Column<int>(type: "int", nullable: false),
                    ParentUnitId = table.Column<int>(type: "int", nullable: true),
                    TreePath = table.Column<string>(type: "varchar(250)", nullable: true),
                    ManagerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Phone = table.Column<string>(type: "varchar(30)", nullable: true),
                    CostCenterCode = table.Column<string>(type: "varchar(20)", nullable: true),
                    PlannedHeadcount = table.Column<int>(type: "int", nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationUnits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationUnits_OrganizationUnits_ParentUnitId",
                        column: x => x.ParentUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollParameters",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ParamCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    ParamName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ParamValue = table.Column<decimal>(type: "decimal(18,4)", nullable: false),
                    ValueUnit = table.Column<string>(type: "varchar(30)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    LegalReference = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollParameters", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayrollPeriods",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PeriodCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    PeriodTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StandardWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Draft"),
                    CalculatedByUserId = table.Column<int>(type: "int", nullable: true),
                    CalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ApprovedByUserId = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    PayslipSentAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollPeriods", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Permissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PermissionCode = table.Column<string>(type: "varchar(100)", nullable: false),
                    ModuleName = table.Column<string>(type: "varchar(50)", nullable: false),
                    ActionName = table.Column<string>(type: "varchar(30)", nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permissions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Positions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PositionCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    PositionName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PositionGroup = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Staff"),
                    PositionLevel = table.Column<int>(type: "int", nullable: false),
                    DefaultPositionAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Positions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RewardDisciplineCategories",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CategoryCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    CategoryName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    CategoryType = table.Column<string>(type: "varchar(20)", nullable: false),
                    DefaultAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardDisciplineCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Roles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DefaultDataScope = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "SelfOnly"),
                    IsSystemRole = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryComponents",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ComponentCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ComponentType = table.Column<string>(type: "varchar(30)", nullable: false),
                    IsTaxable = table.Column<bool>(type: "bit", nullable: false),
                    TaxExemptLimit = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsSocialInsuranceBase = table.Column<bool>(type: "bit", nullable: false),
                    FormulaExpression = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    SortOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryComponents", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryScales",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ScaleCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    ScaleName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ReviewCycleMonths = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryScales", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaxBrackets",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BracketLevel = table.Column<int>(type: "int", nullable: false),
                    FromIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ToIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    TaxRatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    QuickSubtractionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaxBrackets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TrainingCourses",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CourseCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    CourseName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CourseCategory = table.Column<string>(type: "varchar(50)", nullable: false),
                    DurationHours = table.Column<int>(type: "int", nullable: false),
                    StandardCostPerPerson = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CommitmentMonths = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingCourses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowDefinitions",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    WorkflowName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowDefinitions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkShifts",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ShiftCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    ShiftName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    BreakStartTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    BreakEndTime = table.Column<TimeOnly>(type: "time", nullable: true),
                    IsOvernight = table.Column<bool>(type: "bit", nullable: false),
                    StandardHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WorkdayValue = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NightShiftAllowanceRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    GraceLateMinutes = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkShifts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RecruitmentRequests",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RequestCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    RequestedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    HiredCount = table.Column<int>(type: "int", nullable: false),
                    EmploymentType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "FullTime"),
                    ReasonType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "NewHeadcount"),
                    BudgetMinSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    BudgetMaxSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedOnboardDate = table.Column<DateOnly>(type: "date", nullable: true),
                    JobRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RecruitmentRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RecruitmentRequests_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RecruitmentRequests_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RolePermissions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    PermissionId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Permissions_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "identity",
                        principalTable: "Permissions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RolePermissions_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalarySteps",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryScaleId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    StandardSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SalaryCoefficient = table.Column<decimal>(type: "decimal(8,4)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalarySteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalarySteps_SalaryScales_SalaryScaleId",
                        column: x => x.SalaryScaleId,
                        principalSchema: "core",
                        principalTable: "SalaryScales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingClasses",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClassCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    TrainingCourseId = table.Column<int>(type: "int", nullable: false),
                    ClassName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    InstructorName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    MaxCapacity = table.Column<int>(type: "int", nullable: false),
                    TotalActualCost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Completed"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingClasses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingClasses_TrainingCourses_TrainingCourseId",
                        column: x => x.TrainingCourseId,
                        principalSchema: "talent",
                        principalTable: "TrainingCourses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowSteps",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApproverType = table.Column<string>(type: "varchar(30)", nullable: false),
                    ApproverRoleId = table.Column<int>(type: "int", nullable: true),
                    ApproverUserId = table.Column<int>(type: "int", nullable: true),
                    ConditionExpression = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    SlaHours = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowSteps_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "system",
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobPostings",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PostingCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    RecruitmentRequestId = table.Column<int>(type: "int", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    Vacancies = table.Column<int>(type: "int", nullable: false),
                    SalaryRangeText = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    WorkLocation = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PublishDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ClosingDate = table.Column<DateOnly>(type: "date", nullable: false),
                    JobDescription = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    JobRequirements = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Benefits = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Open"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobPostings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobPostings_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobPostings_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobPostings_RecruitmentRequests_RecruitmentRequestId",
                        column: x => x.RecruitmentRequestId,
                        principalSchema: "talent",
                        principalTable: "RecruitmentRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Employees",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: false),
                    CitizenIdEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: false),
                    CitizenIdHash = table.Column<string>(type: "char(64)", nullable: false),
                    CitizenIdIssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CitizenIdIssuePlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Ethnicity = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Religion = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaritalStatus = table.Column<string>(type: "varchar(20)", nullable: false),
                    PlaceOfOrigin = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PermanentAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CurrentAddress = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: true),
                    WorkEmail = table.Column<string>(type: "varchar(150)", nullable: true),
                    PersonalEmail = table.Column<string>(type: "varchar(150)", nullable: true),
                    BankAccountEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    BankName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    BankBranch = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    TaxCodeEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    SocialInsuranceNumber = table.Column<string>(type: "varchar(30)", nullable: true),
                    HealthInsuranceNumber = table.Column<string>(type: "varchar(30)", nullable: true),
                    HealthInsurancePlace = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    DirectManagerId = table.Column<int>(type: "int", nullable: true),
                    EducationLevelId = table.Column<int>(type: "int", nullable: true),
                    Major = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    SalaryStepId = table.Column<int>(type: "int", nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    HireDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OfficialDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TerminationDate = table.Column<DateOnly>(type: "date", nullable: true),
                    WorkStatus = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Official"),
                    CurrentContractType = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Indefinite"),
                    AvatarUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsUnionMember = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Employees", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Employees_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalSchema: "core",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Employees_DirectManagerId",
                        column: x => x.DirectManagerId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Employees_SalarySteps_SalaryStepId",
                        column: x => x.SalaryStepId,
                        principalSchema: "core",
                        principalTable: "SalarySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AiRecommendations",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: true),
                    RecommendationType = table.Column<string>(type: "varchar(50)", nullable: false),
                    PriorityLevel = table.Column<string>(type: "varchar(20)", nullable: false),
                    ConfidenceScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    AnalysisContent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SuggestedAction = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "New"),
                    ReviewedByUserId = table.Column<int>(type: "int", nullable: true),
                    ReviewedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiRecommendations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AiRecommendations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AiRecommendations_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AttendanceLogs",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PunchTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PunchType = table.Column<string>(type: "varchar(20)", nullable: false),
                    SourceType = table.Column<string>(type: "varchar(30)", nullable: false),
                    DeviceId = table.Column<string>(type: "varchar(50)", nullable: true),
                    Latitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    Longitude = table.Column<decimal>(type: "decimal(10,7)", nullable: true),
                    LocationAddress = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceLogs_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Candidates",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    JobPostingId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    Gender = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    PhoneNumber = table.Column<string>(type: "varchar(20)", nullable: false),
                    Email = table.Column<string>(type: "varchar(150)", nullable: false),
                    EducationLevelId = table.Column<int>(type: "int", nullable: true),
                    Major = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    YearsOfExperience = table.Column<decimal>(type: "decimal(4,1)", nullable: false),
                    ExpectedSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    SourceChannel = table.Column<string>(type: "varchar(50)", nullable: false),
                    ResumeUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Stage = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Applied"),
                    OverallScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ConvertedEmployeeId = table.Column<int>(type: "int", nullable: true),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Candidates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Candidates_EducationLevels_EducationLevelId",
                        column: x => x.EducationLevelId,
                        principalSchema: "core",
                        principalTable: "EducationLevels",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidates_Employees_ConvertedEmployeeId",
                        column: x => x.ConvertedEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Candidates_JobPostings_JobPostingId",
                        column: x => x.JobPostingId,
                        principalSchema: "talent",
                        principalTable: "JobPostings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DailyAttendances",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AttendanceDate = table.Column<DateOnly>(type: "date", nullable: false),
                    WorkShiftId = table.Column<int>(type: "int", nullable: true),
                    CheckInTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CheckOutTime = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AttendanceSymbol = table.Column<string>(type: "varchar(10)", nullable: false),
                    WorkingHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    WorkdayCount = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    LateMinutes = table.Column<int>(type: "int", nullable: false),
                    EarlyLeaveMinutes = table.Column<int>(type: "int", nullable: false),
                    OvertimeHours150 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OvertimeHours200 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OvertimeHours300 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    NightShiftHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsManualEdited = table.Column<bool>(type: "bit", nullable: false),
                    EditReason = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DailyAttendances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DailyAttendances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_DailyAttendances_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalSchema: "time",
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "DataPrivacyConsents",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    PolicyVersion = table.Column<string>(type: "varchar(30)", nullable: false),
                    IsConsented = table.Column<bool>(type: "bit", nullable: false),
                    ConsentedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(64)", nullable: true),
                    ConsentNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataPrivacyConsents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DataPrivacyConsents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeAssetAssignments",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    AssignedDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReturnedDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ConditionOnAssign = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    ConditionOnReturn = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Active"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeAssetAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "talent",
                        principalTable: "Assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeAssetAssignments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeCertificates",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CertificateName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    CertificateType = table.Column<string>(type: "varchar(30)", nullable: false),
                    IssuedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    IssueDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ScoreOrGrade = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeCertificates", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeCertificates_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDependents",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FullName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Relationship = table.Column<string>(type: "varchar(30)", nullable: false),
                    DateOfBirth = table.Column<DateOnly>(type: "date", nullable: true),
                    CitizenIdEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    TaxCodeEncrypted = table.Column<string>(type: "nvarchar(512)", maxLength: 512, nullable: true),
                    IsTaxDeductible = table.Column<bool>(type: "bit", nullable: false),
                    DeductionFromDate = table.Column<DateOnly>(type: "date", nullable: true),
                    DeductionToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    ProofDocumentUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsEmergencyContact = table.Column<bool>(type: "bit", nullable: false),
                    EmergencyPhone = table.Column<string>(type: "varchar(20)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDependents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDependents_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeDocuments",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    DocumentCategory = table.Column<string>(type: "varchar(50)", nullable: false),
                    DocumentTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ContentType = table.Column<string>(type: "varchar(100)", nullable: true),
                    FileSizeBytes = table.Column<long>(type: "bigint", nullable: false),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeDocuments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeKpiEvaluations",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EvaluationCycleId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ReviewerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    TotalWeight = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SelfTotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ManagerTotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RatingGrade = table.Column<string>(type: "varchar(10)", nullable: false),
                    BonusMultiplier = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EmployeeComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    ReviewerComment = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeKpiEvaluations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeKpiEvaluations_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeKpiEvaluations_Employees_ReviewerEmployeeId",
                        column: x => x.ReviewerEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeKpiEvaluations_EvaluationCycles_EvaluationCycleId",
                        column: x => x.EvaluationCycleId,
                        principalSchema: "talent",
                        principalTable: "EvaluationCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeOnboardingTasks",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OnboardingTemplateId = table.Column<int>(type: "int", nullable: true),
                    ProcessType = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "Onboarding"),
                    TaskName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    PicEmployeeId = table.Column<int>(type: "int", nullable: true),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeOnboardingTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeOnboardingTasks_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_EmployeeOnboardingTasks_OnboardingTemplates_OnboardingTemplateId",
                        column: x => x.OnboardingTemplateId,
                        principalSchema: "talent",
                        principalTable: "OnboardingTemplates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmploymentHistories",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ToDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CompanyName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    DepartmentName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    PositionName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    IsInternal = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmploymentHistories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmploymentHistories_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LaborContracts",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ContractNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ContractType = table.Column<string>(type: "varchar(30)", nullable: false),
                    SignDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    SalaryStepId = table.Column<int>(type: "int", nullable: true),
                    BaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ProbationSalaryRate = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 100m),
                    SignerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Active"),
                    SignedFileUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LaborContracts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LaborContracts_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LaborContracts_SalarySteps_SalaryStepId",
                        column: x => x.SalaryStepId,
                        principalSchema: "core",
                        principalTable: "SalarySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveBalances",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StandardEntitlement = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SeniorityExtraDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CarriedForwardDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UsedDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PendingDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    RemainingDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CarryForwardExpiryDate = table.Column<DateOnly>(type: "date", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveBalances_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "time",
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveRequests",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    LeaveTypeId = table.Column<int>(type: "int", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartSession = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "FullDay"),
                    EndSession = table.Column<string>(type: "varchar(20)", nullable: false, defaultValue: "FullDay"),
                    TotalDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    HandoverToEmployeeId = table.Column<int>(type: "int", nullable: true),
                    AttachmentUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    ApproverComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveRequests_LeaveTypes_LeaveTypeId",
                        column: x => x.LeaveTypeId,
                        principalSchema: "time",
                        principalTable: "LeaveTypes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MonthlyAttendanceSummaries",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    Month = table.Column<int>(type: "int", nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    StandardWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ActualWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PaidLeaveDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    HolidayDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SocialInsuranceLeaveDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    UnpaidLeaveDays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalLateCount = table.Column<int>(type: "int", nullable: false),
                    TotalLateMinutes = table.Column<int>(type: "int", nullable: false),
                    TotalOtHours150 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalOtHours200 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalOtHours300 = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalNightHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalPayableWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    LockedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LockedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MonthlyAttendanceSummaries", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MonthlyAttendanceSummaries_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "OvertimeRequests",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OvertimeDate = table.Column<DateOnly>(type: "date", nullable: false),
                    StartTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    EndTime = table.Column<TimeOnly>(type: "time", nullable: false),
                    RequestedHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ApprovedHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    OvertimeRateType = table.Column<string>(type: "varchar(20)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OvertimeRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OvertimeRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PersonnelTransfers",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    TransferType = table.Column<string>(type: "varchar(30)", nullable: false),
                    FromUnitId = table.Column<int>(type: "int", nullable: false),
                    ToUnitId = table.Column<int>(type: "int", nullable: false),
                    FromPositionId = table.Column<int>(type: "int", nullable: false),
                    ToPositionId = table.Column<int>(type: "int", nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    AppliedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PersonnelTransfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PersonnelTransfers_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTransfers_OrganizationUnits_FromUnitId",
                        column: x => x.FromUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTransfers_OrganizationUnits_ToUnitId",
                        column: x => x.ToUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTransfers_Positions_FromPositionId",
                        column: x => x.FromPositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PersonnelTransfers_Positions_ToPositionId",
                        column: x => x.ToPositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RewardDisciplines",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    CategoryId = table.Column<int>(type: "int", nullable: false),
                    Type = table.Column<string>(type: "varchar(20)", nullable: false),
                    DecisionDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SignedByEmployeeId = table.Column<int>(type: "int", nullable: true),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Approved"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RewardDisciplines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RewardDisciplines_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RewardDisciplines_RewardDisciplineCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalSchema: "core",
                        principalTable: "RewardDisciplineCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryProgressions",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    DecisionNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OldSalaryStepId = table.Column<int>(type: "int", nullable: true),
                    NewSalaryStepId = table.Column<int>(type: "int", nullable: true),
                    OldBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OldAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NewAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    NextReviewDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Applied"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryProgressions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryProgressions_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryProgressions_SalarySteps_NewSalaryStepId",
                        column: x => x.NewSalaryStepId,
                        principalSchema: "core",
                        principalTable: "SalarySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryProgressions_SalarySteps_OldSalaryStepId",
                        column: x => x.OldSalaryStepId,
                        principalSchema: "core",
                        principalTable: "SalarySteps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ShiftSchedules",
                schema: "time",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    WorkShiftId = table.Column<int>(type: "int", nullable: false),
                    WorkDate = table.Column<DateOnly>(type: "date", nullable: false),
                    IsDayOff = table.Column<bool>(type: "bit", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShiftSchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ShiftSchedules_WorkShifts_WorkShiftId",
                        column: x => x.WorkShiftId,
                        principalSchema: "time",
                        principalTable: "WorkShifts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TrainingEnrollments",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TrainingClassId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    EnrolledDate = table.Column<DateOnly>(type: "date", nullable: false),
                    AttendanceRatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    FinalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    Result = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Passed"),
                    CertificateNumber = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    FeedbackRating = table.Column<int>(type: "int", nullable: true),
                    FeedbackComment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TrainingEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_TrainingEnrollments_TrainingClasses_TrainingClassId",
                        column: x => x.TrainingClassId,
                        principalSchema: "talent",
                        principalTable: "TrainingClasses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Username = table.Column<string>(type: "varchar(50)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorSecret = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false),
                    LockoutEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    PasswordChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    MustChangePassword = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowInstances",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowDefinitionId = table.Column<int>(type: "int", nullable: false),
                    EntityType = table.Column<string>(type: "varchar(50)", nullable: false),
                    EntityId = table.Column<int>(type: "int", nullable: false),
                    InitiatorEmployeeId = table.Column<int>(type: "int", nullable: false),
                    CurrentStepOrder = table.Column<int>(type: "int", nullable: false),
                    CurrentApproverUserId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "InProgress"),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowInstances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowInstances_Employees_InitiatorEmployeeId",
                        column: x => x.InitiatorEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId",
                        column: x => x.WorkflowDefinitionId,
                        principalSchema: "system",
                        principalTable: "WorkflowDefinitions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Interviews",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    RoundNumber = table.Column<int>(type: "int", nullable: false),
                    RoundTitle = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ScheduledStartTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEndTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InterviewMode = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "InPerson"),
                    LocationOrLink = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    InterviewerEmployeeId = table.Column<int>(type: "int", nullable: true),
                    TechnicalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    CultureFitScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EvaluationNotes = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Result = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Pending"),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Interviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Interviews_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "talent",
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Interviews_Employees_InterviewerEmployeeId",
                        column: x => x.InterviewerEmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "JobOffers",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OfferCode = table.Column<string>(type: "varchar(30)", nullable: false),
                    CandidateId = table.Column<int>(type: "int", nullable: false),
                    ProposedPositionId = table.Column<int>(type: "int", nullable: false),
                    ProposedUnitId = table.Column<int>(type: "int", nullable: false),
                    OfferedProbationSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OfferedOfficialSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ExpectedStartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    OfferExpiryDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Sent"),
                    CandidateFeedback = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobOffers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_JobOffers_Candidates_CandidateId",
                        column: x => x.CandidateId,
                        principalSchema: "talent",
                        principalTable: "Candidates",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobOffers_OrganizationUnits_ProposedUnitId",
                        column: x => x.ProposedUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_JobOffers_Positions_ProposedPositionId",
                        column: x => x.ProposedPositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "EmployeeKpiItems",
                schema: "talent",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeKpiEvaluationId = table.Column<int>(type: "int", nullable: false),
                    GoalTitle = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    Perspective = table.Column<string>(type: "varchar(30)", nullable: false),
                    MeasureUnit = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    WeightPercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TargetValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualValue = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompletionRatePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    SelfScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ManagerScore = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    EvidenceNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmployeeKpiItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EmployeeKpiItems_EmployeeKpiEvaluations_EmployeeKpiEvaluationId",
                        column: x => x.EmployeeKpiEvaluationId,
                        principalSchema: "talent",
                        principalTable: "EmployeeKpiEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ContractAddendums",
                schema: "core",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    LaborContractId = table.Column<int>(type: "int", nullable: false),
                    AddendumNumber = table.Column<string>(type: "varchar(50)", nullable: false),
                    SignDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ChangeType = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    NewBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    NewAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ContentSummary = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SignedFileUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ContractAddendums", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ContractAddendums_LaborContracts_LaborContractId",
                        column: x => x.LaborContractId,
                        principalSchema: "core",
                        principalTable: "LaborContracts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecords",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollPeriodId = table.Column<int>(type: "int", nullable: false),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: false),
                    PositionId = table.Column<int>(type: "int", nullable: false),
                    MonthlyAttendanceSummaryId = table.Column<int>(type: "int", nullable: true),
                    StandardWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    PayableWorkdays = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    TotalOtHours = table.Column<decimal>(type: "decimal(5,2)", nullable: false),
                    ContractBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ActualBaseSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PositionAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OtherAllowance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OvertimePay = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    KpiBonus = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    GrossIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    InsuranceSalaryBase = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeeSocialInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeeHealthInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    EmployeeUnemploymentInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TotalEmployeeInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanySocialInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanyHealthInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanyUnemploymentInsurance = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CompanyUnionFee = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DependentCount = table.Column<int>(type: "int", nullable: false),
                    SelfDeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DependentDeductionAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    TaxableIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    AssessableIncome = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PersonalIncomeTax = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    DisciplineDeduction = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    NetSalary = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentStatus = table.Column<string>(type: "varchar(30)", nullable: false, defaultValue: "Paid"),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalSchema: "core",
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_MonthlyAttendanceSummaries_MonthlyAttendanceSummaryId",
                        column: x => x.MonthlyAttendanceSummaryId,
                        principalSchema: "time",
                        principalTable: "MonthlyAttendanceSummaries",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_PayrollPeriods_PayrollPeriodId",
                        column: x => x.PayrollPeriodId,
                        principalSchema: "payroll",
                        principalTable: "PayrollPeriods",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecords_Positions_PositionId",
                        column: x => x.PositionId,
                        principalSchema: "core",
                        principalTable: "Positions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RecipientUserId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Message = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    NotificationType = table.Column<string>(type: "varchar(50)", nullable: false),
                    TargetUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    ReadAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Users_RecipientUserId",
                        column: x => x.RecipientUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserDataScopes",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    OrganizationUnitId = table.Column<int>(type: "int", nullable: false),
                    IncludeSubUnits = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserDataScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserDataScopes_OrganizationUnits_OrganizationUnitId",
                        column: x => x.OrganizationUnitId,
                        principalSchema: "core",
                        principalTable: "OrganizationUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserDataScopes_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserRoles",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RoleId = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRoles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserRoles_Roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identity",
                        principalTable: "Roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_UserRoles_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    RefreshTokenHash = table.Column<string>(type: "varchar(500)", nullable: false),
                    IpAddress = table.Column<string>(type: "varchar(64)", nullable: true),
                    UserAgent = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserSessions_Users_UserId",
                        column: x => x.UserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkflowApprovalLogs",
                schema: "system",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    WorkflowInstanceId = table.Column<int>(type: "int", nullable: false),
                    StepOrder = table.Column<int>(type: "int", nullable: false),
                    StepName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    ApproverUserId = table.Column<int>(type: "int", nullable: false),
                    Action = table.Column<string>(type: "varchar(20)", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ActionAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkflowApprovalLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovalLogs_Users_ApproverUserId",
                        column: x => x.ApproverUserId,
                        principalSchema: "identity",
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_WorkflowApprovalLogs_WorkflowInstances_WorkflowInstanceId",
                        column: x => x.WorkflowInstanceId,
                        principalSchema: "system",
                        principalTable: "WorkflowInstances",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PayrollRecordDetails",
                schema: "payroll",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PayrollRecordId = table.Column<long>(type: "bigint", nullable: false),
                    SalaryComponentId = table.Column<int>(type: "int", nullable: false),
                    ComponentCode = table.Column<string>(type: "varchar(50)", nullable: false),
                    ComponentName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    CalculationNote = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: false, defaultValueSql: "SYSUTCDATETIME()"),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false, defaultValue: "SYSTEM"),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2(7)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false, defaultValue: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRecordDetails", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRecordDetails_PayrollRecords_PayrollRecordId",
                        column: x => x.PayrollRecordId,
                        principalSchema: "payroll",
                        principalTable: "PayrollRecords",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRecordDetails_SalaryComponents_SalaryComponentId",
                        column: x => x.SalaryComponentId,
                        principalSchema: "payroll",
                        principalTable: "SalaryComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_EmployeeId",
                schema: "system",
                table: "AiRecommendations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_OrganizationUnitId",
                schema: "system",
                table: "AiRecommendations",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_AiRecommendations_Type_Priority",
                schema: "system",
                table: "AiRecommendations",
                columns: new[] { "RecommendationType", "PriorityLevel", "Status" });

            migrationBuilder.CreateIndex(
                name: "UQ_Assets_AssetCode",
                schema: "talent",
                table: "Assets",
                column: "AssetCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceLogs_Emp_PunchTime",
                schema: "time",
                table: "AttendanceLogs",
                columns: new[] { "EmployeeId", "PunchTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AuditLogs_Entity_CreatedAt",
                schema: "system",
                table: "AuditLogs",
                columns: new[] { "EntityName", "EntityId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_ConvertedEmployeeId",
                schema: "talent",
                table: "Candidates",
                column: "ConvertedEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_EducationLevelId",
                schema: "talent",
                table: "Candidates",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Candidates_Posting_Stage",
                schema: "talent",
                table: "Candidates",
                columns: new[] { "JobPostingId", "Stage" });

            migrationBuilder.CreateIndex(
                name: "UQ_Candidates_CandidateCode",
                schema: "talent",
                table: "Candidates",
                column: "CandidateCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ContractAddendums_LaborContractId",
                schema: "core",
                table: "ContractAddendums",
                column: "LaborContractId");

            migrationBuilder.CreateIndex(
                name: "UQ_ContractAddendums_AddendumNumber",
                schema: "core",
                table: "ContractAddendums",
                column: "AddendumNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_Date_Symbol",
                schema: "time",
                table: "DailyAttendances",
                columns: new[] { "AttendanceDate", "AttendanceSymbol" });

            migrationBuilder.CreateIndex(
                name: "IX_DailyAttendances_WorkShiftId",
                schema: "time",
                table: "DailyAttendances",
                column: "WorkShiftId");

            migrationBuilder.CreateIndex(
                name: "UQ_DailyAttendances_Employee_Date",
                schema: "time",
                table: "DailyAttendances",
                columns: new[] { "EmployeeId", "AttendanceDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_DataPrivacyConsents_Emp_Version",
                schema: "identity",
                table: "DataPrivacyConsents",
                columns: new[] { "EmployeeId", "PolicyVersion" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_EducationLevels_LevelCode",
                schema: "core",
                table: "EducationLevels",
                column: "LevelCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_AssetId",
                schema: "talent",
                table: "EmployeeAssetAssignments",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeAssetAssignments_Emp_Status",
                schema: "talent",
                table: "EmployeeAssetAssignments",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeCertificates_EmployeeId",
                schema: "core",
                table: "EmployeeCertificates",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDependents_Emp_Tax",
                schema: "core",
                table: "EmployeeDependents",
                columns: new[] { "EmployeeId", "IsTaxDeductible" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeDocuments_EmployeeId",
                schema: "core",
                table: "EmployeeDocuments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeKpiEvaluations_EmployeeId",
                schema: "talent",
                table: "EmployeeKpiEvaluations",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeKpiEvaluations_ReviewerEmployeeId",
                schema: "talent",
                table: "EmployeeKpiEvaluations",
                column: "ReviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_EmployeeKpiEvaluations_Cycle_Emp",
                schema: "talent",
                table: "EmployeeKpiEvaluations",
                columns: new[] { "EvaluationCycleId", "EmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeKpiItems_EvaluationId",
                schema: "talent",
                table: "EmployeeKpiItems",
                column: "EmployeeKpiEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnboardingTasks_Emp_Type",
                schema: "talent",
                table: "EmployeeOnboardingTasks",
                columns: new[] { "EmployeeId", "ProcessType" });

            migrationBuilder.CreateIndex(
                name: "IX_EmployeeOnboardingTasks_OnboardingTemplateId",
                schema: "talent",
                table: "EmployeeOnboardingTasks",
                column: "OnboardingTemplateId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_DirectManagerId",
                schema: "core",
                table: "Employees",
                column: "DirectManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_EducationLevelId",
                schema: "core",
                table: "Employees",
                column: "EducationLevelId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_FullName",
                schema: "core",
                table: "Employees",
                column: "FullName");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_PositionId",
                schema: "core",
                table: "Employees",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_SalaryStepId",
                schema: "core",
                table: "Employees",
                column: "SalaryStepId");

            migrationBuilder.CreateIndex(
                name: "IX_Employees_Unit_Status_HireDate",
                schema: "core",
                table: "Employees",
                columns: new[] { "OrganizationUnitId", "WorkStatus", "HireDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_CitizenIdHash",
                schema: "core",
                table: "Employees",
                column: "CitizenIdHash",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_EmployeeCode",
                schema: "core",
                table: "Employees",
                column: "EmployeeCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Employees_WorkEmail",
                schema: "core",
                table: "Employees",
                column: "WorkEmail",
                unique: true,
                filter: "[WorkEmail] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_EmploymentHistories_EmployeeId",
                schema: "core",
                table: "EmploymentHistories",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_EvaluationCycles_CycleCode",
                schema: "talent",
                table: "EvaluationCycles",
                column: "CycleCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Holidays_HolidayDate",
                schema: "time",
                table: "Holidays",
                column: "HolidayDate",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_CandidateId",
                schema: "talent",
                table: "Interviews",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_Interviews_InterviewerEmployeeId",
                schema: "talent",
                table: "Interviews",
                column: "InterviewerEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_CandidateId",
                schema: "talent",
                table: "JobOffers",
                column: "CandidateId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ProposedPositionId",
                schema: "talent",
                table: "JobOffers",
                column: "ProposedPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobOffers_ProposedUnitId",
                schema: "talent",
                table: "JobOffers",
                column: "ProposedUnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_JobOffers_OfferCode",
                schema: "talent",
                table: "JobOffers",
                column: "OfferCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_OrganizationUnitId",
                schema: "talent",
                table: "JobPostings",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_PositionId",
                schema: "talent",
                table: "JobPostings",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_JobPostings_RecruitmentRequestId",
                schema: "talent",
                table: "JobPostings",
                column: "RecruitmentRequestId");

            migrationBuilder.CreateIndex(
                name: "UQ_JobPostings_PostingCode",
                schema: "talent",
                table: "JobPostings",
                column: "PostingCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LaborContracts_EmployeeId",
                schema: "core",
                table: "LaborContracts",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborContracts_SalaryStepId",
                schema: "core",
                table: "LaborContracts",
                column: "SalaryStepId");

            migrationBuilder.CreateIndex(
                name: "IX_LaborContracts_Status_EndDate",
                schema: "core",
                table: "LaborContracts",
                columns: new[] { "Status", "EndDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_LaborContracts_ContractNumber",
                schema: "core",
                table: "LaborContracts",
                column: "ContractNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveBalances_LeaveTypeId",
                schema: "time",
                table: "LeaveBalances",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveBalances_Emp_Type_Year",
                schema: "time",
                table: "LeaveBalances",
                columns: new[] { "EmployeeId", "LeaveTypeId", "Year" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_Emp_Start_Status",
                schema: "time",
                table: "LeaveRequests",
                columns: new[] { "EmployeeId", "StartDate", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveRequests_LeaveTypeId",
                schema: "time",
                table: "LeaveRequests",
                column: "LeaveTypeId");

            migrationBuilder.CreateIndex(
                name: "UQ_LeaveTypes_Code",
                schema: "time",
                table: "LeaveTypes",
                column: "LeaveTypeCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_MenuItems_MenuCode",
                schema: "system",
                table: "MenuItems",
                column: "MenuCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_MonthlyAttendanceSummaries_Emp_Month_Year",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                columns: new[] { "EmployeeId", "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Recipient_Read",
                schema: "system",
                table: "Notifications",
                columns: new[] { "RecipientUserId", "IsRead", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationUnits_ParentUnitId",
                schema: "core",
                table: "OrganizationUnits",
                column: "ParentUnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_OrganizationUnits_UnitCode",
                schema: "core",
                table: "OrganizationUnits",
                column: "UnitCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_OvertimeRequests_Emp_Date",
                schema: "time",
                table: "OvertimeRequests",
                columns: new[] { "EmployeeId", "OvertimeDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollParameters_ParamCode",
                schema: "payroll",
                table: "PayrollParameters",
                column: "ParamCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_PeriodCode",
                schema: "payroll",
                table: "PayrollPeriods",
                column: "PeriodCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollPeriods_Year_Month",
                schema: "payroll",
                table: "PayrollPeriods",
                columns: new[] { "Year", "Month" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecordDetails_PayrollRecordId",
                schema: "payroll",
                table: "PayrollRecordDetails",
                column: "PayrollRecordId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecordDetails_SalaryComponentId",
                schema: "payroll",
                table: "PayrollRecordDetails",
                column: "SalaryComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_EmployeeId",
                schema: "payroll",
                table: "PayrollRecords",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_MonthlyAttendanceSummaryId",
                schema: "payroll",
                table: "PayrollRecords",
                column: "MonthlyAttendanceSummaryId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_PositionId",
                schema: "payroll",
                table: "PayrollRecords",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRecords_Unit_Period",
                schema: "payroll",
                table: "PayrollRecords",
                columns: new[] { "OrganizationUnitId", "PayrollPeriodId" });

            migrationBuilder.CreateIndex(
                name: "UQ_PayrollRecords_Period_Employee",
                schema: "payroll",
                table: "PayrollRecords",
                columns: new[] { "PayrollPeriodId", "EmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Permissions_PermissionCode",
                schema: "identity",
                table: "Permissions",
                column: "PermissionCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTransfers_Emp_Date",
                schema: "core",
                table: "PersonnelTransfers",
                columns: new[] { "EmployeeId", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTransfers_FromPositionId",
                schema: "core",
                table: "PersonnelTransfers",
                column: "FromPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTransfers_FromUnitId",
                schema: "core",
                table: "PersonnelTransfers",
                column: "FromUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTransfers_ToPositionId",
                schema: "core",
                table: "PersonnelTransfers",
                column: "ToPositionId");

            migrationBuilder.CreateIndex(
                name: "IX_PersonnelTransfers_ToUnitId",
                schema: "core",
                table: "PersonnelTransfers",
                column: "ToUnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_PersonnelTransfers_DecisionNumber",
                schema: "core",
                table: "PersonnelTransfers",
                column: "DecisionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Positions_PositionCode",
                schema: "core",
                table: "Positions",
                column: "PositionCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentRequests_OrganizationUnitId",
                schema: "talent",
                table: "RecruitmentRequests",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_RecruitmentRequests_PositionId",
                schema: "talent",
                table: "RecruitmentRequests",
                column: "PositionId");

            migrationBuilder.CreateIndex(
                name: "UQ_RecruitmentRequests_RequestCode",
                schema: "talent",
                table: "RecruitmentRequests",
                column: "RequestCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_RewardDisciplineCategories_Code",
                schema: "core",
                table: "RewardDisciplineCategories",
                column: "CategoryCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RewardDisciplines_CategoryId",
                schema: "core",
                table: "RewardDisciplines",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_RewardDisciplines_Emp_Type_Date",
                schema: "core",
                table: "RewardDisciplines",
                columns: new[] { "EmployeeId", "Type", "EffectiveDate" });

            migrationBuilder.CreateIndex(
                name: "UQ_RewardDisciplines_DecisionNumber",
                schema: "core",
                table: "RewardDisciplines",
                column: "DecisionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermissions_PermissionId",
                schema: "identity",
                table: "RolePermissions",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "UQ_RolePermissions_Role_Perm",
                schema: "identity",
                table: "RolePermissions",
                columns: new[] { "RoleId", "PermissionId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Roles_RoleCode",
                schema: "identity",
                table: "Roles",
                column: "RoleCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_SalaryComponents_ComponentCode",
                schema: "payroll",
                table: "SalaryComponents",
                column: "ComponentCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProgressions_EmployeeId",
                schema: "core",
                table: "SalaryProgressions",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProgressions_NewSalaryStepId",
                schema: "core",
                table: "SalaryProgressions",
                column: "NewSalaryStepId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProgressions_OldSalaryStepId",
                schema: "core",
                table: "SalaryProgressions",
                column: "OldSalaryStepId");

            migrationBuilder.CreateIndex(
                name: "UQ_SalaryProgressions_DecisionNumber",
                schema: "core",
                table: "SalaryProgressions",
                column: "DecisionNumber",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_SalaryScales_ScaleCode",
                schema: "core",
                table: "SalaryScales",
                column: "ScaleCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_SalarySteps_Scale_Order",
                schema: "core",
                table: "SalarySteps",
                columns: new[] { "SalaryScaleId", "StepOrder" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_ShiftSchedules_WorkShiftId",
                schema: "time",
                table: "ShiftSchedules",
                column: "WorkShiftId");

            migrationBuilder.CreateIndex(
                name: "UQ_ShiftSchedules_Emp_Date",
                schema: "time",
                table: "ShiftSchedules",
                columns: new[] { "EmployeeId", "WorkDate" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_TaxBrackets_BracketLevel",
                schema: "payroll",
                table: "TaxBrackets",
                column: "BracketLevel",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingClasses_TrainingCourseId",
                schema: "talent",
                table: "TrainingClasses",
                column: "TrainingCourseId");

            migrationBuilder.CreateIndex(
                name: "UQ_TrainingClasses_ClassCode",
                schema: "talent",
                table: "TrainingClasses",
                column: "ClassCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_TrainingCourses_CourseCode",
                schema: "talent",
                table: "TrainingCourses",
                column: "CourseCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_TrainingEnrollments_EmployeeId",
                schema: "talent",
                table: "TrainingEnrollments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "UQ_TrainingEnrollments_Class_Employee",
                schema: "talent",
                table: "TrainingEnrollments",
                columns: new[] { "TrainingClassId", "EmployeeId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserDataScopes_OrganizationUnitId",
                schema: "identity",
                table: "UserDataScopes",
                column: "OrganizationUnitId");

            migrationBuilder.CreateIndex(
                name: "UQ_UserDataScopes_User_Unit",
                schema: "identity",
                table: "UserDataScopes",
                columns: new[] { "UserId", "OrganizationUnitId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserRoles_RoleId",
                schema: "identity",
                table: "UserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "UQ_UserRoles_User_Role",
                schema: "identity",
                table: "UserRoles",
                columns: new[] { "UserId", "RoleId" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Email",
                schema: "identity",
                table: "Users",
                column: "Email",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_EmployeeId",
                schema: "identity",
                table: "Users",
                column: "EmployeeId",
                unique: true,
                filter: "[EmployeeId] IS NOT NULL AND [IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_Users_Username",
                schema: "identity",
                table: "Users",
                column: "Username",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_UserSessions_UserId",
                schema: "identity",
                table: "UserSessions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovalLogs_ApproverUserId",
                schema: "system",
                table: "WorkflowApprovalLogs",
                column: "ApproverUserId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowApprovalLogs_InstanceId",
                schema: "system",
                table: "WorkflowApprovalLogs",
                column: "WorkflowInstanceId");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkflowDefinitions_WorkflowCode",
                schema: "system",
                table: "WorkflowDefinitions",
                column: "WorkflowCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_Entity",
                schema: "system",
                table: "WorkflowInstances",
                columns: new[] { "EntityType", "EntityId" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_InitiatorEmployeeId",
                schema: "system",
                table: "WorkflowInstances",
                column: "InitiatorEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkflowInstances_WorkflowDefinitionId",
                schema: "system",
                table: "WorkflowInstances",
                column: "WorkflowDefinitionId");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkflowSteps_Def_Order",
                schema: "system",
                table: "WorkflowSteps",
                columns: new[] { "WorkflowDefinitionId", "StepOrder" },
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.CreateIndex(
                name: "UQ_WorkShifts_ShiftCode",
                schema: "time",
                table: "WorkShifts",
                column: "ShiftCode",
                unique: true,
                filter: "[IsDeleted] = 0");

            migrationBuilder.Sql(Vinamilk.HRM.Infrastructure.Persistence.SqlObjects.ReportingSqlObjects.CreateDepartmentHeadcountViewSql);
            migrationBuilder.Sql(Vinamilk.HRM.Infrastructure.Persistence.SqlObjects.ReportingSqlObjects.CreateMonthlyPayrollFundViewSql);
            migrationBuilder.Sql(Vinamilk.HRM.Infrastructure.Persistence.SqlObjects.ReportingSqlObjects.CreateSpMonthlyHeadcountAndTurnoverSql);
            migrationBuilder.Sql(Vinamilk.HRM.Infrastructure.Persistence.SqlObjects.ReportingSqlObjects.CreateSpPayrollFundAnalysisSql);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(Vinamilk.HRM.Infrastructure.Persistence.SqlObjects.ReportingSqlObjects.DropAllSqlObjects);

            migrationBuilder.DropTable(
                name: "AiRecommendations",
                schema: "system");

            migrationBuilder.DropTable(
                name: "AttendanceLogs",
                schema: "time");

            migrationBuilder.DropTable(
                name: "AuditLogs",
                schema: "system");

            migrationBuilder.DropTable(
                name: "ContractAddendums",
                schema: "core");

            migrationBuilder.DropTable(
                name: "DailyAttendances",
                schema: "time");

            migrationBuilder.DropTable(
                name: "DataPrivacyConsents",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "EmployeeAssetAssignments",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "EmployeeCertificates",
                schema: "core");

            migrationBuilder.DropTable(
                name: "EmployeeDependents",
                schema: "core");

            migrationBuilder.DropTable(
                name: "EmployeeDocuments",
                schema: "core");

            migrationBuilder.DropTable(
                name: "EmployeeKpiItems",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "EmployeeOnboardingTasks",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "EmploymentHistories",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Holidays",
                schema: "time");

            migrationBuilder.DropTable(
                name: "Interviews",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "JobOffers",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "LeaveBalances",
                schema: "time");

            migrationBuilder.DropTable(
                name: "LeaveRequests",
                schema: "time");

            migrationBuilder.DropTable(
                name: "MenuItems",
                schema: "system");

            migrationBuilder.DropTable(
                name: "Notifications",
                schema: "system");

            migrationBuilder.DropTable(
                name: "OvertimeRequests",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PayrollParameters",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PayrollRecordDetails",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "PersonnelTransfers",
                schema: "core");

            migrationBuilder.DropTable(
                name: "RewardDisciplines",
                schema: "core");

            migrationBuilder.DropTable(
                name: "RolePermissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "SalaryProgressions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "ShiftSchedules",
                schema: "time");

            migrationBuilder.DropTable(
                name: "TaxBrackets",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "TrainingEnrollments",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "UserDataScopes",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserRoles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "UserSessions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "WorkflowApprovalLogs",
                schema: "system");

            migrationBuilder.DropTable(
                name: "WorkflowSteps",
                schema: "system");

            migrationBuilder.DropTable(
                name: "LaborContracts",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Assets",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "EmployeeKpiEvaluations",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "OnboardingTemplates",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "Candidates",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "LeaveTypes",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PayrollRecords",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "SalaryComponents",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "RewardDisciplineCategories",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Permissions",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "WorkShifts",
                schema: "time");

            migrationBuilder.DropTable(
                name: "TrainingClasses",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "Roles",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "Users",
                schema: "identity");

            migrationBuilder.DropTable(
                name: "WorkflowInstances",
                schema: "system");

            migrationBuilder.DropTable(
                name: "EvaluationCycles",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "JobPostings",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "MonthlyAttendanceSummaries",
                schema: "time");

            migrationBuilder.DropTable(
                name: "PayrollPeriods",
                schema: "payroll");

            migrationBuilder.DropTable(
                name: "TrainingCourses",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "WorkflowDefinitions",
                schema: "system");

            migrationBuilder.DropTable(
                name: "RecruitmentRequests",
                schema: "talent");

            migrationBuilder.DropTable(
                name: "Employees",
                schema: "core");

            migrationBuilder.DropTable(
                name: "EducationLevels",
                schema: "core");

            migrationBuilder.DropTable(
                name: "OrganizationUnits",
                schema: "core");

            migrationBuilder.DropTable(
                name: "Positions",
                schema: "core");

            migrationBuilder.DropTable(
                name: "SalarySteps",
                schema: "core");

            migrationBuilder.DropTable(
                name: "SalaryScales",
                schema: "core");
        }
    }
}
