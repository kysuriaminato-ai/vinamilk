-- ====================================================================================================
-- DỰ ÁN: HỆ THỐNG QUẢN LÝ NHÂN LỰC TẬP ĐOÀN VINAMILK (VINAMILK ENTERPRISE HRM)
-- TẬP TIN: VinamilkHRMDb_Full_Schema_And_Seed.sql
-- TÁC GIẢ: Principal Software Architect
-- MỤC ĐÍCH: Kịch bản khởi tạo toàn diện Cơ sở dữ liệu, Schema, Ràng buộc khóa, Chỉ mục hiệu năng cao,
--           và Dữ liệu mẫu thực tế của Vinamilk (Trụ sở Q7, Mega Factory Bình Dương, Farm Tây Ninh, GMSV).
-- TƯƠNG THÍCH: Microsoft SQL Server 2019 / 2022 / SQL Server Management Studio (SSMS).
-- ====================================================================================================

SET NOCOUNT ON;
GO

-- ====================================================================================================
-- PHẦN 1: KHỞI TẠO CƠ SỞ DỮ LIỆU VINAMILK ENTERPRISE HRM
-- ====================================================================================================
IF NOT EXISTS (SELECT 1 FROM sys.databases WHERE name = N'VinamilkHRMDb')
BEGIN
    PRINT N'==> Đang khởi tạo Cơ sở dữ liệu [VinamilkHRMDb]...';
    CREATE DATABASE [VinamilkHRMDb]
    COLLATE Vietnamese_CI_AS;
END
ELSE
BEGIN
    PRINT N'==> Cơ sở dữ liệu [VinamilkHRMDb] đã tồn tại. Đang chuyển ngữ cảnh...';
END
GO

USE [VinamilkHRMDb];
GO

-- ====================================================================================================
-- PHẦN 2: XÓA RÀNG BUỘC KHÓA NGOẠI VÀ BẢNG CŨ (NẾU ĐÃ TỒN TẠI ĐỂ ĐẢM BẢO TÍNH IDEMPOTENT KHI CHẠY LẠI)
-- ====================================================================================================
IF OBJECT_ID(N'dbo.DisciplinaryWarnings', N'U') IS NOT NULL DROP TABLE dbo.DisciplinaryWarnings;
IF OBJECT_ID(N'dbo.AuditLogs', N'U') IS NOT NULL DROP TABLE dbo.AuditLogs;
IF OBJECT_ID(N'dbo.CompanyAssets', N'U') IS NOT NULL DROP TABLE dbo.CompanyAssets;
IF OBJECT_ID(N'dbo.PaySlips', N'U') IS NOT NULL DROP TABLE dbo.PaySlips;
IF OBJECT_ID(N'dbo.SalaryProposals', N'U') IS NOT NULL DROP TABLE dbo.SalaryProposals;
IF OBJECT_ID(N'dbo.KpiAssignments', N'U') IS NOT NULL DROP TABLE dbo.KpiAssignments;
IF OBJECT_ID(N'dbo.LeaveRequests', N'U') IS NOT NULL DROP TABLE dbo.LeaveRequests;
IF OBJECT_ID(N'dbo.Attendances', N'U') IS NOT NULL DROP TABLE dbo.Attendances;
IF OBJECT_ID(N'dbo.Shifts', N'U') IS NOT NULL DROP TABLE dbo.Shifts;
IF OBJECT_ID(N'dbo.LaborContracts', N'U') IS NOT NULL DROP TABLE dbo.LaborContracts;

-- Gỡ bỏ ràng buộc tự tham chiếu vòng giữa Employees và OrganizationalUnits nếu có
IF OBJECT_ID(N'dbo.FK_OrganizationalUnits_Manager', N'F') IS NOT NULL 
    ALTER TABLE dbo.OrganizationalUnits DROP CONSTRAINT FK_OrganizationalUnits_Manager;

IF OBJECT_ID(N'dbo.Employees', N'U') IS NOT NULL DROP TABLE dbo.Employees;
IF OBJECT_ID(N'dbo.OrganizationalUnits', N'U') IS NOT NULL DROP TABLE dbo.OrganizationalUnits;
GO

-- ====================================================================================================
-- PHẦN 3: TẠO CẤU TRÚC BẢNG (TABLE SCHEMAS) CHUẨN CLEAN ARCHITECTURE
-- ====================================================================================================

