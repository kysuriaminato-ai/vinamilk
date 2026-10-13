using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VinamilkHRM.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays");

            migrationBuilder.DropIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems");

            migrationBuilder.DropIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories");

            migrationBuilder.DropIndex(
                name: "IX_JobTitles_JobTitleCode",
                table: "JobTitles");

            migrationBuilder.DropIndex(
                name: "IX_JobGrades_GradeCode",
                table: "JobGrades");

            migrationBuilder.DropIndex(
                name: "IX_JobGrades_GradeNumber",
                table: "JobGrades");

            migrationBuilder.DropIndex(
                name: "IX_JobFamilies_FamilyCode",
                table: "JobFamilies");

            migrationBuilder.DropIndex(
                name: "IX_AllowanceTypes_Code",
                table: "AllowanceTypes");

            migrationBuilder.AddColumn<string>(
                name: "LegalReference",
                table: "OvertimeRates",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "WorkPattern",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "MinimumWageRegion",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<int>(
                name: "LineNumber",
                table: "OrganizationalUnits",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CostCenterCode",
                table: "OrganizationalUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "CC_DEFAULT",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "DivisionScope",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "FunctionDescription",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "SalaryStep",
                table: "Employees",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "CitizenIdIssuePlace",
                table: "Employees",
                type: "nvarchar(150)",
                maxLength: 150,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(250)",
                oldMaxLength: 250,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date");

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems",
                columns: new[] { "CategoryId", "Code" });

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories",
                column: "Code");

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_JobTitleCode",
                table: "JobTitles",
                column: "JobTitleCode");

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeCode",
                table: "JobGrades",
                column: "GradeCode");

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeNumber",
                table: "JobGrades",
                column: "GradeNumber");

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_FamilyCode",
                table: "JobFamilies",
                column: "FamilyCode");

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceTypes_Code",
                table: "AllowanceTypes",
                column: "Code");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays");

            migrationBuilder.DropIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems");

            migrationBuilder.DropIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories");

            migrationBuilder.DropIndex(
                name: "IX_JobTitles_JobTitleCode",
                table: "JobTitles");

            migrationBuilder.DropIndex(
                name: "IX_JobGrades_GradeCode",
                table: "JobGrades");

            migrationBuilder.DropIndex(
                name: "IX_JobGrades_GradeNumber",
                table: "JobGrades");

            migrationBuilder.DropIndex(
                name: "IX_JobFamilies_FamilyCode",
                table: "JobFamilies");

            migrationBuilder.DropIndex(
                name: "IX_AllowanceTypes_Code",
                table: "AllowanceTypes");

            migrationBuilder.DropColumn(
                name: "LegalReference",
                table: "OvertimeRates");

            migrationBuilder.DropColumn(
                name: "DivisionScope",
                table: "OrganizationalUnits");

            migrationBuilder.DropColumn(
                name: "FunctionDescription",
                table: "OrganizationalUnits");

            migrationBuilder.AlterColumn<int>(
                name: "WorkPattern",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<int>(
                name: "MinimumWageRegion",
                table: "OrganizationalUnits",
                type: "int",
                nullable: false,
                defaultValue: 1,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AlterColumn<string>(
                name: "LineNumber",
                table: "OrganizationalUnits",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CostCenterCode",
                table: "OrganizationalUnits",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(50)",
                oldMaxLength: 50,
                oldDefaultValue: "CC_DEFAULT");

            migrationBuilder.AlterColumn<int>(
                name: "SalaryStep",
                table: "Employees",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldDefaultValue: 1);

            migrationBuilder.AlterColumn<string>(
                name: "CitizenIdIssuePlace",
                table: "Employees",
                type: "nvarchar(250)",
                maxLength: 250,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(150)",
                oldMaxLength: 150,
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_PublicHolidays_Date",
                table: "PublicHolidays",
                column: "Date",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupItems_CategoryId_Code",
                table: "LookupItems",
                columns: new[] { "CategoryId", "Code" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LookupCategories_Code",
                table: "LookupCategories",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobTitles_JobTitleCode",
                table: "JobTitles",
                column: "JobTitleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeCode",
                table: "JobGrades",
                column: "GradeCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobGrades_GradeNumber",
                table: "JobGrades",
                column: "GradeNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_JobFamilies_FamilyCode",
                table: "JobFamilies",
                column: "FamilyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AllowanceTypes_Code",
                table: "AllowanceTypes",
                column: "Code",
                unique: true);
        }
    }
}
