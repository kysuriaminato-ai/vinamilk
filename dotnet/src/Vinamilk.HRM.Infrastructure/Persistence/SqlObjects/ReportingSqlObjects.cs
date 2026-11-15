namespace Vinamilk.HRM.Infrastructure.Persistence.SqlObjects;

public static class ReportingSqlObjects
{
    public const string CreateDepartmentHeadcountViewSql = @"
CREATE OR ALTER VIEW [core].[vw_DepartmentHeadcountStructure]
AS
SELECT
    u.Id AS OrganizationUnitId,
    u.UnitCode,
    u.UnitName,
    u.UnitType,
    u.UnitLevel,
    u.ParentUnitId,
    u.PlannedHeadcount,
    COUNT(e.Id) AS ActualHeadcount,
    SUM(CASE WHEN e.Gender IN (N'Nam', 'Male') THEN 1 ELSE 0 END) AS MaleCount,
    SUM(CASE WHEN e.Gender IN (N'N' + NCHAR(7919), N'Nữ', 'Female', 'Nu') THEN 1 ELSE 0 END) AS FemaleCount,
    SUM(CASE WHEN e.WorkStatus = 'Official' THEN 1 ELSE 0 END) AS OfficialCount,
    SUM(CASE WHEN e.WorkStatus = 'Probation' THEN 1 ELSE 0 END) AS ProbationCount,
    SUM(CASE WHEN e.CurrentContractType = 'Indefinite' THEN 1 ELSE 0 END) AS IndefiniteContractCount,
    SUM(CASE WHEN e.CurrentContractType IN ('Fixed12M', 'Fixed36M') THEN 1 ELSE 0 END) AS FixedTermContractCount,
    SUM(CASE WHEN ed.LevelCode IN ('TD01', 'TD02', 'TD03') THEN 1 ELSE 0 END) AS UniversityOrAboveCount,
    ISNULL(AVG(CAST(DATEDIFF(YEAR, e.DateOfBirth, GETDATE()) AS DECIMAL(5,2))), 0) AS AverageAge,
    ISNULL(SUM(e.BaseSalary + e.PositionAllowance + e.OtherAllowance), 0) AS TotalContractSalaryFund
FROM [core].[OrganizationUnits] u
LEFT JOIN [core].[Employees] e
    ON e.OrganizationUnitId = u.Id
    AND e.IsDeleted = 0
    AND e.WorkStatus NOT IN ('Resigned', 'Retired')
LEFT JOIN [core].[EducationLevels] ed
    ON e.EducationLevelId = ed.Id
    AND ed.IsDeleted = 0
WHERE u.IsDeleted = 0
GROUP BY
    u.Id, u.UnitCode, u.UnitName, u.UnitType, u.UnitLevel, u.ParentUnitId, u.PlannedHeadcount;
";

    public const string CreateMonthlyPayrollFundViewSql = @"
CREATE OR ALTER VIEW [payroll].[vw_MonthlyPayrollFundSummary]
AS
SELECT
    p.Id AS PayrollPeriodId,
    p.PeriodCode,
    p.Year,
    p.Month,
    p.Status AS PeriodStatus,
    u.Id AS OrganizationUnitId,
    u.UnitCode,
    u.UnitName,
    COUNT(r.Id) AS EmployeeCount,
    SUM(r.ActualBaseSalary) AS TotalActualBaseSalary,
    SUM(r.PositionAllowance + r.OtherAllowance) AS TotalAllowances,
    SUM(r.OvertimePay) AS TotalOvertimePay,
    SUM(r.KpiBonus) AS TotalKpiBonus,
    SUM(r.GrossIncome) AS TotalGrossIncome,
    SUM(r.TotalEmployeeInsurance) AS TotalEmployeeInsurance,
    SUM(r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyUnionFee) AS TotalCompanyContribution,
    SUM(r.PersonalIncomeTax) AS TotalPersonalIncomeTax,
    SUM(r.DisciplineDeduction) AS TotalDisciplineDeduction,
    SUM(r.NetSalary) AS TotalNetSalary,
    SUM(r.GrossIncome + r.CompanySocialInsurance + r.CompanyHealthInsurance + r.CompanyUnemploymentInsurance + r.CompanyUnionFee) AS TotalEmployerLaborCost
FROM [payroll].[PayrollPeriods] p
INNER JOIN [payroll].[PayrollRecords] r
    ON r.PayrollPeriodId = p.Id AND r.IsDeleted = 0
INNER JOIN [core].[OrganizationUnits] u
    ON r.OrganizationUnitId = u.Id
WHERE p.IsDeleted = 0
GROUP BY
    p.Id, p.PeriodCode, p.Year, p.Month, p.Status,
    u.Id, u.UnitCode, u.UnitName;
";

