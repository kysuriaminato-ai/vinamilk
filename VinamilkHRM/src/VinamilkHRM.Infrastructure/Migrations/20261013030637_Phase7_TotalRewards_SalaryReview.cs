using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinamilkHRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase7_TotalRewards_SalaryReview : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AdjustmentType",
                table: "SalaryProposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "AnnualKpiGrade",
                table: "SalaryProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "AnnualKpiScore",
                table: "SalaryProposals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "CompaRatioAfter",
                table: "SalaryProposals",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "CompaRatioBefore",
                table: "SalaryProposals",
                type: "decimal(5,4)",
                precision: 5,
                scale: 4,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "ConflictReason",
                table: "SalaryProposals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Discriminator",
                table: "SalaryProposals",
                type: "nvarchar(21)",
                maxLength: 21,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Evidence",
                table: "SalaryProposals",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FundSource",
                table: "SalaryProposals",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<decimal>(
                name: "IncreasePercentage",
                table: "SalaryProposals",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<bool>(
                name: "IsConflictOfInterestFlagged",
                table: "SalaryProposals",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OrganizationalUnitId",
                table: "SalaryProposals",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SalaryReviewCycleId",
                table: "SalaryProposals",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SalaryAdjustmentApprovalSteps",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryProposalId = table.Column<int>(type: "int", nullable: false),
                    StepNumber = table.Column<int>(type: "int", nullable: false),
                    RoleLevel = table.Column<int>(type: "int", nullable: false),
                    RoleName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    ApproverId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Comment = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsConflictOfInterestFlagged = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryAdjustmentApprovalSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentApprovalSteps_Employees_ApproverId",
                        column: x => x.ApproverId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryAdjustmentApprovalSteps_SalaryProposals_SalaryProposalId",
                        column: x => x.SalaryProposalId,
                        principalTable: "SalaryProposals",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SalaryBands",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    JobGradeId = table.Column<int>(type: "int", nullable: false),
                    Min = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Mid = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Max = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryBands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryBands_JobGrades_JobGradeId",
                        column: x => x.JobGradeId,
                        principalTable: "JobGrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SalaryReviewCycles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Year = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    EffectiveDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MinTenureMonths = table.Column<int>(type: "int", nullable: false),
                    ExcludeDisciplined = table.Column<bool>(type: "bit", nullable: false),
                    ExcludeRecentPromotionMonths = table.Column<int>(type: "int", nullable: false),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryReviewCycles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "SalaryBudgets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    SalaryReviewCycleId = table.Column<int>(type: "int", nullable: false),
                    OrganizationalUnitId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Used = table.Column<decimal>(type: "decimal(18,0)", precision: 18, scale: 0, nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalaryBudgets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SalaryBudgets_OrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SalaryBudgets_SalaryReviewCycles_SalaryReviewCycleId",
                        column: x => x.SalaryReviewCycleId,
                        principalTable: "SalaryReviewCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProposals_OrganizationalUnitId",
                table: "SalaryProposals",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryProposals_SalaryReviewCycleId",
                table: "SalaryProposals",
                column: "SalaryReviewCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentApprovalSteps_ApproverId",
                table: "SalaryAdjustmentApprovalSteps",
                column: "ApproverId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryAdjustmentApprovalSteps_SalaryProposalId",
                table: "SalaryAdjustmentApprovalSteps",
                column: "SalaryProposalId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryBands_JobGradeId",
                table: "SalaryBands",
                column: "JobGradeId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryBudgets_OrganizationalUnitId",
                table: "SalaryBudgets",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_SalaryBudgets_SalaryReviewCycleId",
                table: "SalaryBudgets",
                column: "SalaryReviewCycleId");

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryProposals_OrganizationalUnits_OrganizationalUnitId",
                table: "SalaryProposals",
                column: "OrganizationalUnitId",
                principalTable: "OrganizationalUnits",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalaryProposals_SalaryReviewCycles_SalaryReviewCycleId",
                table: "SalaryProposals",
                column: "SalaryReviewCycleId",
                principalTable: "SalaryReviewCycles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalaryProposals_OrganizationalUnits_OrganizationalUnitId",
                table: "SalaryProposals");

            migrationBuilder.DropForeignKey(
                name: "FK_SalaryProposals_SalaryReviewCycles_SalaryReviewCycleId",
                table: "SalaryProposals");

            migrationBuilder.DropTable(
                name: "SalaryAdjustmentApprovalSteps");

            migrationBuilder.DropTable(
                name: "SalaryBands");

            migrationBuilder.DropTable(
                name: "SalaryBudgets");

            migrationBuilder.DropTable(
                name: "SalaryReviewCycles");

            migrationBuilder.DropIndex(
                name: "IX_SalaryProposals_OrganizationalUnitId",
                table: "SalaryProposals");

            migrationBuilder.DropIndex(
                name: "IX_SalaryProposals_SalaryReviewCycleId",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "AdjustmentType",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "AnnualKpiGrade",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "AnnualKpiScore",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "CompaRatioAfter",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "CompaRatioBefore",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "ConflictReason",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "Discriminator",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "Evidence",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "FundSource",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "IncreasePercentage",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "IsConflictOfInterestFlagged",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "OrganizationalUnitId",
                table: "SalaryProposals");

            migrationBuilder.DropColumn(
                name: "SalaryReviewCycleId",
                table: "SalaryProposals");
        }
    }
}
