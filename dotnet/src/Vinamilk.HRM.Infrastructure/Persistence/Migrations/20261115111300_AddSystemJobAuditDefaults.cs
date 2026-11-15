using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Vinamilk.HRM.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSystemJobAuditDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "WorkShifts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowSteps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowInstances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowApprovalLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserRoles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserDataScopes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingEnrollments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingCourses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingClasses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "TaxBrackets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "ShiftSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalarySteps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalaryScales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalaryProgressions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "RolePermissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "RewardDisciplines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "RewardDisciplineCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "RecruitmentRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "Positions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PitBrackets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "PersonnelTransfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayslipDispatchLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollRecordDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollParameters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollCalculationRevisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "OvertimeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "OnboardingTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "MenuItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveBalances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "LaborContracts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "JobPostings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "JobOffers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Interviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "Holidays",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EvaluationCycles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmploymentHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeOnboardingTasks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeKpiItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeKpiEvaluations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeDependents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeCertificates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeAssetAssignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EducationLevels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "DataPrivacyConsents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "DailyAttendances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "ContractAddendums",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "AiRecommendations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "system.job",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "SYSTEM");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "WorkShifts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowSteps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowInstances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowDefinitions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "WorkflowApprovalLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserSessions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Users",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserRoles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "UserDataScopes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingEnrollments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingCourses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "TrainingClasses",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "TaxBrackets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "ShiftSchedules",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalarySteps",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalaryScales",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "SalaryProgressions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "SalaryComponents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Roles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "RolePermissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "RewardDisciplines",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "RewardDisciplineCategories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "RecruitmentRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "Positions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PitBrackets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "PersonnelTransfers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "Permissions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayslipDispatchLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollStatutoryParameters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollRecords",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollRecordDetails",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollPeriods",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollParameters",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "payroll",
                table: "PayrollCalculationRevisions",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "OvertimeRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "OrganizationUnits",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "OnboardingTemplates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "Notifications",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "MonthlyAttendanceSummaries",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "MenuItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveTypes",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "LeaveBalances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "LaborContracts",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "JobPostings",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "JobOffers",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Interviews",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "Holidays",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EvaluationCycles",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmploymentHistories",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "Employees",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeOnboardingTasks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeKpiItems",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeKpiEvaluations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeDocuments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeDependents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EmployeeCertificates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "EmployeeAssetAssignments",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "EducationLevels",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "identity",
                table: "DataPrivacyConsents",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "DailyAttendances",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "core",
                table: "ContractAddendums",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Candidates",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "AuditLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "time",
                table: "AttendanceLogs",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "talent",
                table: "Assets",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                schema: "system",
                table: "AiRecommendations",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "SYSTEM",
                oldClrType: typeof(string),
                oldType: "nvarchar(100)",
                oldMaxLength: 100,
                oldDefaultValue: "system.job");
        }
    }
}
