/* =========================================================================================
 * VINAMILK ENTERPRISE HRM (.NET 8 CLEAN ARCHITECTURE)
 * SCRIPT CHUYỂN ĐỔI DỮ LIỆU TỪ MYSQL (database/vinamilk_hrm.sql) SANG SQL SERVER (SSMS)
 * VÀ KHỞI TẠO VIEW / STORED PROCEDURE BÁO CÁO TỐI ƯU
 * =========================================================================================
 * Hướng dẫn chạy trên SQL Server Management Studio (SSMS):
 *   1. Chạy lệnh `dotnet ef database update` để khởi tạo cấu trúc 49 bảng chuẩn 3NF, hoặc
 *      chạy ứng dụng `dotnet run --project dotnet/src/Vinamilk.HRM.Web` để tự động Migrate & Seed.
 *   2. Mở file này trong SSMS (kết nối vào Server = ., Database = VinamilkHRM) và nhấn F5 (Execute)
 *      để kiểm tra/đồng bộ dữ liệu, khởi tạo Views & Stored Procedures và xem báo cáo nghiệm thu.
 * ========================================================================================= */

USE [VinamilkHRM];
GO

SET NOCOUNT ON;
GO

-- =========================================================================================
-- PHẦN 1: TẠO VIEW TỐI ƯU CHO BÁO CÁO CƠ CẤU NHÂN SỰ (BM01) & TỔNG QUỸ LƯƠNG (BM03)
-- =========================================================================================

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
GO

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
GO

-- =========================================================================================
-- PHẦN 2: STORED PROCEDURES BÁO CÁO NẶNG (HEADCOUNT THEO THÁNG & PHÂN TÍCH TỔNG QUỸ LƯƠNG)
-- =========================================================================================

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
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.HireDate < m.MonthStart
           AND (e.TerminationDate IS NULL OR e.TerminationDate >= m.MonthStart)
        ) AS OpeningHeadcount,
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.HireDate BETWEEN m.MonthStart AND m.MonthEnd
        ) AS NewHires,
        (SELECT COUNT(1)
         FROM [core].[Employees] e
         WHERE e.IsDeleted = 0
           AND (@OrganizationUnitId IS NULL OR e.OrganizationUnitId = @OrganizationUnitId)
           AND e.TerminationDate BETWEEN m.MonthStart AND m.MonthEnd
        ) AS Terminations,
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
GO

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
GO

-- =========================================================================================
-- PHẦN 3: TRUY VẤN KIỂM TRA NGHIỆM THU TRÊN SSMS
-- =========================================================================================
PRINT N'=== KIỂM TRA SỐ LƯỢNG BẢNG THEO SCHEMA ===';
SELECT s.name AS SchemaName, COUNT(t.object_id) AS TableCount
FROM sys.tables t
INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
GROUP BY s.name
ORDER BY s.name;

PRINT N'=== BÁO CÁO CƠ CẤU NHÂN SỰ THEO ĐƠN VỊ (VIEW) ===';
SELECT TOP 15 UnitCode, UnitName, PlannedHeadcount, ActualHeadcount, MaleCount, FemaleCount, TotalContractSalaryFund
FROM [core].[vw_DepartmentHeadcountStructure]
ORDER BY ActualHeadcount DESC;

PRINT N'=== BÁO CÁO HEADCOUNT THEO THÁNG NĂM 2026 (STORED PROCEDURE) ===';
EXEC [core].[sp_GetMonthlyHeadcountAndTurnover] @Year = 2026;

PRINT N'=== BÁO CÁO QUỸ LƯƠNG THÁNG 08/2026 (STORED PROCEDURE) ===';
EXEC [payroll].[sp_GetPayrollFundAnalysis] @Year = 2026, @Month = 8;
GO

