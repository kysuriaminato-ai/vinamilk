using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinamilkHRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase6_PayrollEngine_3P_Statutory : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "EmployerCost",
                table: "PaySlips",
                type: "decimal(18,0)",
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "FormulaTraceJson",
                table: "PaySlips",
                type: "nvarchar(4000)",
                maxLength: 4000,
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "PayrollRunId",
                table: "PaySlips",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "PayrollAdjustments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<int>(type: "int", nullable: false),
                    SourcePeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    TargetPeriod = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    AdjustmentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    ApprovedBy = table.Column<int>(type: "int", nullable: true),
                    ApprovedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsApplied = table.Column<bool>(type: "bit", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollAdjustments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_Employees_ApprovedBy",
                        column: x => x.ApprovedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollAdjustments_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PayrollAdjustmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateTable(
                name: "PayrollRuns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Period = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    OrganizationalUnitId = table.Column<int>(type: "int", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    TotalGross = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalNet = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalEmployerCost = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalPersonalIncomeTax = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalEmployeeInsurance = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    TotalEmployerInsurance = table.Column<decimal>(type: "decimal(18,0)", nullable: false),
                    EmployeeCount = table.Column<int>(type: "int", nullable: false),
                    SubmittedBy = table.Column<int>(type: "int", nullable: true),
                    SubmittedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByChiefAccountant = table.Column<int>(type: "int", nullable: true),
                    ApprovedAccountantAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ApprovedByHrDirector = table.Column<int>(type: "int", nullable: true),
                    ApprovedDirectorAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockedBy = table.Column<int>(type: "int", nullable: true),
                    LockedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaidBy = table.Column<int>(type: "int", nullable: true),
                    PaidAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodEndColumn", true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false)
                        .Annotation("SqlServer:TemporalIsPeriodStartColumn", true),
                    RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    UpdatedBy = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayrollRuns", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Employees_ApprovedByChiefAccountant",
                        column: x => x.ApprovedByChiefAccountant,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Employees_ApprovedByHrDirector",
                        column: x => x.ApprovedByHrDirector,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Employees_LockedBy",
                        column: x => x.LockedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Employees_PaidBy",
                        column: x => x.PaidBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_Employees_SubmittedBy",
                        column: x => x.SubmittedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_PayrollRuns_OrganizationalUnits_OrganizationalUnitId",
                        column: x => x.OrganizationalUnitId,
                        principalTable: "OrganizationalUnits",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PayrollRunsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.CreateIndex(
                name: "IX_PaySlips_PayrollRunId",
                table: "PaySlips",
                column: "PayrollRunId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_ApprovedBy",
                table: "PayrollAdjustments",
                column: "ApprovedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_EmployeeId",
                table: "PayrollAdjustments",
                column: "EmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollAdjustments_TargetPeriod",
                table: "PayrollAdjustments",
                column: "TargetPeriod");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_ApprovedByChiefAccountant",
                table: "PayrollRuns",
                column: "ApprovedByChiefAccountant");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_ApprovedByHrDirector",
                table: "PayrollRuns",
                column: "ApprovedByHrDirector");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_LockedBy",
                table: "PayrollRuns",
                column: "LockedBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_OrganizationalUnitId",
                table: "PayrollRuns",
                column: "OrganizationalUnitId");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_PaidBy",
                table: "PayrollRuns",
                column: "PaidBy");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_Period",
                table: "PayrollRuns",
                column: "Period");

            migrationBuilder.CreateIndex(
                name: "IX_PayrollRuns_SubmittedBy",
                table: "PayrollRuns",
                column: "SubmittedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_PaySlips_PayrollRuns_PayrollRunId",
                table: "PaySlips",
                column: "PayrollRunId",
                principalTable: "PayrollRuns",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaySlips_PayrollRuns_PayrollRunId",
                table: "PaySlips");

            migrationBuilder.DropTable(
                name: "PayrollAdjustments")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PayrollAdjustmentsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropTable(
                name: "PayrollRuns")
                .Annotation("SqlServer:IsTemporal", true)
                .Annotation("SqlServer:TemporalHistoryTableName", "PayrollRunsHistory")
                .Annotation("SqlServer:TemporalHistoryTableSchema", null)
                .Annotation("SqlServer:TemporalPeriodEndColumnName", "PeriodEnd")
                .Annotation("SqlServer:TemporalPeriodStartColumnName", "PeriodStart");

            migrationBuilder.DropIndex(
                name: "IX_PaySlips_PayrollRunId",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "EmployerCost",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "FormulaTraceJson",
                table: "PaySlips");

            migrationBuilder.DropColumn(
                name: "PayrollRunId",
                table: "PaySlips");
        }
    }
}
