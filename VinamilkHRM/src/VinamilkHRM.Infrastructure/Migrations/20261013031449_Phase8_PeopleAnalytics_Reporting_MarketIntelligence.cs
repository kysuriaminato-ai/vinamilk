using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinamilkHRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase8_PeopleAnalytics_Reporting_MarketIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "rpt");

            migrationBuilder.CreateTable(
                name: "AiAuditLogs",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FeatureName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    PromptSent = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ResponseReceived = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ModelName = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsAnonymized = table.Column<bool>(type: "bit", nullable: false),
                    TriggeredByUserId = table.Column<int>(type: "int", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CompanyMetrics",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Quarter = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Revenue = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    NetProfitAfterTax = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    ProductionVolumeLiters = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyMetrics", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExitSurveys",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    ResignationDate = table.Column<DateOnly>(type: "date", nullable: false),
                    ReasonCategory = table.Column<int>(type: "int", nullable: false),
                    DetailedReason = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    CompensationSatisfaction = table.Column<int>(type: "int", nullable: false),
                    ManagementSatisfaction = table.Column<int>(type: "int", nullable: false),
                    WorkEnvironmentSatisfaction = table.Column<int>(type: "int", nullable: false),
                    CareerGrowthSatisfaction = table.Column<int>(type: "int", nullable: false),
                    WouldRecommendCompany = table.Column<bool>(type: "bit", nullable: false),
                    ImprovementSuggestions = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExitSurveys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExitSurveys_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MacroIndicators",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Code = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Value = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    SourceUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MacroIndicators", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MarketSalaryBenchmarks",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SurveySource = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Year = table.Column<int>(type: "int", nullable: false),
                    JobFamilyId = table.Column<int>(type: "int", nullable: false),
                    JobGradeId = table.Column<int>(type: "int", nullable: false),
                    Region = table.Column<int>(type: "int", nullable: false),
                    P25 = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    P50 = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    P75 = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketSalaryBenchmarks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MarketSalaryBenchmarks_JobFamilies_JobFamilyId",
                        column: x => x.JobFamilyId,
                        principalTable: "JobFamilies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_MarketSalaryBenchmarks_JobGrades_JobGradeId",
                        column: x => x.JobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "WorkforceSnapshots",
                schema: "rpt",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Month = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: false),
                    OrganizationalUnitId = table.Column<int>(type: "int", nullable: false),
                    StartHeadcount = table.Column<int>(type: "int", nullable: false),
                    EndHeadcount = table.Column<int>(type: "int", nullable: false),
                    AverageHeadcount = table.Column<decimal>(type: "decimal(10,2)", precision: 10, scale: 2, nullable: false),
                    Hires = table.Column<int>(type: "int", nullable: false),
                    Exits = table.Column<int>(type: "int", nullable: false),
                    ExitsByReasonJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TransfersIn = table.Column<int>(type: "int", nullable: false),
                    TransfersOut = table.Column<int>(type: "int", nullable: false),
                    Promotions = table.Column<int>(type: "int", nullable: false),
                    AbsenceRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    OvertimeHours = table.Column<decimal>(type: "decimal(10,1)", precision: 10, scale: 1, nullable: false),
                    LaborCost = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    AvgKpi = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    GradeDistributionJson = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SnapshotDate = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    IsSampleData = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkforceSnapshots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_WorkforceSnapshots_OrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CompanyMetrics_Quarter",
                schema: "rpt",
                table: "CompanyMetrics",
                column: "Quarter",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExitSurveys_EmployeeId",
                schema: "rpt",
                table: "ExitSurveys",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_MacroIndicators_Code_Period",
                schema: "rpt",
                table: "MacroIndicators",
                columns: new[] { "Code", "Period" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_MarketSalaryBenchmarks_JobFamilyId",
                schema: "rpt",
                table: "MarketSalaryBenchmarks",
                column: "JobFamilyId");

            migrationBuilder.CreateIndex(
                name: "IX_MarketSalaryBenchmarks_JobGradeId",
                schema: "rpt",
                table: "MarketSalaryBenchmarks",
                column: "JobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceSnapshots_Month_OrganizationalUnitId",
                schema: "rpt",
                table: "WorkforceSnapshots",
                columns: new[] { "Month", "OrganizationalUnitId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkforceSnapshots_OrganizationalUnitId",
                schema: "rpt",
                table: "WorkforceSnapshots",
                column: "OrganizationalUnitId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiAuditLogs",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "CompanyMetrics",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "ExitSurveys",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "MacroIndicators",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "MarketSalaryBenchmarks",
                schema: "rpt");

            migrationBuilder.DropTable(
                name: "WorkforceSnapshots",
                schema: "rpt");
        }
    }
}