-- ----------------------------------------------------------------------------------------------------
-- 1. BẢNG: OrganizationalUnits (Đơn vị tổ chức: Trụ sở chính, Nhà máy, Trang trại, Cửa hàng bán lẻ)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [OrganizationalUnits]...';
CREATE TABLE dbo.OrganizationalUnits
(
    Id INT IDENTITY(1,1) NOT NULL,
    UnitCode NVARCHAR(50) NOT NULL,
    UnitName NVARCHAR(200) NOT NULL,
    UnitType INT NOT NULL, -- 1: Headquarters, 2: Factory, 3: DairyFarm, 4: RetailStore
    Address NVARCHAR(500) NOT NULL,
    PhoneNumber NVARCHAR(50) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    TaxCode NVARCHAR(50) NOT NULL,
    RegionId INT NOT NULL CONSTRAINT DF_OrgUnits_RegionId DEFAULT 3, -- 1: Miền Bắc, 2: Miền Trung, 3: Miền Nam
    RegionName NVARCHAR(100) NOT NULL CONSTRAINT DF_OrgUnits_RegionName DEFAULT N'Miền Nam',
    ParentUnitId INT NULL,
    ManagerEmployeeId INT NULL,
    
    -- Các trường đặc thù theo mô hình Table-Per-Hierarchy (TPH)
    -- Dành cho HeadquartersUnit:
    FloorCount INT NULL,
    MeetingRoomCount INT NULL,
    HasExecutiveSuite BIT NULL,
    DivisionName NVARCHAR(100) NULL,
    
    -- Dành cho FactoryUnit:
    FactoryCapacityLitersPerDay DECIMAL(18,2) NULL,
    ProductionLineCount INT NULL,
    IsoStandard NVARCHAR(250) NULL,
    EnvironmentalPermitNumber NVARCHAR(100) NULL,
    
    -- Dành cho DairyFarmUnit:
    HerdSize INT NULL,
    TotalAreaHectares DECIMAL(18,2) NULL,
    BarnCount INT NULL,
    GlobalGapCertificationNumber NVARCHAR(100) NULL,
    
    -- Dành cho RetailStoreUnit:
    StoreCode NVARCHAR(50) NULL,
    ShelfAreaSquareMeters DECIMAL(18,2) NULL,
    ColdStorageCapacityLiters DECIMAL(18,2) NULL,
    BusinessHours NVARCHAR(100) NULL,
    
    -- Quản lý dấu vết kiểm toán (BaseAuditableEntity)
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_OrgUnits_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_OrgUnits_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_OrgUnits_IsDeleted DEFAULT 0,

    CONSTRAINT PK_OrganizationalUnits PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_OrganizationalUnits_Parent FOREIGN KEY (ParentUnitId) 
        REFERENCES dbo.OrganizationalUnits(Id) ON DELETE NO ACTION
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 2. BẢNG: Employees (Cán bộ nhân viên Vinamilk: Văn phòng, Nhà máy, Trang trại, Bán lẻ)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [Employees]...';
CREATE TABLE dbo.Employees
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeCode NVARCHAR(50) NOT NULL,
    FirstName NVARCHAR(100) NOT NULL,
    LastName NVARCHAR(100) NOT NULL,
    CitizenId NVARCHAR(20) NOT NULL,
    DateOfBirth DATETIME2 NOT NULL,
    Gender INT NOT NULL, -- 1: Male, 2: Female, 3: Other
    Email NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(50) NOT NULL,
    Address NVARCHAR(500) NOT NULL,
    HireDate DATETIME2 NOT NULL,
    EmployeeType INT NOT NULL, -- 1: Office, 2: FactoryWorker, 3: FarmSpecialist, 4: StoreAssociate
    IsActive BIT NOT NULL CONSTRAINT DF_Employees_IsActive DEFAULT 1,
    OrganizationalUnitId INT NOT NULL,
    ManagerId INT NULL,
    BaseSalary DECIMAL(18,2) NOT NULL,
    Allowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_Employees_Allowance DEFAULT 0,
    InsuranceSalary DECIMAL(18,2) NOT NULL CONSTRAINT DF_Employees_InsSalary DEFAULT 0,
    DependentCount INT NOT NULL CONSTRAINT DF_Employees_Dependents DEFAULT 0,
    AnnualLeaveBalance DECIMAL(5,2) NOT NULL CONSTRAINT DF_Employees_LeaveBal DEFAULT 12.00,
    DiligenceScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_Employees_Diligence DEFAULT 100.00,

    -- Thuộc tính mở rộng TPH: OfficeEmployee
    JobTitle NVARCHAR(150) NULL,
    DeskLocation NVARCHAR(150) NULL,
    ExtensionNumber NVARCHAR(50) NULL,
    CubicleNumber NVARCHAR(50) NULL,

    -- Thuộc tính mở rộng TPH: FactoryWorker
    ProductionShiftGroup NVARCHAR(150) NULL,
    HazardousAllowanceRate DECIMAL(18,2) NULL,
    MachineCertificationCode NVARCHAR(100) NULL,
    SafetyLevel NVARCHAR(150) NULL,

    -- Thuộc tính mở rộng TPH: FarmSpecialist
    SpecializationArea NVARCHAR(200) NULL,
    BioSecurityClearanceCode NVARCHAR(100) NULL,
    AnimalCareAllowanceRate DECIMAL(18,2) NULL,
    VeterinaryLicenseNumber NVARCHAR(100) NULL,

    -- Thuộc tính mở rộng TPH: StoreAssociate
    CashRegisterId NVARCHAR(50) NULL,
    RetailSalesBonusRate DECIMAL(5,2) NULL,
    UniformSize NVARCHAR(20) NULL,
    CustomerServiceRating DECIMAL(3,2) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Employees_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Employees_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Employees_IsDeleted DEFAULT 0,

    CONSTRAINT PK_Employees PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Employees_OrganizationalUnit FOREIGN KEY (OrganizationalUnitId) 
        REFERENCES dbo.OrganizationalUnits(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Employees_Manager FOREIGN KEY (ManagerId) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Employees_BaseSalary CHECK (BaseSalary >= 0),
    CONSTRAINT CK_Employees_Allowance CHECK (Allowance >= 0)
);
GO

-- Bổ sung Khóa ngoại từ Đơn vị tổ chức đến Người quản lý (ManagerEmployeeId -> Employees.Id)
ALTER TABLE dbo.OrganizationalUnits
ADD CONSTRAINT FK_OrganizationalUnits_Manager FOREIGN KEY (ManagerEmployeeId)
    REFERENCES dbo.Employees(Id) ON DELETE NO ACTION;
GO

-- ----------------------------------------------------------------------------------------------------
-- 3. BẢNG: LaborContracts (Hợp đồng lao động: Thử việc, Có thời hạn, Không xác định thời hạn)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [LaborContracts]...';
CREATE TABLE dbo.LaborContracts
(
    Id INT IDENTITY(1,1) NOT NULL,
    ContractNumber NVARCHAR(50) NOT NULL,
    EmployeeId INT NOT NULL,
    ContractType INT NOT NULL, -- 1: Probation, 2: Definite, 3: Indefinite
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NULL,
    BasicSalary DECIMAL(18,2) NOT NULL,
    InsuranceSalary DECIMAL(18,2) NOT NULL,
    WorkingHoursPerWeek INT NOT NULL CONSTRAINT DF_Contracts_Hours DEFAULT 40,
    Status INT NOT NULL CONSTRAINT DF_Contracts_Status DEFAULT 2, -- 1: Draft, 2: Active, 3: Expired, 4: Terminated
    SignDate DATETIME2 NOT NULL,
    TerminationDate DATETIME2 NULL,
    TerminationReason NVARCHAR(500) NULL,

    -- TPH: ProbationContract
    ProbationEvaluationScore DECIMAL(5,2) NULL,
    MentorEmployeeId INT NULL,
    ProbationPassed BIT NULL,
    EvaluationFeedback NVARCHAR(500) NULL,

    -- TPH: DefiniteContract
    DurationMonths INT NULL,
    MaximumRenewalsAllowed INT NULL,
    CurrentRenewalCount INT NULL,

    -- TPH: IndefiniteContract
    SeverancePackageTerms NVARCHAR(500) NULL,
    AnnualTenureBonusRate DECIMAL(5,2) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Contracts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Contracts_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Contracts_IsDeleted DEFAULT 0,

    CONSTRAINT PK_LaborContracts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_LaborContracts_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LaborContracts_Mentor FOREIGN KEY (MentorEmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_LaborContracts_BasicSalary CHECK (BasicSalary > 0),
    CONSTRAINT CK_LaborContracts_InsuranceSalary CHECK (InsuranceSalary > 0),
    CONSTRAINT CK_LaborContracts_Dates CHECK (EndDate IS NULL OR EndDate >= StartDate)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 3B. BẢNG: ElectronicContractAppendices (Phụ lục hợp đồng lao động điện tử - Tăng lương & Đặc cách)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [ElectronicContractAppendices]...';
CREATE TABLE dbo.ElectronicContractAppendices
(
    Id INT IDENTITY(1,1) NOT NULL,
    AppendixNumber NVARCHAR(100) NOT NULL,
    LaborContractId INT NOT NULL,
    EmployeeId INT NOT NULL,
    EffectiveDate DATETIME2 NOT NULL,
    SignDate DATETIME2 NOT NULL,
    PreviousSalary DECIMAL(18,2) NOT NULL,
    NewSalary DECIMAL(18,2) NOT NULL,
    PreviousAllowance DECIMAL(18,2) NOT NULL,
    NewAllowance DECIMAL(18,2) NOT NULL,
    AdjustmentReason NVARCHAR(500) NOT NULL,
    IsCeoOverride BIT NOT NULL CONSTRAINT DF_ECA_CeoOverride DEFAULT 0,
    CeoOverrideCode NVARCHAR(100) NULL,
    DigitalSignatureHash NVARCHAR(256) NOT NULL,
    SignerName NVARCHAR(150) NOT NULL,
    SignerTitle NVARCHAR(150) NOT NULL,
    IsSigned BIT NOT NULL CONSTRAINT DF_ECA_Signed DEFAULT 1,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_ECA_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_ECA_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_ECA_IsDeleted DEFAULT 0,

    CONSTRAINT PK_ElectronicContractAppendices PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_ECA_LaborContract FOREIGN KEY (LaborContractId) 
        REFERENCES dbo.LaborContracts(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_ECA_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT UQ_ECA_Number UNIQUE (AppendixNumber)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 4. BẢNG: Shifts (Ca làm việc quy chuẩn tại Trụ sở, Nhà máy, Trang trại, Cửa hàng)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [Shifts]...';
CREATE TABLE dbo.Shifts
(
    Id INT IDENTITY(1,1) NOT NULL,
    ShiftCode NVARCHAR(50) NOT NULL,
    ShiftName NVARCHAR(100) NOT NULL,
    ShiftType INT NOT NULL, -- 1: OfficeStandard, 2: FactoryMorning, 3: FactoryAfternoon, 4: FactoryNight, 5: FarmEarlyCare, 6: FarmLateCare, 7: RetailMorning, 8: RetailEvening
    StartTime TIME(0) NOT NULL,
    EndTime TIME(0) NOT NULL,
    BreakMinutes INT NOT NULL CONSTRAINT DF_Shifts_Break DEFAULT 60,
    MultiplierRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Shifts_Rate DEFAULT 1.00,
    OrganizationalUnitId INT NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Shifts_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Shifts_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Shifts_IsDeleted DEFAULT 0,

    CONSTRAINT PK_Shifts PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Shifts_OrganizationalUnit FOREIGN KEY (OrganizationalUnitId)
        REFERENCES dbo.OrganizationalUnits(Id) ON DELETE SET NULL,
    CONSTRAINT CK_Shifts_Multiplier CHECK (MultiplierRate > 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 5. BẢNG: Attendances (Dữ liệu chấm công hàng ngày & Giờ làm thêm OT)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [Attendances]...';
CREATE TABLE dbo.Attendances
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    ShiftId INT NOT NULL,
    AttendanceDate DATE NOT NULL,
    CheckInTime TIME(0) NULL,
    CheckOutTime TIME(0) NULL,
    WorkHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_Attendances_WorkHours DEFAULT 0.00,
    OvertimeHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_Attendances_OT DEFAULT 0.00,
    Status INT NOT NULL CONSTRAINT DF_Attendances_Status DEFAULT 1, -- 1: Present, 2: Late, 3: EarlyDeparture, 4: LateAndEarly, 5: AbsentUnexcused, 6: OnLeave, 7: Holiday
    Note NVARCHAR(500) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Attendances_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Attendances_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Attendances_IsDeleted DEFAULT 0,

    CONSTRAINT PK_Attendances PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Attendances_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT FK_Attendances_Shift FOREIGN KEY (ShiftId) 
        REFERENCES dbo.Shifts(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Attendances_Hours CHECK (WorkHours >= 0 AND OvertimeHours >= 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 6. BẢNG: LeaveRequests (Đơn xin nghỉ phép & Quản lý ngày phép hưởng lương)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [LeaveRequests]...';
CREATE TABLE dbo.LeaveRequests
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    LeaveType INT NOT NULL, -- 1: AnnualLeave, 2: SickLeave, 3: MaternityLeave, 4: CompassionateLeave, 5: UnpaidLeave
    StartDate DATE NOT NULL,
    EndDate DATE NOT NULL,
    TotalDays DECIMAL(5,2) NOT NULL,
    Reason NVARCHAR(500) NOT NULL,
    Status INT NOT NULL CONSTRAINT DF_Leaves_Status DEFAULT 1, -- 1: Pending, 2: Approved, 3: Rejected, 4: Cancelled
    ApprovedById INT NULL,
    ApprovalNotes NVARCHAR(500) NULL,
    DecisionDate DATETIME2 NULL,
    IsSocialInsuranceCovered BIT NOT NULL CONSTRAINT DF_Leaves_Bhxh DEFAULT 0,
    SocialInsuranceBenefitRate DECIMAL(5,2) NOT NULL CONSTRAINT DF_Leaves_BhxhRate DEFAULT 0.00,
    SocialInsuranceFormCode NVARCHAR(100) NULL,
    SocialInsuranceFormContent NVARCHAR(MAX) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Leaves_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Leaves_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Leaves_IsDeleted DEFAULT 0,

    CONSTRAINT PK_LeaveRequests PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_LeaveRequests_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT FK_LeaveRequests_Approver FOREIGN KEY (ApprovedById) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_LeaveRequests_Days CHECK (TotalDays > 0 AND EndDate >= StartDate)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 7. BẢNG: KpiAssignments (Giao và Đánh giá Chỉ tiêu Hiệu suất Công việc)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [KpiAssignments]...';
CREATE TABLE dbo.KpiAssignments
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    EvaluatorId INT NOT NULL,
    Period NVARCHAR(20) NOT NULL, -- '2025-Q1', '2025-M01'
    Title NVARCHAR(250) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    TargetMetric NVARCHAR(500) NOT NULL,
    WeightPercentage DECIMAL(5,2) NOT NULL,
    DueDate DATETIME2 NOT NULL CONSTRAINT DF_Kpis_DueDate DEFAULT DATEADD(DAY, 7, SYSUTCDATETIME()),
    BaseScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_Kpis_BaseScore DEFAULT 100.00,
    SlaBonusPenaltyScore DECIMAL(5,2) NOT NULL CONSTRAINT DF_Kpis_SlaBonus DEFAULT 0.00,
    ScoreAdjustment DECIMAL(5,2) NOT NULL CONSTRAINT DF_Kpis_ScoreAdj DEFAULT 0.00,
    SlaCalculationDetail NVARCHAR(500) NULL,
    CompletedDate DATETIME2 NULL,
    ActualResult NVARCHAR(2000) NULL,
    SelfEvaluationScore DECIMAL(5,2) NULL,
    FinalScore DECIMAL(5,2) NULL,
    Status INT NOT NULL CONSTRAINT DF_Kpis_Status DEFAULT 2, -- 1: Draft, 2: Assigned, 3: InProgress, 4: Submitted, 5: Approved, 6: Rejected
    Feedback NVARCHAR(1000) NULL,
    SubmissionDate DATETIME2 NULL,
    EvaluationDate DATETIME2 NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Kpis_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Kpis_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Kpis_IsDeleted DEFAULT 0,

    CONSTRAINT PK_KpiAssignments PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_KpiAssignments_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT FK_KpiAssignments_Evaluator FOREIGN KEY (EvaluatorId) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_KpiAssignments_Weight CHECK (WeightPercentage > 0 AND WeightPercentage <= 100),
    CONSTRAINT CK_KpiAssignments_Scores CHECK (
        (SelfEvaluationScore IS NULL OR (SelfEvaluationScore >= 0 AND SelfEvaluationScore <= 100)) AND
        (FinalScore IS NULL OR (FinalScore >= 0 AND FinalScore <= 100))
    )
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 8. BẢNG: SalaryProposals (Tờ trình đề xuất điều chỉnh / nâng lương)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [SalaryProposals]...';
CREATE TABLE dbo.SalaryProposals
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    ProposedById INT NOT NULL,
    CurrentSalary DECIMAL(18,2) NOT NULL,
    ProposedSalary DECIMAL(18,2) NOT NULL,
    CurrentAllowance DECIMAL(18,2) NOT NULL,
    ProposedAllowance DECIMAL(18,2) NOT NULL,
    Reason NVARCHAR(1000) NOT NULL,
    EffectiveDate DATE NOT NULL,
    Status INT NOT NULL CONSTRAINT DF_SalaryProp_Status DEFAULT 2, -- 1: Draft, 2: PendingDepartmentHead, 3: PendingHRDirector, 4: Approved, 5: Rejected
    DepartmentHeadApprovedById INT NULL,
    DepartmentHeadNotes NVARCHAR(500) NULL,
    HRDirectorApprovedById INT NULL,
    HRDirectorNotes NVARCHAR(500) NULL,
    FinalDecisionDate DATETIME2 NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_SalaryProp_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_SalaryProp_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_SalaryProp_IsDeleted DEFAULT 0,

    CONSTRAINT PK_SalaryProposals PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_SalaryProposals_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT FK_SalaryProposals_Proposer FOREIGN KEY (ProposedById) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SalaryProposals_DeptHead FOREIGN KEY (DepartmentHeadApprovedById) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_SalaryProposals_HRDirector FOREIGN KEY (HRDirectorApprovedById) 
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_SalaryProposals_Salaries CHECK (ProposedSalary > 0 AND ProposedAllowance >= 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 9. BẢNG: PaySlips (Bảng lương & Phiếu lương chi tiết hàng tháng)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [PaySlips]...';
CREATE TABLE dbo.PaySlips
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    PayrollPeriod NVARCHAR(20) NOT NULL, -- '2025-01'
    StandardWorkDays DECIMAL(5,2) NOT NULL CONSTRAINT DF_PaySlips_StdDays DEFAULT 22.00,
    ActualWorkDays DECIMAL(5,2) NOT NULL CONSTRAINT DF_PaySlips_ActDays DEFAULT 22.00,
    BaseSalary DECIMAL(18,2) NOT NULL,
    Allowance DECIMAL(18,2) NOT NULL,
    LunchAllowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_Lunch DEFAULT 0.00,
    OvertimeHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_PaySlips_OTHours DEFAULT 0.00,
    OvertimePay DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_OTPay DEFAULT 0.00,
    NightShiftHours DECIMAL(5,2) NOT NULL CONSTRAINT DF_PaySlips_NightHours DEFAULT 0.00,
    NightShiftPay DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_NightPay DEFAULT 0.00,
    HazardousAllowance DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_Hazard DEFAULT 0.00,
    SalesCommission DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_Comm DEFAULT 0.00,
    KpiBonus DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_KpiBonus DEFAULT 0.00,
    OtherBonuses DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_OtherBonus DEFAULT 0.00,
    GrossSalary DECIMAL(18,2) NOT NULL,
    SocialInsuranceDeduction DECIMAL(18,2) NOT NULL,       -- 8%
    HealthInsuranceDeduction DECIMAL(18,2) NOT NULL,       -- 1.5%
    UnemploymentInsuranceDeduction DECIMAL(18,2) NOT NULL, -- 1%
    DependentCount INT NOT NULL CONSTRAINT DF_PaySlips_DepCount DEFAULT 0,
    PersonalDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_PersonalRelief DEFAULT 11000000.00,
    DependentDeduction DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_DepRelief DEFAULT 0.00,
    TaxableIncome DECIMAL(18,2) NOT NULL CONSTRAINT DF_PaySlips_Taxable DEFAULT 0.00,
    PersonalIncomeTax DECIMAL(18,2) NOT NULL,              -- Thuế TNCN lũy tiến 7 bậc
    TotalDeductions DECIMAL(18,2) NOT NULL,
    NetSalary DECIMAL(18,2) NOT NULL,
    Status INT NOT NULL CONSTRAINT DF_PaySlips_Status DEFAULT 1, -- 1: Draft, 2: PendingApproval, 3: Approved, 4: Paid
    PaymentDate DATETIME2 NULL,
    TransactionReference NVARCHAR(100) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_PaySlips_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_PaySlips_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_PaySlips_IsDeleted DEFAULT 0,

    CONSTRAINT PK_PaySlips PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_PaySlips_Employee FOREIGN KEY (EmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE,
    CONSTRAINT CK_PaySlips_Salaries CHECK (GrossSalary >= 0 AND NetSalary >= 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 10. BẢNG: CompanyAssets (Tài sản & Trang thiết bị cấp phát cho người lao động)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [CompanyAssets]...';
CREATE TABLE dbo.CompanyAssets
(
    Id INT IDENTITY(1,1) NOT NULL,
    AssetCode NVARCHAR(50) NOT NULL,
    AssetName NVARCHAR(200) NOT NULL,
    Category NVARCHAR(100) NOT NULL,
    SerialNumber NVARCHAR(100) NOT NULL,
    AssignedToEmployeeId INT NULL,
    AssignedDate DATETIME2 NULL,
    OriginalValue DECIMAL(18,2) NOT NULL CONSTRAINT DF_Assets_Value DEFAULT 0,
    Status INT NOT NULL CONSTRAINT DF_Assets_Status DEFAULT 1, -- 1: Available, 2: InUse, 3: UnderMaintenance, 4: Disposed, 5: Lost
    Notes NVARCHAR(500) NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Assets_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Assets_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Assets_IsDeleted DEFAULT 0,

    CONSTRAINT PK_CompanyAssets PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CompanyAssets_Employee FOREIGN KEY (AssignedToEmployeeId) 
        REFERENCES dbo.Employees(Id) ON DELETE SET NULL,
    CONSTRAINT CK_CompanyAssets_Value CHECK (OriginalValue >= 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 11. BẢNG: AuditLogs (Nhật ký kiểm toán lưu vết thay đổi dữ liệu nhạy cảm)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [AuditLogs]...';
CREATE TABLE dbo.AuditLogs
(
    Id INT IDENTITY(1,1) NOT NULL,
    TableName NVARCHAR(100) NOT NULL,
    Action NVARCHAR(50) NOT NULL,
    EntityId NVARCHAR(50) NOT NULL,
    OldValues NVARCHAR(MAX) NULL,
    NewValues NVARCHAR(MAX) NULL,
    PerformedBy NVARCHAR(100) NOT NULL,
    PerformedAt DATETIME2 NOT NULL CONSTRAINT DF_AuditLogs_PerformedAt DEFAULT SYSUTCDATETIME(),
    IpAddress NVARCHAR(50) NULL,

    CONSTRAINT PK_AuditLogs PRIMARY KEY CLUSTERED (Id)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 12. BẢNG: DisciplinaryWarnings (Cảnh báo vi phạm Kỷ luật lao động tự động Điều 125 BLLĐ)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [DisciplinaryWarnings]...';
CREATE TABLE dbo.DisciplinaryWarnings
(
    Id INT IDENTITY(1,1) NOT NULL,
    EmployeeId INT NOT NULL,
    WarningDate DATETIME2 NOT NULL CONSTRAINT DF_Disc_WarnDate DEFAULT SYSUTCDATETIME(),
    ConsecutiveAbsentDays INT NOT NULL,
    ViolationTitle NVARCHAR(250) NOT NULL,
    Details NVARCHAR(1000) NOT NULL,
    IsResolved BIT NOT NULL CONSTRAINT DF_Disc_IsResolved DEFAULT 0,
    ResolutionNotes NVARCHAR(1000) NULL,
    ResolvedAt DATETIME2 NULL,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Disc_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Disc_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Disc_IsDeleted DEFAULT 0,

    CONSTRAINT PK_DisciplinaryWarnings PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_DisciplinaryWarnings_Employee FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(Id) ON DELETE CASCADE
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 13. BẢNG: ElectronicContractAppendices (Phụ lục Hợp đồng Lao động Điện tử & Quyết định Nâng lương)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [ElectronicContractAppendices]...';
CREATE TABLE dbo.ElectronicContractAppendices
(
    Id INT IDENTITY(1,1) NOT NULL,
    AppendixNumber NVARCHAR(100) NOT NULL,
    LaborContractId INT NOT NULL,
    EmployeeId INT NOT NULL,
    EffectiveDate DATETIME2 NOT NULL,
    SignDate DATETIME2 NOT NULL,
    PreviousSalary DECIMAL(18,2) NOT NULL,
    NewSalary DECIMAL(18,2) NOT NULL,
    PreviousAllowance DECIMAL(18,2) NOT NULL,
    NewAllowance DECIMAL(18,2) NOT NULL,
    AdjustmentReason NVARCHAR(500) NOT NULL,
    IsCeoOverride BIT NOT NULL CONSTRAINT DF_Appendices_CeoOverride DEFAULT 0,
    CeoOverrideCode NVARCHAR(100) NULL,
    DigitalSignatureHash NVARCHAR(256) NOT NULL,
    SignerName NVARCHAR(150) NOT NULL CONSTRAINT DF_Appendices_SignerName DEFAULT N'Mai Kiều Liên',
    SignerTitle NVARCHAR(150) NOT NULL CONSTRAINT DF_Appendices_SignerTitle DEFAULT N'Tổng Giám Đốc (CEO) - Vinamilk',
    IsSigned BIT NOT NULL CONSTRAINT DF_Appendices_IsSigned DEFAULT 1,

    -- Audit fields
    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_Appendices_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_Appendices_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_Appendices_IsDeleted DEFAULT 0,

    CONSTRAINT PK_ElectronicContractAppendices PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_Appendices_Employee FOREIGN KEY (EmployeeId)
        REFERENCES dbo.Employees(Id) ON DELETE NO ACTION,
    CONSTRAINT FK_Appendices_LaborContract FOREIGN KEY (LaborContractId)
        REFERENCES dbo.LaborContracts(Id) ON DELETE NO ACTION,
    CONSTRAINT CK_Appendices_Salary CHECK (NewSalary > 0)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 14. BẢNG: JobPostings (Tin tuyển dụng công khai Cổng Careers Vinamilk - Chuẩn SEO & Google Jobs)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [JobPostings]...';
CREATE TABLE dbo.JobPostings
(
    Id INT IDENTITY(1,1) NOT NULL,
    JobCode NVARCHAR(50) NOT NULL,
    Title NVARCHAR(250) NOT NULL,
    Slug NVARCHAR(250) NOT NULL,
    UnitType INT NOT NULL,
    Department NVARCHAR(150) NOT NULL,
    WorkLocation NVARCHAR(300) NOT NULL,
    City NVARCHAR(100) NOT NULL,
    RegionId INT NOT NULL CONSTRAINT DF_JobPostings_Region DEFAULT 3,
    EmploymentType NVARCHAR(50) NOT NULL CONSTRAINT DF_JobPostings_EmpType DEFAULT N'FULL_TIME',
    ExperienceLevel NVARCHAR(100) NULL,
    EducationRequirement NVARCHAR(100) NULL,
    SalaryMin DECIMAL(18,2) NULL,
    SalaryMax DECIMAL(18,2) NULL,
    SalaryDisplay NVARCHAR(150) NOT NULL,
    DatePosted DATETIME2 NOT NULL,
    ValidThrough DATETIME2 NOT NULL,
    Description NVARCHAR(MAX) NOT NULL,
    Requirements NVARCHAR(MAX) NOT NULL,
    Benefits NVARCHAR(MAX) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_JobPostings_Active DEFAULT 1,
    IsFeatured BIT NOT NULL CONSTRAINT DF_JobPostings_Featured DEFAULT 0,
    ViewCount INT NOT NULL CONSTRAINT DF_JobPostings_Views DEFAULT 0,

    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_JobPostings_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_JobPostings_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_JobPostings_IsDeleted DEFAULT 0,

    CONSTRAINT PK_JobPostings PRIMARY KEY CLUSTERED (Id)
);
GO

-- ----------------------------------------------------------------------------------------------------
-- 15. BẢNG: CandidateApplications (Hồ sơ ứng tuyển & Luồng Onboard Nhân viên Thử việc)
-- ----------------------------------------------------------------------------------------------------
PRINT N'==> Tạo bảng [CandidateApplications]...';
CREATE TABLE dbo.CandidateApplications
(
    Id INT IDENTITY(1,1) NOT NULL,
    JobPostingId INT NOT NULL,
    FullName NVARCHAR(150) NOT NULL,
    Email NVARCHAR(150) NOT NULL,
    PhoneNumber NVARCHAR(50) NOT NULL,
    CitizenId NVARCHAR(20) NULL,
    DateOfBirth DATETIME2 NOT NULL,
    Gender INT NOT NULL CONSTRAINT DF_CandApp_Gender DEFAULT 1,
    Address NVARCHAR(300) NULL,
    ResumeFileName NVARCHAR(250) NOT NULL,
    CoverLetter NVARCHAR(2000) NULL,
    PortfolioUrl NVARCHAR(300) NULL,
    Status INT NOT NULL CONSTRAINT DF_CandApp_Status DEFAULT 1,
    AppliedAt DATETIME2 NOT NULL CONSTRAINT DF_CandApp_AppliedAt DEFAULT SYSUTCDATETIME(),
    InterviewDate DATETIME2 NULL,
    InterviewScore DECIMAL(5,2) NULL,
    EvaluationNotes NVARCHAR(1000) NULL,
    HiredEmployeeId INT NULL,
    HiredDate DATETIME2 NULL,

    CreatedAt DATETIME2 NOT NULL CONSTRAINT DF_CandApp_CreatedAt DEFAULT SYSUTCDATETIME(),
    CreatedBy NVARCHAR(100) NOT NULL CONSTRAINT DF_CandApp_CreatedBy DEFAULT N'SYSTEM',
    UpdatedAt DATETIME2 NULL,
    UpdatedBy NVARCHAR(100) NULL,
    IsDeleted BIT NOT NULL CONSTRAINT DF_CandApp_IsDeleted DEFAULT 0,

    CONSTRAINT PK_CandidateApplications PRIMARY KEY CLUSTERED (Id),
    CONSTRAINT FK_CandApp_Job FOREIGN KEY (JobPostingId) REFERENCES dbo.JobPostings(Id) ON DELETE CASCADE,
    CONSTRAINT FK_CandApp_Employee FOREIGN KEY (HiredEmployeeId) REFERENCES dbo.Employees(Id) ON DELETE SET NULL
);
GO

-- ====================================================================================================
-- PHẦN 4: THIẾT LẬP CÁC CHỈ MỤC HIỆU NĂNG CAO (NON-CLUSTERED INDEXES)
-- ====================================================================================================
PRINT N'==> Đang thiết lập các chỉ mục tối ưu hóa truy vấn...';

-- 1. Index duy nhất trên Mã nhân viên (EmployeeCode)
CREATE UNIQUE NONCLUSTERED INDEX IX_Employees_EmployeeCode 
    ON dbo.Employees(EmployeeCode) 
    WHERE IsDeleted = 0;

-- 2. Index duy nhất trên Căn cước công dân (CitizenId)
CREATE UNIQUE NONCLUSTERED INDEX IX_Employees_CitizenId 
    ON dbo.Employees(CitizenId) 
    WHERE IsDeleted = 0;

-- 3. Index duy nhất trên Mã đơn vị tổ chức (UnitCode)
CREATE UNIQUE NONCLUSTERED INDEX IX_OrganizationalUnits_UnitCode 
    ON dbo.OrganizationalUnits(UnitCode) 
    WHERE IsDeleted = 0;

-- 4. Index trên Ngày chấm công (AttendanceDate) và tổ hợp (EmployeeId, AttendanceDate)
CREATE NONCLUSTERED INDEX IX_Attendances_AttendanceDate 
    ON dbo.Attendances(AttendanceDate);

CREATE UNIQUE NONCLUSTERED INDEX IX_Attendances_Employee_Date 
    ON dbo.Attendances(EmployeeId, AttendanceDate);

-- 5. Index trên Kỳ tính lương (PayrollPeriod) và tổ hợp (EmployeeId, PayrollPeriod)
CREATE NONCLUSTERED INDEX IX_PaySlips_PayrollPeriod 
    ON dbo.PaySlips(PayrollPeriod);

CREATE UNIQUE NONCLUSTERED INDEX IX_PaySlips_Employee_Period 
    ON dbo.PaySlips(EmployeeId, PayrollPeriod);

-- 6. Index trên Mã hợp đồng lao động
CREATE UNIQUE NONCLUSTERED INDEX IX_LaborContracts_ContractNumber 
    ON dbo.LaborContracts(ContractNumber) 
    WHERE IsDeleted = 0;

-- 7. Index trên Mã tài sản công ty
CREATE UNIQUE NONCLUSTERED INDEX IX_CompanyAssets_AssetCode 
    ON dbo.CompanyAssets(AssetCode) 
    WHERE IsDeleted = 0;

-- 8. Index trên Mã ca làm việc
CREATE UNIQUE NONCLUSTERED INDEX IX_Shifts_ShiftCode 
    ON dbo.Shifts(ShiftCode) 
    WHERE IsDeleted = 0;

-- 9. Index trên Thời điểm thực hiện Audit Log và Bảng tương ứng
CREATE NONCLUSTERED INDEX IX_AuditLogs_PerformedAt 
    ON dbo.AuditLogs(PerformedAt DESC);

CREATE NONCLUSTERED INDEX IX_AuditLogs_TableName 
    ON dbo.AuditLogs(TableName, EntityId);

-- 10. Index trên Phụ lục hợp đồng lao động điện tử
CREATE UNIQUE NONCLUSTERED INDEX IX_ElectronicContractAppendices_AppendixNumber 
    ON dbo.ElectronicContractAppendices(AppendixNumber) 
    WHERE IsDeleted = 0;

CREATE NONCLUSTERED INDEX IX_ElectronicContractAppendices_EmployeeId 
    ON dbo.ElectronicContractAppendices(EmployeeId);

-- 11. Index trên Tin tuyển dụng (Slug và JobCode)
CREATE UNIQUE NONCLUSTERED INDEX IX_JobPostings_JobCode 
    ON dbo.JobPostings(JobCode) 
    WHERE IsDeleted = 0;

CREATE UNIQUE NONCLUSTERED INDEX IX_JobPostings_Slug 
    ON dbo.JobPostings(Slug) 
    WHERE IsDeleted = 0;

-- 12. Index trên Hồ sơ ứng tuyển (JobPostingId và Email)
CREATE NONCLUSTERED INDEX IX_CandidateApplications_JobPostingId 
    ON dbo.CandidateApplications(JobPostingId);

CREATE NONCLUSTERED INDEX IX_CandidateApplications_Email 
    ON dbo.CandidateApplications(Email);
GO

-- ====================================================================================================
-- PHẦN 5: BỘ DỮ LIỆU MẪU THỰC TẾ CỦA TẬP ĐOÀN VINAMILK (SEED DATA)
-- ====================================================================================================
PRINT N'==> Bắt đầu nạp dữ liệu mẫu thực tế của Vinamilk...';

BEGIN TRANSACTION;

-- ----------------------------------------------------------------------------------------------------
-- 1. DỮ LIỆU ĐƠN VỊ TỔ CHỨC (ORGANIZATIONAL UNITS)
-- ----------------------------------------------------------------------------------------------------
-- Cấp 1: Trụ sở chính Vinamilk Tower (Quận 7)
INSERT INTO dbo.OrganizationalUnits
(
    UnitCode, UnitName, UnitType, Address, PhoneNumber, Email, TaxCode, ParentUnitId,
    FloorCount, MeetingRoomCount, HasExecutiveSuite, DivisionName, CreatedBy
)
VALUES
(
    N'HQ-VINAMILK', N'Tập đoàn Vinamilk - Trụ sở chính Vinamilk Tower', 1,
    N'Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP. Hồ Chí Minh',
    N'028-5415-5555', N'vinamilk@vinamilk.com.vn', N'0300588569', NULL,
    18, 12, 1, N'Hội Đồng Quản Trị & Ban Điều Hành', N'INIT_SEED'
);

DECLARE @HqId INT = SCOPE_IDENTITY();

-- Cấp 2.1: Nhà máy Sữa Việt Nam (Mega Factory) Bình Dương
INSERT INTO dbo.OrganizationalUnits
(
    UnitCode, UnitName, UnitType, Address, PhoneNumber, Email, TaxCode, ParentUnitId,
    FactoryCapacityLitersPerDay, ProductionLineCount, IsoStandard, EnvironmentalPermitNumber, CreatedBy
)
VALUES
(
    N'FAC-MEGABD', N'Nhà máy Sữa Việt Nam (Mega Factory Bình Dương)', 2,
    N'Lô CN2, Đường D1, KCN Mỹ Phước 2, Thị xã Bến Cát, Tỉnh Bình Dương',
    N'0274-355-6666', N'megafactory@vinamilk.com.vn', N'0300588569-002', @HqId,
    2400000.00, 18, N'ISO 9001:2015, FSSC 22000, ISO 14001, BRC Grade A', N'GP-MT-BD-2022-089', N'INIT_SEED'
);

DECLARE @FactoryId INT = SCOPE_IDENTITY();

-- Cấp 2.2: Resort Trang trại Bò sữa Vinamilk Tây Ninh (Quy mô 8.000 con)
INSERT INTO dbo.OrganizationalUnits
(
    UnitCode, UnitName, UnitType, Address, PhoneNumber, Email, TaxCode, ParentUnitId,
    HerdSize, TotalAreaHectares, BarnCount, GlobalGapCertificationNumber, CreatedBy
)
VALUES
(
    N'FARM-TAYNINH', N'Trang trại Bò sữa Công nghệ cao Vinamilk Tây Ninh', 3,
    N'Xã Long Khánh, Huyện Bến Cầu, Tỉnh Tây Ninh',
    N'0276-388-7777', N'tayninhfarm@vinamilk.com.vn', N'0300588569-015', @HqId,
    8000, 685.50, 12, N'GG-VN-2023-GLOBALGAP-9981', N'INIT_SEED'
);

DECLARE @FarmId INT = SCOPE_IDENTITY();

-- Cấp 2.3: Chuỗi Cửa hàng Bán lẻ Giấc Mơ Sữa Việt (Chi nhánh Quận 1)
INSERT INTO dbo.OrganizationalUnits
(
    UnitCode, UnitName, UnitType, Address, PhoneNumber, Email, TaxCode, ParentUnitId,
    StoreCode, ShelfAreaSquareMeters, ColdStorageCapacityLiters, BusinessHours, CreatedBy
)
VALUES
(
    N'RET-GMSV01', N'Hệ thống Cửa hàng Giấc Mơ Sữa Việt - CN Lê Duẩn Quận 1', 4,
    N'Số 25 Lê Duẩn, Phường Bến Nghé, Quận 1, TP. Hồ Chí Minh',
    N'028-3829-1111', N'store.leduan@giacmosuaviet.com.vn', N'0300588569-028', @HqId,
    N'GMSV-Q1-01', 120.00, 1500.00, N'07:30 - 21:30', N'INIT_SEED'
);

DECLARE @StoreId INT = SCOPE_IDENTITY();

-- ----------------------------------------------------------------------------------------------------
-- 2. DỮ LIỆU NHÂN SỰ ĐẦY ĐỦ 4 KHỐI (EMPLOYEES)
-- ----------------------------------------------------------------------------------------------------
-- [Khối 1: Văn phòng Trụ sở Vinamilk Tower]
-- Giám đốc Quản trị Nhân lực Tập đoàn
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    JobTitle, DeskLocation, ExtensionNumber, CubicleNumber, CreatedBy
)
VALUES
(
    N'VNM-OFF-001', N'Hùng', N'Nguyễn Văn', N'079080001234', '1980-05-15', 1,
    N'hungnv@vinamilk.com.vn', N'0903111222', N'Số 88 Nguyễn Thị Thập, Quận 7, TP.HCM',
    '2012-03-01', 1, 1, @HqId, NULL, 48000000.00, 12000000.00,
    N'Giám đốc Khối Quản trị Nguồn nhân lực', N'Tầng 16 Vinamilk Tower', N'1601', N'HQ-T16-EXEC01', N'INIT_SEED'
);

DECLARE @EmpDirectorId INT = SCOPE_IDENTITY();

-- Cập nhật Giám đốc quản lý cho Đơn vị HQ
UPDATE dbo.OrganizationalUnits SET ManagerEmployeeId = @EmpDirectorId WHERE Id = @HqId;

-- Chuyên viên C&B (Tiền lương & Phúc lợi)
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    JobTitle, DeskLocation, ExtensionNumber, CubicleNumber, CreatedBy
)
VALUES
(
    N'VNM-OFF-002', N'Trang', N'Trần Thị Thu', N'079192005678', '1992-08-20', 2,
    N'trangttt@vinamilk.com.vn', N'0918222333', N'Căn hộ Sunrise City, Quận 7, TP.HCM',
    '2018-06-15', 1, 1, @HqId, @EmpDirectorId, 22500000.00, 3500000.00,
    N'Chuyên viên Cao cấp Quản trị Tiền lương (C&B)', N'Tầng 16 Vinamilk Tower', N'1612', N'HQ-T16-CB02', N'INIT_SEED'
);

DECLARE @EmpOffice2Id INT = SCOPE_IDENTITY();

-- [Khối 2: Nhà máy Sữa Mega Bình Dương]
-- Kỹ sư Trưởng Vận hành Chuyền Chiết rót Tiệt trùng Tetra Pak
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    ProductionShiftGroup, HazardousAllowanceRate, MachineCertificationCode, SafetyLevel, CreatedBy
)
VALUES
(
    N'VNM-FAC-001', N'Long', N'Lê Hoàng', N'074085002468', '1985-11-10', 1,
    N'longlh@vinamilk.com.vn', N'0908333444', N'Khu Dân cư Mỹ Phước 2, Thị xã Bến Cát, Tỉnh Bình Dương',
    '2015-09-01', 2, 1, @FactoryId, @EmpDirectorId, 26000000.00, 4500000.00,
    N'Tổ Vận hành Tự động hóa Chiết rót Tetra Pak A', 2500000.00, N'CERT-TETRAPAK-E3-2022',
    N'Level 4 - An toàn Công nghệ Thực phẩm BRC & FSSC 22000', N'INIT_SEED'
);

DECLARE @EmpFactory1Id INT = SCOPE_IDENTITY();
UPDATE dbo.OrganizationalUnits SET ManagerEmployeeId = @EmpFactory1Id WHERE Id = @FactoryId;

-- Công nhân Kỹ thuật Tiệt trùng UHT
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    ProductionShiftGroup, HazardousAllowanceRate, MachineCertificationCode, SafetyLevel, CreatedBy
)
VALUES
(
    N'VNM-FAC-002', N'Minh', N'Phạm Văn', N'074195003698', '1995-04-18', 1,
    N'minhpv@vinamilk.com.vn', N'0977444555', N'Khu phố 3, Phường Mỹ Phước, Bến Cát, Bình Dương',
    '2021-02-10', 2, 1, @FactoryId, @EmpFactory1Id, 16500000.00, 2000000.00,
    N'Tổ Tiệt trùng UHT Siêu nhiệt Ca Kíp 2', 2000000.00, N'CERT-UHT-OPERATOR-2023',
    N'Level 3 - Tiêu chuẩn Vận hành Dây chuyền Sữa tiệt trùng', N'INIT_SEED'
);

DECLARE @EmpFactory2Id INT = SCOPE_IDENTITY();

-- [Khối 3: Trang trại Bò sữa Vinamilk Tây Ninh]
-- Bác sĩ Thú y Trưởng Giám sát Đàn Bò
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    SpecializationArea, BioSecurityClearanceCode, AnimalCareAllowanceRate, VeterinaryLicenseNumber, CreatedBy
)
VALUES
(
    N'VNM-FARM-001', N'Phong', N'Nguyễn Thanh', N'072083007890', '1983-09-25', 1,
    N'phongnt@vinamilk.com.vn', N'0912555666', N'Thị trấn Bến Cầu, Huyện Bến Cầu, Tỉnh Tây Ninh',
    '2016-10-01', 3, 1, @FarmId, @EmpDirectorId, 28000000.00, 4500000.00,
    N'Bác sĩ Thú y Lâm sàng & Kiểm soát Dịch tễ Đàn Bò Thuần Chủng HF', N'BIO-SEC-LVL4-TN99',
    3500000.00, N'VET-LIC-VN-2015-089', N'INIT_SEED'
);

DECLARE @EmpFarm1Id INT = SCOPE_IDENTITY();
UPDATE dbo.OrganizationalUnits SET ManagerEmployeeId = @EmpFarm1Id WHERE Id = @FarmId;

-- Kỹ sư Dinh dưỡng Cỏ & Khẩu phần TMR
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    SpecializationArea, BioSecurityClearanceCode, AnimalCareAllowanceRate, VeterinaryLicenseNumber, CreatedBy
)
VALUES
(
    N'VNM-FARM-002', N'Cường', N'Đặng Quốc', N'072194004561', '1994-12-05', 1,
    N'cuongdq@vinamilk.com.vn', N'0988666777', N'Xã Long Khánh, Huyện Bến Cầu, Tỉnh Tây Ninh',
    '2020-04-15', 3, 1, @FarmId, @EmpFarm1Id, 18500000.00, 2500000.00,
    N'Kỹ sư Phối trộn Khẩu phần Dinh dưỡng TMR & Đồng cỏ Yến mạch', N'BIO-SEC-LVL3-TN42',
    2500000.00, NULL, N'INIT_SEED'
);

DECLARE @EmpFarm2Id INT = SCOPE_IDENTITY();

-- [Khối 4: Cửa hàng Giấc Mơ Sữa Việt Lê Duẩn Quận 1]
-- Cửa hàng trưởng (Store Manager)
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    CashRegisterId, RetailSalesBonusRate, UniformSize, CustomerServiceRating, CreatedBy
)
VALUES
(
    N'VNM-RET-001', N'Phượng', N'Võ Thị Bích', N'079190008899', '1990-07-12', 2,
    N'phuongvtb@giacmosuaviet.com.vn', N'0909777888', N'120 Nguyễn Đình Chiểu, Phường Đa Kao, Quận 1, TP.HCM',
    '2017-08-01', 4, 1, @StoreId, @EmpDirectorId, 17500000.00, 2500000.00,
    N'POS-GMSV-Q1-01', 3.50, N'M', 4.95, N'INIT_SEED'
);

DECLARE @EmpStore1Id INT = SCOPE_IDENTITY();
UPDATE dbo.OrganizationalUnits SET ManagerEmployeeId = @EmpStore1Id WHERE Id = @StoreId;

-- Nhân viên Bán hàng & Tư vấn Sữa tươi
INSERT INTO dbo.Employees
(
    EmployeeCode, FirstName, LastName, CitizenId, DateOfBirth, Gender, Email, PhoneNumber, Address,
    HireDate, EmployeeType, IsActive, OrganizationalUnitId, ManagerId, BaseSalary, Allowance,
    CashRegisterId, RetailSalesBonusRate, UniformSize, CustomerServiceRating, CreatedBy
)
VALUES
(
    N'VNM-RET-002', N'Đạt', N'Đỗ Thành', N'079201009988', '2001-03-22', 1,
    N'datdt@giacmosuaviet.com.vn', N'0938888999', N'45 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP.HCM',
    '2024-11-01', 4, 1, @StoreId, @EmpStore1Id, 10500000.00, 1500000.00,
    N'POS-GMSV-Q1-02', 2.00, N'L', 4.80, N'INIT_SEED'
);

DECLARE @EmpStore2Id INT = SCOPE_IDENTITY();

-- ----------------------------------------------------------------------------------------------------
-- 3. DỮ LIỆU HỢP ĐỒNG LAO ĐỘNG (LABOR CONTRACTS)
-- ----------------------------------------------------------------------------------------------------
-- HĐ không xác định thời hạn (Giám đốc Khối & Kỹ sư Trưởng)
INSERT INTO dbo.LaborContracts
(
    ContractNumber, EmployeeId, ContractType, StartDate, EndDate, BasicSalary, InsuranceSalary,
    WorkingHoursPerWeek, Status, SignDate, SeverancePackageTerms, AnnualTenureBonusRate, CreatedBy
)
VALUES
(
    N'HDLD-2015-VNM001', @EmpDirectorId, 3, '2015-03-01', NULL, 48000000.00, 36000000.00,
    40, 2, '2015-02-25', N'Chế độ trợ cấp thôi việc cấp Lãnh đạo cao cấp Tập đoàn Vinamilk', 6.00, N'INIT_SEED'
),
(
    N'HDLD-2018-VNM003', @EmpFactory1Id, 3, '2018-09-01', NULL, 26000000.00, 22000000.00,
    40, 2, '2018-08-25', N'Theo quy định Hợp đồng không thời hạn Khối Sản xuất Mega Factory', 5.00, N'INIT_SEED'
);

-- HĐ xác định thời hạn (Chuyên viên C&B, Bác sĩ thú y, Cửa hàng trưởng)
INSERT INTO dbo.LaborContracts
(
    ContractNumber, EmployeeId, ContractType, StartDate, EndDate, BasicSalary, InsuranceSalary,
    WorkingHoursPerWeek, Status, SignDate, DurationMonths, MaximumRenewalsAllowed, CurrentRenewalCount, CreatedBy
)
VALUES
(
    N'HDLD-2023-VNM002', @EmpOffice2Id, 2, '2023-06-15', '2026-06-14', 22500000.00, 19000000.00,
    40, 2, '2023-06-10', 36, 2, 1, N'INIT_SEED'
),
(
    N'HDLD-2022-VNM005', @EmpFarm1Id, 2, '2022-10-01', '2025-09-30', 28000000.00, 24000000.00,
    40, 2, '2022-09-20', 36, 2, 1, N'INIT_SEED'
),
(
    N'HDLD-2023-VNM007', @EmpStore1Id, 2, '2023-08-01', '2025-07-31', 17500000.00, 15000000.00,
    40, 2, '2023-07-25', 24, 2, 1, N'INIT_SEED'
),
(
    N'HDLD-2024-VNM004', @EmpFactory2Id, 2, '2024-02-10', '2026-02-09', 16500000.00, 14000000.00,
    40, 2, '2024-02-01', 24, 2, 0, N'INIT_SEED'
);

-- HĐ thử việc (Nhân viên bán lẻ mới thử việc 60 ngày)
INSERT INTO dbo.LaborContracts
(
    ContractNumber, EmployeeId, ContractType, StartDate, EndDate, BasicSalary, InsuranceSalary,
    WorkingHoursPerWeek, Status, SignDate, MentorEmployeeId, ProbationEvaluationScore, ProbationPassed, EvaluationFeedback, CreatedBy
)
VALUES
(
    N'HDTV-2024-VNM008', @EmpStore2Id, 1, '2024-11-01', '2024-12-31', 10500000.00, 9000000.00,
    40, 2, '2024-10-28', @EmpStore1Id, 92.50, 1, N'Hoàn thành xuất sắc thử việc, giao tiếp khách hàng tốt, nắm vững sản phẩm Vinamilk', N'INIT_SEED'
);

-- ----------------------------------------------------------------------------------------------------
-- 4. DỮ LIỆU CA LÀM VIỆC (SHIFTS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.Shifts
(
    ShiftCode, ShiftName, ShiftType, StartTime, EndTime, BreakMinutes, MultiplierRate, OrganizationalUnitId, CreatedBy
)
VALUES
(
    N'SH-OFF-STD', N'Ca Hành chính Trụ sở Vinamilk Tower (08:00 - 17:00)', 1,
    '08:00:00', '17:00:00', 60, 1.00, @HqId, N'INIT_SEED'
),
(
    N'SH-FAC-CA1', N'Ca 1 Nhà máy Mega - Sáng (06:00 - 14:00)', 2,
    '06:00:00', '14:00:00', 45, 1.00, @FactoryId, N'INIT_SEED'
),
(
    N'SH-FAC-CA2', N'Ca 2 Nhà máy Mega - Chiều (14:00 - 22:00)', 3,
    '14:00:00', '22:00:00', 45, 1.10, @FactoryId, N'INIT_SEED'
),
(
    N'SH-FAC-CA3', N'Ca 3 Nhà máy Mega - Đêm UHT (22:00 - 06:00)', 4,
    '22:00:00', '06:00:00', 45, 1.30, @FactoryId, N'INIT_SEED'
),
(
    N'SH-FARM-EARLY', N'Ca Chăm sóc Đàn bò & Vắt sữa Sáng (04:30 - 12:30)', 5,
    '04:30:00', '12:30:00', 60, 1.15, @FarmId, N'INIT_SEED'
),
(
    N'SH-RET-SANG', N'Ca Bán lẻ Giấc Mơ Sữa Việt Sáng (07:30 - 15:30)', 7,
    '07:30:00', '15:30:00', 60, 1.00, @StoreId, N'INIT_SEED'
),
(
    N'SH-RET-TOI', N'Ca Bán lẻ Giấc Mơ Sữa Việt Tối (14:30 - 22:00)', 8,
    '14:30:00', '22:00:00', 30, 1.10, @StoreId, N'INIT_SEED'
);

DECLARE @ShiftOffId INT = (SELECT Id FROM dbo.Shifts WHERE ShiftCode = N'SH-OFF-STD');
DECLARE @ShiftFac1Id INT = (SELECT Id FROM dbo.Shifts WHERE ShiftCode = N'SH-FAC-CA1');
DECLARE @ShiftFac3Id INT = (SELECT Id FROM dbo.Shifts WHERE ShiftCode = N'SH-FAC-CA3');
DECLARE @ShiftFarmId INT = (SELECT Id FROM dbo.Shifts WHERE ShiftCode = N'SH-FARM-EARLY');
DECLARE @ShiftRetId INT = (SELECT Id FROM dbo.Shifts WHERE ShiftCode = N'SH-RET-SANG');

-- ----------------------------------------------------------------------------------------------------
-- 5. DỮ LIỆU CHẤM CÔNG (ATTENDANCES)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.Attendances
(
    EmployeeId, ShiftId, AttendanceDate, CheckInTime, CheckOutTime, WorkHours, OvertimeHours, Status, Note, CreatedBy
)
VALUES
-- Chuyên viên C&B Văn phòng Tower (Có làm thêm giờ phân tích bảng lương)
(@EmpOffice2Id, @ShiftOffId, '2025-01-06', '07:55:00', '19:30:00', 8.00, 2.50, 1, N'Hoàn tất phân tích dữ liệu tiền lương Q1', N'INIT_SEED'),
(@EmpOffice2Id, @ShiftOffId, '2025-01-07', '08:02:00', '17:05:00', 8.00, 0.00, 1, N'Đúng giờ', N'INIT_SEED'),

-- Kỹ sư Nhà máy Mega Bình Dương (Làm thêm giờ ca vận hành)
(@EmpFactory1Id, @ShiftFac1Id, '2025-01-06', '05:50:00', '16:00:00', 8.00, 2.00, 1, N'Giám sát bảo trì định kỳ chuyền chiết rót 18', N'INIT_SEED'),
(@EmpFactory2Id, @ShiftFac3Id, '2025-01-06', '21:55:00', '06:10:00', 8.00, 0.00, 1, N'Vận hành ca đêm đạt chỉ tiêu mẻ sữa tiệt trùng', N'INIT_SEED'),

-- Bác sĩ Thú y Trang trại Tây Ninh
(@EmpFarm1Id, @ShiftFarmId, '2025-01-06', '04:25:00', '15:30:00', 8.00, 3.00, 1, N'Cấp cứu sinh sản bê non giống HF thành công', N'INIT_SEED'),

-- Cửa hàng bán lẻ GMSV Quận 1
(@EmpStore2Id, @ShiftRetId, '2025-01-06', '07:25:00', '15:35:00', 8.00, 0.00, 1, N'Mở cửa chuẩn bị quầy lạnh sữa chua đúng giờ', N'INIT_SEED');

-- ----------------------------------------------------------------------------------------------------
-- 6. DỮ LIỆU ĐƠN NGHỈ PHÉP (LEAVE REQUESTS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.LeaveRequests
(
    EmployeeId, LeaveType, StartDate, EndDate, TotalDays, Reason, Status, ApprovedById, ApprovalNotes, DecisionDate, CreatedBy
)
VALUES
(
    @EmpOffice2Id, 1, '2025-02-03', '2025-02-04', 2.00,
    N'Nghỉ phép năm kết hợp về quê đón Tết Nguyên Đán', 2,
    @EmpDirectorId, N'Đã duyệt, bàn giao công việc C&B cho đồng nghiệp', '2025-01-15 09:30:00', N'INIT_SEED'
),
(
    @EmpFactory2Id, 2, '2025-01-12', '2025-01-12', 1.00,
    N'Nghỉ ốm theo chỉ định của Bác sĩ phòng y tế Mega Factory', 2,
    @EmpFactory1Id, N'Duyệt nghỉ ốm hưởng BHXH, đã sắp xếp người thay thế ca', '2025-01-12 07:00:00', N'INIT_SEED'
);

-- ----------------------------------------------------------------------------------------------------
-- 7. DỮ LIỆU GIAO VÀ ĐÁNH GIÁ CHỈ TIÊU KPI (KPI ASSIGNMENTS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.KpiAssignments
(
    EmployeeId, EvaluatorId, Period, Title, Description, TargetMetric, WeightPercentage,
    ActualResult, SelfEvaluationScore, FinalScore, Status, Feedback, SubmissionDate, EvaluationDate, CreatedBy
)
VALUES
(
    @EmpOffice2Id, @EmpDirectorId, N'2024-Q4',
    N'Đảm bảo độ chính xác tính lương và phúc lợi tập đoàn',
    N'Tính toán bảng lương 100% đúng hạn, không sai sót dữ liệu thuế TNCN & BHXH cho toàn bộ khối văn phòng.',
    N'Tỷ lệ đúng hạn 100%, tỷ lệ khiếu nại = 0%', 40.00,
    N'Hoàn tất 100% bảng lương 3 tháng Q4 đúng hạn, tỷ lệ khiếu nại 0%', 98.00, 98.00, 5,
    N'Xuất sắc, nắm vững chính sách nhân sự và biểu thuế mới', '2025-01-03 14:00:00', '2025-01-05 10:00:00', N'INIT_SEED'
),
(
    @EmpFactory1Id, @EmpDirectorId, N'2024-Q4',
    N'Chỉ số OEE Hiệu suất Hoạt động Thiết bị Dây chuyền Tetra Pak',
    N'Duy trì hiệu suất vận hành hệ thống chiết rót sữa tiệt trùng đạt mức chuẩn thế giới (World Class Manufacturing).',
    N'OEE >= 88.5%, Tỷ lệ hao hụt nguyên liệu bao bì < 0.8%', 50.00,
    N'OEE thực tế đạt 91.2%, hao hụt bao bì 0.65%', 95.00, 95.00, 5,
    N'Vận hành vượt chỉ tiêu, tiết kiệm hàng trăm triệu chi phí phế phẩm cho Mega Factory', '2025-01-04 11:00:00', '2025-01-05 15:30:00', N'INIT_SEED'
),
(
    @EmpFarm1Id, @EmpDirectorId, N'2024-Q4',
    N'Kiểm soát An toàn Sinh học & Tỷ lệ Bò Khỏe Mạnh Chuẩn GlobalGAP',
    N'Đảm bảo 100% đàn bò Tây Ninh tuân thủ vệ sinh thú y, không phát sinh dịch bệnh truyền nhiễm, sản lượng sữa chuẩn.',
    N'Tỷ lệ đàn bò khỏe mạnh >= 98.5%, Điểm kiểm toán GlobalGAP >= 95%', 50.00,
    N'Tỷ lệ đàn bò khỏe đạt 99.1%, đợt kiểm toán độc lập GlobalGAP đạt 98/100', 100.00, 98.00, 5,
    N'Quản lý đàn bò sinh thái xuất sắc, đàn bò phát triển rất đồng đều', '2025-01-02 16:00:00', '2025-01-04 09:00:00', N'INIT_SEED'
),
(
    @EmpStore1Id, @EmpDirectorId, N'2024-Q4',
    N'Chỉ tiêu Doanh số Chuỗi Cửa hàng Bán lẻ Giấc Mơ Sữa Việt',
    N'Đạt doanh số bán lẻ dòng sản phẩm sữa chua, sữa hạt, sữa tươi thanh trùng 100% Organic tại cửa hàng Lê Duẩn.',
    N'Doanh số tối thiểu 1.8 tỷ VNĐ/quý, Tỷ lệ hài lòng khách hàng >= 95%', 60.00,
    N'Doanh thu đạt 2.05 tỷ VNĐ (vượt 113.8%), Điểm hài lòng khách hàng 98.2%', 96.00, 96.00, 5,
    N'Tư vấn tận tâm, tổ chức chương trình dùng thử sản phẩm mới rất hiệu quả', '2025-01-02 17:00:00', '2025-01-03 16:00:00', N'INIT_SEED'
);

-- ----------------------------------------------------------------------------------------------------
-- 8. DỮ LIỆU TỜ TRÌNH ĐỀ XUẤT LƯƠNG (SALARY PROPOSALS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.SalaryProposals
(
    EmployeeId, ProposedById, CurrentSalary, ProposedSalary, CurrentAllowance, ProposedAllowance,
    Reason, EffectiveDate, Status, DepartmentHeadApprovedById, DepartmentHeadNotes,
    HRDirectorApprovedById, HRDirectorNotes, FinalDecisionDate, CreatedBy
)
VALUES
(
    @EmpOffice2Id, @EmpDirectorId, 20000000.00, 22500000.00, 3000000.00, 3500000.00,
    N'Đề xuất nâng bậc lương chuyên môn: Nhân sự hoàn thành xuất sắc đề án tối ưu hóa quy trình C&B tự động hóa năm 2024.',
    '2025-01-01', 4, @EmpDirectorId, N'Nhất trí thông qua mức lương mới',
    @EmpDirectorId, N'Phê duyệt nâng lương theo quyết định số 45/QĐ-HR-VNM', '2024-12-28 15:00:00', N'INIT_SEED'
);

-- ----------------------------------------------------------------------------------------------------
-- 9. DỮ LIỆU BẢNG LƯƠNG & PHIẾU LƯƠNG CHI TIẾT (PAYSLIPS) KỲ 2025-01 (ĐA HÌNH & LUẬT LAO ĐỘNG VN)
-- ----------------------------------------------------------------------------------------------------
-- Phiếu lương Chuyên viên C&B (OfficeEmployee)
INSERT INTO dbo.PaySlips
(
    EmployeeId, PayrollPeriod, StandardWorkDays, ActualWorkDays, BaseSalary, Allowance, LunchAllowance,
    OvertimeHours, OvertimePay, NightShiftHours, NightShiftPay, HazardousAllowance, SalesCommission,
    KpiBonus, OtherBonuses, GrossSalary, SocialInsuranceDeduction, HealthInsuranceDeduction,
    UnemploymentInsuranceDeduction, DependentCount, PersonalDeduction, DependentDeduction,
    TaxableIncome, PersonalIncomeTax, TotalDeductions, NetSalary, Status, PaymentDate, TransactionReference, CreatedBy
)
VALUES
(
    @EmpOffice2Id, N'2025-01', 22.00, 22.00, 22500000.00, 3500000.00, 730000.00,
    10.00, 1989000.00, 0.00, 0.00, 0.00, 0.00,
    3307500.00, 500000.00, 32526500.00, 1800000.00, 337500.00,
    225000.00, 1, 11000000.00, 4400000.00,
    14764000.00, 1464600.00, 3827100.00, 28699400.00, 4, '2025-02-05 10:30:00', N'VCB-VNM-SAL-202501-0012', N'INIT_SEED'
),
-- Phiếu lương Kỹ sư Vận hành Nhà máy Mega (FactoryWorker - Ca đêm +30%, Độc hại)
(
    @EmpFactory1Id, N'2025-01', 22.00, 22.00, 26000000.00, 4500000.00, 730000.00,
    16.00, 3886000.00, 40.00, 1772727.00, 2500000.00, 0.00,
    3705000.00, 500000.00, 43593727.00, 2080000.00, 390000.00,
    260000.00, 2, 11000000.00, 8800000.00,
    20963727.00, 2242745.00, 4972745.00, 38620982.00, 4, '2025-02-05 10:30:00', N'VCB-VNM-SAL-202501-0045', N'INIT_SEED'
),
-- Phiếu lương Bác sĩ Thú y Trang trại Tây Ninh (FarmSpecialist)
(
    @EmpFarm1Id, N'2025-01', 22.00, 22.00, 28000000.00, 4500000.00, 730000.00,
    12.00, 3345000.00, 0.00, 0.00, 0.00, 0.00,
    4116000.00, 500000.00, 41191000.00, 2240000.00, 420000.00,
    280000.00, 1, 11000000.00, 4400000.00,
    22851000.00, 2620200.00, 5560200.00, 35630800.00, 4, '2025-02-05 10:30:00', N'VCB-VNM-SAL-202501-0078', N'INIT_SEED'
),
-- Phiếu lương Cửa hàng trưởng Chuỗi Giấc Mơ Sữa Việt (StoreAssociate - Hoa hồng chuỗi)
(
    @EmpStore1Id, N'2025-01', 22.00, 22.00, 17500000.00, 2500000.00, 730000.00,
    8.00, 1193000.00, 0.00, 0.00, 0.00, 4250000.00,
    2520000.00, 500000.00, 29193000.00, 1400000.00, 262500.00,
    175000.00, 1, 11000000.00, 4400000.00,
    11955500.00, 1043325.00, 2880825.00, 26312175.00, 4, '2025-02-05 10:30:00', N'VCB-VNM-SAL-202501-0099', N'INIT_SEED'
);

-- ----------------------------------------------------------------------------------------------------
-- 10. DỮ LIỆU PHỤ LỤC HỢP ĐỒNG ĐIỆN TỬ (ELECTRONIC CONTRACT APPENDICES - CEO OVERRIDE & REGULAR)
-- ----------------------------------------------------------------------------------------------------
DECLARE @ContractOffice2Id INT;
SELECT TOP 1 @ContractOffice2Id = Id FROM dbo.LaborContracts WHERE EmployeeId = @EmpOffice2Id;

DECLARE @ContractFactory1Id INT;
SELECT TOP 1 @ContractFactory1Id = Id FROM dbo.LaborContracts WHERE EmployeeId = @EmpFactory1Id;

INSERT INTO dbo.ElectronicContractAppendices
(
    AppendixNumber, LaborContractId, EmployeeId, EffectiveDate, SignDate,
    PreviousSalary, NewSalary, PreviousAllowance, NewAllowance, AdjustmentReason,
    IsCeoOverride, CeoOverrideCode, DigitalSignatureHash, SignerName, SignerTitle, IsSigned, CreatedBy
)
VALUES
(
    N'PLHD-2025-VNM-001', @ContractOffice2Id, @EmpOffice2Id, '2025-01-01', '2024-12-28',
    20000000.00, 22500000.00, 3000000.00, 3500000.00,
    N'Nâng bậc lương định kỳ Khối Văn phòng theo thẩm định C&B và phê duyệt Hội đồng Nhân sự',
    0, NULL, N'SHA256-VNM-PLHD-2025-001-9F83A20B71E4C118B8A992', N'Bùi Thị Hương', N'Giám Đốc Nhân Sự (HRD) - Vinamilk', 1, N'INIT_SEED'
),
(
    N'PLHD-2025-FAST-001', @ContractFactory1Id, @EmpFactory1Id, '2025-01-01', '2024-12-30',
    26000000.00, 30000000.00, 4500000.00, 5000000.00,
    N'Tổng Giám Đốc ĐẶC CÁCH tăng lương vượt khung: Sáng kiến tiết kiệm năng lượng 15 tỷ VNĐ tại Mega Factory',
    1, N'CEO-OVERRIDE-2025-VNM-001', N'SHA256-CEO-VNM-MKLIEN-8899A1C2E4F0B71190284AA9234BCDEF', N'Mai Kiều Liên', N'Tổng Giám Đốc (CEO) - Vinamilk', 1, N'CEO_OVERRIDE'
);

-- ----------------------------------------------------------------------------------------------------
-- 11. DỮ LIỆU TÀI SẢN & THIẾT BỊ CẤP PHÁT (COMPANY ASSETS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.CompanyAssets
(
    AssetCode, AssetName, Category, SerialNumber, AssignedToEmployeeId, AssignedDate,
    OriginalValue, Status, Notes, CreatedBy
)
VALUES
(
    N'AST-LAP-001', N'Laptop Dell Latitude 7440 Core i7 vPro 32GB RAM', N'Thiết bị CNTT Văn phòng',
    N'SN-DELL-HQ-88991', @EmpOffice2Id, '2023-06-15', 34500000.00, 2,
    N'Cấp phát chuyên môn phân tích dữ liệu nhân sự C&B', N'INIT_SEED'
),
(
    N'AST-RADIO-002', N'Bộ đàm Công nghiệp Chống cháy nổ Motorola GP328', N'Thiết bị An toàn Nhà máy',
    N'SN-MOTO-FAC-4421', @EmpFactory1Id, '2020-05-10', 12500000.00, 2,
    N'Sử dụng trong khu vực chuyền chiết rót Mega Factory', N'INIT_SEED'
),
(
    N'AST-EV-003', N'Xe điện Tuần tra Khu chuồng trại Vinamilk Eco Farm', N'Phương tiện Di chuyển Nội bộ',
    N'SN-EV-FARM-007', @EmpFarm1Id, '2021-08-20', 145000000.00, 2,
    N'Phương tiện phục vụ kiểm tra sức khỏe đàn bò trên diện tích 685 ha', N'INIT_SEED'
),
(
    N'AST-POS-004', N'Hệ thống Máy POS Bán hàng Cảm ứng Sunmi T2 Pro', N'Thiết bị Bán lẻ',
    N'SN-POS-SUNMI-991', @EmpStore1Id, '2023-08-01', 18900000.00, 2,
    N'Máy tính tiền và in hóa đơn cửa hàng Giấc Mơ Sữa Việt', N'INIT_SEED'
-- ----------------------------------------------------------------------------------------------------
-- 13. DỮ LIỆU TIN TUYỂN DỤNG CÔNG KHAI (JOB POSTINGS - CỔNG CAREERS VINAMILK)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.JobPostings
(
    JobCode, Title, Slug, UnitType, Department, WorkLocation, City, RegionId,
    EmploymentType, ExperienceLevel, EducationRequirement, SalaryMin, SalaryMax, SalaryDisplay,
    DatePosted, ValidThrough, Description, Requirements, Benefits, IsActive, IsFeatured, ViewCount, CreatedBy
)
VALUES
(
    N'JOB-VNM-FAC-01', N'Kỹ sư Vận hành Dây chuyền Tetra Pak Mega Factory',
    N'ky-su-van-hanh-day-chuyen-tetra-pak-mega-factory', 2, N'Khối Sản xuất & Kỹ thuật Nhà máy',
    N'Khu công nghiệp Mỹ Phước 2, Thị xã Bến Cát', N'Bình Dương', 3,
    N'FULL_TIME', N'2 - 5 năm kinh nghiệm', N'Đại học chuyên ngành Cơ điện tử / Tự động hóa',
    22000000.00, 30000000.00, N'22,000,000 - 30,000,000 VNĐ',
    '2025-01-05', '2025-03-31',
    N'Vận hành dây chuyền chiết rót tiệt trùng tốc độ cao Tetra Pak A3/Speed. Giám sát các thông số kỹ thuật SCADA/PLC, thực hiện bảo trì ngăn ngừa và khắc phục sự cố vi mô. Đảm bảo năng suất OEE đạt trên 95% và tuân thủ tuyệt đối quy định an toàn vệ sinh thực phẩm FSSC 22000.',
    N'- Tốt nghiệp Đại học chuyên ngành Cơ khí, Tự động hóa, Cơ điện tử.\n- Tối thiểu 2 năm kinh nghiệm vận hành dây chuyền tự động hóa thực phẩm, đồ uống.\n- Có thể làm việc theo ca (gồm ca 3 có phụ cấp đêm 30%).\n- Tinh thần trách nhiệm cao, tỉ mỉ và tuân thủ quy trình 5S.',
    N'- Thu nhập cạnh tranh hàng đầu ngành FMCG: Lương cơ bản + Phụ cấp ca đêm 30% + Thưởng hiệu suất OEE.\n- Phụ cấp độc hại công nghiệp và bữa ăn ca dinh dưỡng tiệt trùng Vinamilk.\n- Thưởng tháng 13, thưởng hiệu quả kinh doanh cuối năm từ 2-4 tháng lương.\n- Bảo hiểm sức khỏe Vinamilk Care VIP toàn diện cho bản thân và gia đình.',
    1, 1, 342, N'INIT_SEED'
),
(
    N'JOB-VNM-RET-01', N'Cửa hàng trưởng Chuỗi Cửa hàng Giấc Mơ Sữa Việt Lê Duẩn',
    N'cua-hang-truong-chuoi-cua-hang-giac-mo-sua-viet-le-duan', 4, N'Khối Kinh doanh & Chuỗi Bán lẻ',
    N'120 Nguyễn Đình Chiểu, Phường Đa Kao, Quận 1', N'Hồ Chí Minh', 3,
    N'FULL_TIME', N'1 - 3 năm kinh nghiệm', N'Cao đẳng / Đại học Quản trị kinh doanh, Bán lẻ',
    16000000.00, 24000000.00, N'16,000,000 - 24,000,000 VNĐ + Doanh số',
    '2025-01-10', '2025-04-15',
    N'Quản lý toàn diện hoạt động kinh doanh tại cửa hàng Giấc Mơ Sữa Việt. Điều phối nhân viên bán hàng, kiểm soát tồn kho kho lạnh, tư vấn chuyên sâu các dòng sữa tươi dinh dưỡng cao cấp, phát triển doanh thu và duy trì điểm hài lòng khách hàng CSAT trên 4.9 sao.',
    N'- Tốt nghiệp Cao đẳng trở lên chuyên ngành Kinh tế, Thương mại, QTKD.\n- Có kinh nghiệm làm Quản lý / Trưởng ca chuỗi bán lẻ, siêu thị tiện lợi, F&B.\n- Kỹ năng giao tiếp đàm phán xuất sắc, kỹ năng quản lý đội nhóm.\n- Trung thực, yêu thích ngành hàng tiêu dùng nhanh (FMCG).',
    N'- Lương cơ bản + Thưởng hoa hồng chuỗi bán lẻ không giới hạn theo doanh thu.\n- Cơ hội thăng tiến lên Quản lý khu vực (Area Sales Manager) sau 12 tháng.\n- Trang bị đồng phục thiết kế thương hiệu mới Vinamilk cao cấp.\n- Hưởng đầy đủ chế độ BHXH, BHYT, BHTN và quà tặng sản phẩm sữa Vinamilk các dịp lễ.',
    1, 1, 218, N'INIT_SEED'
),
(
    N'JOB-VNM-FARM-01', N'Bác sĩ Thú y Lâm sàng & Kiểm soát Dịch tễ Bò sữa Organic',
    N'bac-si-thu-y-lam-sang-kiem-soat-dich-te-bo-sua-organic', 3, N'Khối Trang trại & Nông nghiệp Công nghệ cao',
    N'Trang trại Vinamilk Eco Farm, Thị trấn Bến Cầu', N'Tây Ninh', 3,
    N'FULL_TIME', N'2 - 4 năm kinh nghiệm', N'Bác sĩ Thú y / Chăn nuôi Thú y',
    25000000.00, 35000000.00, N'25,000,000 - 35,000,000 VNĐ',
    '2025-01-12', '2025-03-31',
    N'Chăm sóc sức khỏe, phòng ngừa dịch tễ và đỡ đẻ cho đàn bò thuần chủng HF và bò A2 Organic nhập khẩu. Ứng dụng phần mềm quản lý đàn bò SCR Heatime (Israel) để theo dõi thân nhiệt, sức nhai lại và thể trạng đàn bò công nghệ 4.0.',
    N'- Tốt nghiệp Đại học chuyên ngành Thú y, Bác sĩ Thú y.\n- Có chứng chỉ hành nghề thú y hợp lệ.\n- Kinh nghiệm điều trị lâm sàng gia súc lớn (ưu tiên bò sữa).\n- Sẵn sàng lưu trú tại khu chuyên gia của Trang trại Sinh thái Tây Ninh.',
    N'- Mức lương và phụ cấp thú y chuyên biệt hấp dẫn.\n- Cấp phòng nghỉ tiện nghi chuẩn resort tại khuôn viên trang trại Eco Farm.\n- Hỗ trợ chi phí đi lại TP.HCM - Tây Ninh hàng tuần.\n- Được đào tạo chuyên sâu bởi các chuyên gia chăn nuôi bò sữa hàng đầu từ Thụy Điển và Israel.',
    1, 0, 156, N'INIT_SEED'
);

DECLARE @JobFacId INT;
SELECT TOP 1 @JobFacId = Id FROM dbo.JobPostings WHERE JobCode = N'JOB-VNM-FAC-01';

DECLARE @JobRetId INT;
SELECT TOP 1 @JobRetId = Id FROM dbo.JobPostings WHERE JobCode = N'JOB-VNM-RET-01';

-- ----------------------------------------------------------------------------------------------------
-- 14. DỮ LIỆU HỒ SƠ ỨNG TUYỂN CỦA ỨNG VIÊN (CANDIDATE APPLICATIONS)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.CandidateApplications
(
    JobPostingId, FullName, Email, PhoneNumber, CitizenId, DateOfBirth, Gender, Address,
    ResumeFileName, CoverLetter, PortfolioUrl, Status, AppliedAt, InterviewDate, InterviewScore, EvaluationNotes, CreatedBy
)
VALUES
(
    @JobFacId, N'Hoàng Minh Đức', N'hoangminhduc@gmail.com', N'0903889922', N'079196001245',
    '1996-08-14', 1, N'Số 88 Đại lộ Bình Dương, Phường Phú Hòa, TP. Thủ Dầu Một, Tỉnh Bình Dương',
    N'CV_HoangMinhDuc_AutomationEngineer.pdf',
    N'Tôi có 4 năm kinh nghiệm vận hành dây chuyền chiết rót tốc độ cao tại công ty đồ uống quốc tế, am hiểu sâu về hệ thống PLC Siemens và tiêu chuẩn FSSC 22000. Rất mong muốn được cống hiến cho Vinamilk.',
    N'https://linkedin.com/in/hoangminhduc-automation', 4, DATEADD(DAY, -7, SYSUTCDATETIME()),
    DATEADD(DAY, -3, SYSUTCDATETIME()), 92.50,
    N'Ứng viên xuất sắc: Vượt qua bài kiểm tra kỹ thuật điều khiển tự động hóa 9.5/10. Tác phong công nghiệp tốt, sẵn sàng nhận việc ngay.', N'CAREERS_PORTAL'
),
(
    @JobRetId, N'Lê Thu Thảo', N'lethuthao.retail@gmail.com', N'0918776655', N'079201004567',
    '2000-11-20', 2, N'45 Hai Bà Trưng, Phường Bến Nghé, Quận 1, TP.HCM',
    N'CV_LeThuThao_StoreManager.pdf',
    N'Với kinh nghiệm 2 năm làm Trưởng ca tại chuỗi tiện lợi FamilyMart, tôi tự tin sẽ quản lý tốt doanh số và trải nghiệm dịch vụ khách hàng tại cửa hàng Giấc Mơ Sữa Việt.',
    NULL, 3, DATEADD(DAY, -4, SYSUTCDATETIME()),
    DATEADD(DAY, -1, SYSUTCDATETIME()), 88.00,
    N'Giao tiếp hoạt ngôn, xử lý tình huống khiếu nại khách hàng khéo léo. Đang chờ phê duyệt thư mời nhận việc.', N'CAREERS_PORTAL'
);

-- ----------------------------------------------------------------------------------------------------
-- 15. DỮ LIỆU NHẬT KÝ KIỂM TOÁN (AUDIT LOGS - BẢO MẬT & CEO FAST-TRACK OVERRIDE)
-- ----------------------------------------------------------------------------------------------------
INSERT INTO dbo.AuditLogs
(
    TableName, Action, EntityId, OldValues, NewValues, PerformedBy, PerformedAt, IpAddress
)
VALUES
(
    N'Database', N'INITIALIZE', N'VinamilkHRMDb', NULL,
    N'{"Status":"Success","SchemaVersion":"1.0.0","Modules":["OrgUnits","Employees","Contracts","Shifts","Attendances","KPIs","PaySlips","Assets","Appendices","JobPostings","Applications"]}',
    N'PrincipalSoftwareArchitect', SYSUTCDATETIME(), N'127.0.0.1'
),
(
    N'Employees', N'PROMOTE_SALARY', CAST(@EmpOffice2Id AS NVARCHAR(50)),
    N'{"BaseSalary":20000000,"Allowance":3000000}',
    N'{"BaseSalary":22500000,"Allowance":3500000,"Reason":"Nâng bậc lương định kỳ 2025"}',
    N'hungnv@vinamilk.com.vn', DATEADD(DAY, -10, SYSUTCDATETIME()), N'10.20.16.45'
),
(
    N'Employees', N'CEO_FAST_TRACK_SALARY_OVERRIDE', CAST(@EmpFactory1Id AS NVARCHAR(50)),
    N'{"BaseSalary":26000000,"Allowance":4500000}',
    N'{"BaseSalary":30000000,"Allowance":5000000,"Reason":"Đặc cách vượt khung của CEO Mai Kiều Liên","OverrideCode":"CEO-OVERRIDE-2025-VNM-001","AppendixNumber":"PLHD-2025-FAST-001"}',
    N'lienmk@vinamilk.com.vn', DATEADD(DAY, -5, SYSUTCDATETIME()), N'10.20.1.1'
),
(
    N'PaySlips', N'APPROVE_PAYROLL', N'2025-01',
    N'{"Status":"PendingApproval"}',
    N'{"Status":"Approved","TotalEmployees":4,"TotalNetSalary":129263357}',
    N'hungnv@vinamilk.com.vn', DATEADD(DAY, -2, SYSUTCDATETIME()), N'10.20.16.45'
);

COMMIT TRANSACTION;
PRINT N'==> Hoàn tất nạp Seed Data thực tế thành công!';
GO

-- ====================================================================================================
-- PHẦN 6: KIỂM TRA TÍNH TOÀN VẸN VÀ THỐNG KÊ DỮ LIỆU SAU KHI KHỞI TẠO
-- ====================================================================================================
PRINT N'';
PRINT N'====================================================================================================';
PRINT N'BÁO CÁO THỐNG KÊ CƠ SỞ DỮ LIỆU VINAMILK ENTERPRISE HRM SAU KHI TRIỂN KHAI:';
PRINT N'====================================================================================================';

SELECT 
    'OrganizationalUnits' AS [Bảng], COUNT(*) AS [Số Bản Ghi] FROM dbo.OrganizationalUnits UNION ALL
SELECT 'Employees', COUNT(*) FROM dbo.Employees UNION ALL
SELECT 'LaborContracts', COUNT(*) FROM dbo.LaborContracts UNION ALL
SELECT 'Shifts', COUNT(*) FROM dbo.Shifts UNION ALL
SELECT 'Attendances', COUNT(*) FROM dbo.Attendances UNION ALL
SELECT 'LeaveRequests', COUNT(*) FROM dbo.LeaveRequests UNION ALL
SELECT 'KpiAssignments', COUNT(*) FROM dbo.KpiAssignments UNION ALL
SELECT 'SalaryProposals', COUNT(*) FROM dbo.SalaryProposals UNION ALL
SELECT 'PaySlips', COUNT(*) FROM dbo.PaySlips UNION ALL
SELECT 'ElectronicContractAppendices', COUNT(*) FROM dbo.ElectronicContractAppendices UNION ALL
SELECT 'JobPostings', COUNT(*) FROM dbo.JobPostings UNION ALL
SELECT 'CandidateApplications', COUNT(*) FROM dbo.CandidateApplications UNION ALL
SELECT 'CompanyAssets', COUNT(*) FROM dbo.CompanyAssets UNION ALL
SELECT 'AuditLogs', COUNT(*) FROM dbo.AuditLogs;

PRINT N'==> Kịch bản T-SQL chạy thành công 100% trên SQL Server Management Studio (SSMS).';
GO