    public const string CreateSpMonthlyHeadcountAndTurnoverSql = @"
CREATE OR ALTER PROCEDURE [core].[sp_GetMonthlyHeadcountAndTurnover]
    @Year INT,
    @OrganizationUnitId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH Months AS (
        SELECT 1 AS MonthNumber
        UNION ALL
        SELECT MonthNumber + 1 FROM Months WHERE MonthNumber < 12
    ),
    MonthRanges AS (
        SELECT
            MonthNumber,
            DATEFROMPARTS(@Year, MonthNumber, 1) AS MonthStart,
            EOMONTH(DATEFROMPARTS(@Year, MonthNumber, 1)) AS MonthEnd
        FROM Months
    )
    SELECT
        m.MonthNumber AS [Month],
        @Year AS [Year],
        -- Nhân sự đầu tháng
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.HireDate < m.MonthStart
           AND (e.TerminationDate IS NULL OR e.TerminationDate >= m.MonthStart)
        ) AS OpeningHeadcount,
        -- Tuyển mới trong tháng
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.HireDate BETWEEN m.MonthStart AND m.MonthEnd
        ) AS NewHires,
        -- Nghỉ việc / Nghỉ hưu trong tháng
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.TerminationDate BETWEEN m.MonthStart AND m.MonthEnd
        ) AS Terminations,
        -- Nhân sự cuối tháng
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.HireDate <= m.MonthEnd
           AND (e.TerminationDate IS NULL OR e.TerminationDate > m.MonthEnd)
        ) AS ClosingHeadcount
    FROM MonthRanges m
    ORDER BY m.MonthNumber;
END;
";

    public const string CreateSpPayrollFundAnalysisSql = @"
CREATE OR ALTER PROCEDURE [payroll].[sp_GetPayrollFundAnalysis]
    @Year INT,
    @Month INT = NULL,
    @OrganizationUnitId INT = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        v.Year,
        v.Month,
        v.PeriodCode,
        v.OrganizationUnitId,
        v.UnitCode,
        v.UnitName,
        v.EmployeeCount,
        v.TotalActualBaseSalary,
        v.TotalAllowances,
        v.TotalOvertimePay,
        v.TotalKpiBonus,
        v.TotalGrossIncome,
        v.TotalEmployeeInsurance,
        v.TotalCompanyContribution,
        v.TotalPersonalIncomeTax,
        v.TotalNetSalary,
        v.TotalEmployerLaborCost,
        CASE
            WHEN v.EmployeeCount > 0 THEN CAST(v.TotalGrossIncome / v.EmployeeCount AS DECIMAL(18,2))
            ELSE 0
        END AS AverageGrossSalary,
        CASE
            WHEN v.EmployeeCount > 0 THEN CAST(v.TotalNetSalary / v.EmployeeCount AS DECIMAL(18,2))
            ELSE 0
        END AS AverageNetSalary
    FROM [payroll].[vw_MonthlyPayrollFundSummary] v
    WHERE v.Year = @Year
      AND (@Month IS NULL OR v.Month = @Month)
      AND (@OrganizationUnitId IS NULL OR v.OrganizationUnitId = @OrganizationUnitId)
    ORDER BY v.Year DESC, v.Month DESC, v.UnitCode ASC;
END;
";

    public const string DropAllSqlObjects = @"
IF OBJECT_ID('[payroll].[sp_GetPayrollFundAnalysis]', 'P') IS NOT NULL
    DROP PROCEDURE [payroll].[sp_GetPayrollFundAnalysis];
IF OBJECT_ID('[core].[sp_GetMonthlyHeadcountAndTurnover]', 'P') IS NOT NULL
    DROP PROCEDURE [core].[sp_GetMonthlyHeadcountAndTurnover];
IF OBJECT_ID('[payroll].[vw_MonthlyPayrollFundSummary]', 'V') IS NOT NULL
    DROP VIEW [payroll].[vw_MonthlyPayrollFundSummary];
IF OBJECT_ID('[core].[vw_DepartmentHeadcountStructure]', 'V') IS NOT NULL
    DROP VIEW [core].[vw_DepartmentHeadcountStructure];
";
}

