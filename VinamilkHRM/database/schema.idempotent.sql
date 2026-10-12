IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoles] (
        [Id] int NOT NULL IDENTITY,
        [Description] nvarchar(max) NULL,
        [Name] nvarchar(256) NULL,
        [NormalizedName] nvarchar(256) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoles] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [TableName] nvarchar(100) NOT NULL,
        [Action] nvarchar(50) NOT NULL,
        [EntityId] nvarchar(50) NOT NULL,
        [OldValues] nvarchar(max) NULL,
        [NewValues] nvarchar(max) NULL,
        [PerformedBy] nvarchar(100) NOT NULL,
        [PerformedAt] datetime2 NOT NULL,
        [IpAddress] nvarchar(50) NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [JobPostings] (
        [Id] int NOT NULL IDENTITY,
        [JobCode] nvarchar(50) NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [Slug] nvarchar(250) NOT NULL,
        [UnitType] int NOT NULL,
        [Department] nvarchar(150) NOT NULL,
        [WorkLocation] nvarchar(300) NOT NULL,
        [City] nvarchar(100) NOT NULL,
        [RegionId] int NOT NULL,
        [EmploymentType] nvarchar(50) NOT NULL,
        [ExperienceLevel] nvarchar(100) NOT NULL,
        [EducationRequirement] nvarchar(100) NOT NULL,
        [SalaryMin] decimal(18,2) NULL,
        [SalaryMax] decimal(18,2) NULL,
        [SalaryDisplay] nvarchar(150) NOT NULL,
        [DatePosted] datetime2 NOT NULL,
        [ValidThrough] datetime2 NOT NULL,
        [Description] nvarchar(max) NOT NULL,
        [Requirements] nvarchar(max) NOT NULL,
        [Benefits] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [IsFeatured] bit NOT NULL,
        [ViewCount] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JobPostings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetRoleClaims] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetRoleClaims] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AspNetRoleClaims_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserClaims] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [ClaimType] nvarchar(max) NULL,
        [ClaimValue] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserClaims] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserLogins] (
        [LoginProvider] nvarchar(450) NOT NULL,
        [ProviderKey] nvarchar(450) NOT NULL,
        [ProviderDisplayName] nvarchar(max) NULL,
        [UserId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserLogins] PRIMARY KEY ([LoginProvider], [ProviderKey])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserRoles] (
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        CONSTRAINT [PK_AspNetUserRoles] PRIMARY KEY ([UserId], [RoleId]),
        CONSTRAINT [FK_AspNetUserRoles_AspNetRoles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [AspNetRoles] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUsers] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [MustChangePassword] bit NOT NULL,
        [LastLoginAt] datetimeoffset NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [UserName] nvarchar(256) NULL,
        [NormalizedUserName] nvarchar(256) NULL,
        [Email] nvarchar(256) NULL,
        [NormalizedEmail] nvarchar(256) NULL,
        [EmailConfirmed] bit NOT NULL,
        [PasswordHash] nvarchar(max) NULL,
        [SecurityStamp] nvarchar(max) NULL,
        [ConcurrencyStamp] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(max) NULL,
        [PhoneNumberConfirmed] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [LockoutEnd] datetimeoffset NULL,
        [LockoutEnabled] bit NOT NULL,
        [AccessFailedCount] int NOT NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [AspNetUserTokens] (
        [UserId] int NOT NULL,
        [LoginProvider] nvarchar(450) NOT NULL,
        [Name] nvarchar(450) NOT NULL,
        [Value] nvarchar(max) NULL,
        CONSTRAINT [PK_AspNetUserTokens] PRIMARY KEY ([UserId], [LoginProvider], [Name]),
        CONSTRAINT [FK_AspNetUserTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [RefreshTokens] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [TokenHash] nvarchar(128) NOT NULL,
        [ExpiresAt] datetimeoffset NOT NULL,
        [CreatedAt] datetimeoffset NOT NULL,
        [CreatedByIp] nvarchar(50) NULL,
        [RevokedAt] datetimeoffset NULL,
        [RevokedByIp] nvarchar(50) NULL,
        [ReplacedByTokenHash] nvarchar(128) NULL,
        CONSTRAINT [PK_RefreshTokens] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RefreshTokens_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    DECLARE @historyTableSchema sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [Attendances] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [ShiftId] int NOT NULL,
        [AttendanceDate] datetime2 NOT NULL,
        [CheckInTime] time NULL,
        [CheckOutTime] time NULL,
        [WorkHours] decimal(5,2) NOT NULL,
        [OvertimeHours] decimal(5,2) NOT NULL,
        [Status] int NOT NULL,
        [Note] nvarchar(500) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Attendances] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema + N'].[AttendancesHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [CandidateApplications] (
        [Id] int NOT NULL IDENTITY,
        [JobPostingId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [CitizenId] nvarchar(20) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Gender] int NOT NULL,
        [Address] nvarchar(300) NOT NULL,
        [ResumeFileName] nvarchar(250) NOT NULL,
        [CoverLetter] nvarchar(2000) NOT NULL,
        [PortfolioUrl] nvarchar(300) NOT NULL,
        [Status] int NOT NULL,
        [AppliedAt] datetime2 NOT NULL,
        [InterviewDate] datetime2 NULL,
        [InterviewScore] decimal(5,2) NULL,
        [EvaluationNotes] nvarchar(1000) NULL,
        [HiredEmployeeId] int NULL,
        [HiredDate] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_CandidateApplications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CandidateApplications_JobPostings_JobPostingId] FOREIGN KEY ([JobPostingId]) REFERENCES [JobPostings] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [CompanyAssets] (
        [Id] int NOT NULL IDENTITY,
        [AssetCode] nvarchar(50) NOT NULL,
        [AssetName] nvarchar(200) NOT NULL,
        [Category] nvarchar(100) NOT NULL,
        [SerialNumber] nvarchar(100) NOT NULL,
        [AssignedToEmployeeId] int NULL,
        [AssignedDate] datetime2 NULL,
        [OriginalValue] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_CompanyAssets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [DisciplinaryWarnings] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [WarningDate] datetime2 NOT NULL,
        [ConsecutiveAbsentDays] int NOT NULL,
        [ViolationTitle] nvarchar(250) NOT NULL,
        [Details] nvarchar(1000) NOT NULL,
        [IsResolved] bit NOT NULL,
        [ResolutionNotes] nvarchar(1000) NULL,
        [ResolvedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_DisciplinaryWarnings] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [ElectronicContractAppendices] (
        [Id] int NOT NULL IDENTITY,
        [AppendixNumber] nvarchar(100) NOT NULL,
        [LaborContractId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [SignDate] datetime2 NOT NULL,
        [PreviousSalary] decimal(18,2) NOT NULL,
        [NewSalary] decimal(18,2) NOT NULL,
        [PreviousAllowance] decimal(18,2) NOT NULL,
        [NewAllowance] decimal(18,2) NOT NULL,
        [AdjustmentReason] nvarchar(500) NOT NULL,
        [IsCeoOverride] bit NOT NULL,
        [CeoOverrideCode] nvarchar(100) NULL,
        [DigitalSignatureHash] nvarchar(256) NOT NULL,
        [SignerName] nvarchar(150) NOT NULL,
        [SignerTitle] nvarchar(150) NOT NULL,
        [IsSigned] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ElectronicContractAppendices] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] nvarchar(50) NOT NULL,
        [FirstName] nvarchar(100) NOT NULL,
        [LastName] nvarchar(100) NOT NULL,
        [CitizenId] nvarchar(20) NOT NULL,
        [DateOfBirth] datetime2 NOT NULL,
        [Gender] int NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [HireDate] datetime2 NOT NULL,
        [EmployeeType] int NOT NULL,
        [IsActive] bit NOT NULL,
        [OrganizationalUnitId] int NOT NULL,
        [ManagerId] int NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [Allowance] decimal(18,2) NOT NULL,
        [InsuranceSalary] decimal(18,2) NOT NULL,
        [DependentCount] int NOT NULL,
        [AnnualLeaveBalance] decimal(5,2) NOT NULL,
        [DiligenceScore] decimal(5,2) NOT NULL,
        [ProductionShiftGroup] nvarchar(max) NULL,
        [HazardousAllowanceRate] decimal(18,2) NULL,
        [MachineCertificationCode] nvarchar(max) NULL,
        [SafetyLevel] nvarchar(max) NULL,
        [SpecializationArea] nvarchar(max) NULL,
        [BioSecurityClearanceCode] nvarchar(max) NULL,
        [AnimalCareAllowanceRate] decimal(18,2) NULL,
        [VeterinaryLicenseNumber] nvarchar(max) NULL,
        [JobTitle] nvarchar(max) NULL,
        [DeskLocation] nvarchar(max) NULL,
        [ExtensionNumber] nvarchar(max) NULL,
        [CubicleNumber] nvarchar(max) NULL,
        [CashRegisterId] nvarchar(max) NULL,
        [RetailSalesBonusRate] decimal(18,2) NULL,
        [UniformSize] nvarchar(max) NULL,
        [CustomerServiceRating] decimal(18,2) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Employees_ManagerId] FOREIGN KEY ([ManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    DECLARE @historyTableSchema1 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiAssignments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [EvaluatorId] int NOT NULL,
        [Period] nvarchar(20) NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [Description] nvarchar(1000) NOT NULL,
        [TargetMetric] nvarchar(500) NOT NULL,
        [WeightPercentage] decimal(5,2) NOT NULL,
        [DueDate] datetime2 NOT NULL,
        [BaseScore] decimal(5,2) NOT NULL,
        [SlaBonusPenaltyScore] decimal(5,2) NOT NULL,
        [ScoreAdjustment] decimal(5,2) NOT NULL,
        [SlaCalculationDetail] nvarchar(500) NULL,
        [CompletedDate] datetime2 NULL,
        [ActualResult] nvarchar(2000) NULL,
        [SelfEvaluationScore] decimal(5,2) NULL,
        [FinalScore] decimal(5,2) NULL,
        [Status] int NOT NULL,
        [Feedback] nvarchar(1000) NULL,
        [SubmissionDate] datetime2 NULL,
        [EvaluationDate] datetime2 NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiAssignments_Employees_EvaluatorId] FOREIGN KEY ([EvaluatorId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema1 + N'].[KpiAssignmentsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    DECLARE @historyTableSchema2 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [LaborContracts] (
        [Id] int NOT NULL IDENTITY,
        [ContractNumber] nvarchar(50) NOT NULL,
        [EmployeeId] int NOT NULL,
        [ContractType] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NULL,
        [BasicSalary] decimal(18,2) NOT NULL,
        [InsuranceSalary] decimal(18,2) NOT NULL,
        [WorkingHoursPerWeek] int NOT NULL,
        [Status] int NOT NULL,
        [SignDate] datetime2 NOT NULL,
        [TerminationDate] datetime2 NULL,
        [TerminationReason] nvarchar(500) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [DurationMonths] int NULL,
        [MaximumRenewalsAllowed] int NULL,
        [CurrentRenewalCount] int NULL,
        [SeverancePackageTerms] nvarchar(max) NULL,
        [AnnualTenureBonusRate] decimal(18,2) NULL,
        [ProbationEvaluationScore] decimal(18,2) NULL,
        [MentorEmployeeId] int NULL,
        [ProbationPassed] bit NULL,
        [EvaluationFeedback] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LaborContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaborContracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LaborContracts_Employees_MentorEmployeeId] FOREIGN KEY ([MentorEmployeeId]) REFERENCES [Employees] ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema2 + N'].[LaborContractsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [LeaveRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [LeaveType] int NOT NULL,
        [StartDate] datetime2 NOT NULL,
        [EndDate] datetime2 NOT NULL,
        [TotalDays] decimal(5,2) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [ApprovedById] int NULL,
        [ApprovalNotes] nvarchar(500) NULL,
        [DecisionDate] datetime2 NULL,
        [IsSocialInsuranceCovered] bit NOT NULL,
        [SocialInsuranceBenefitRate] decimal(5,2) NOT NULL,
        [SocialInsuranceFormCode] nvarchar(100) NULL,
        [SocialInsuranceFormContent] nvarchar(4000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LeaveRequests_Employees_ApprovedById] FOREIGN KEY ([ApprovedById]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [OrganizationalUnits] (
        [Id] int NOT NULL IDENTITY,
        [UnitCode] nvarchar(50) NOT NULL,
        [UnitName] nvarchar(200) NOT NULL,
        [UnitType] int NOT NULL,
        [Address] nvarchar(500) NOT NULL,
        [PhoneNumber] nvarchar(50) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [TaxCode] nvarchar(50) NOT NULL,
        [RegionId] int NOT NULL DEFAULT 3,
        [RegionName] nvarchar(100) NOT NULL DEFAULT N'Miền Nam',
        [ParentUnitId] int NULL,
        [Path] hierarchyid NULL,
        [ManagerEmployeeId] int NULL,
        [HerdSize] int NULL,
        [TotalAreaHectares] decimal(18,2) NULL,
        [BarnCount] int NULL,
        [GlobalGapCertificationNumber] nvarchar(max) NULL,
        [FactoryCapacityLitersPerDay] decimal(18,2) NULL,
        [ProductionLineCount] int NULL,
        [IsoStandard] nvarchar(max) NULL,
        [EnvironmentalPermitNumber] nvarchar(max) NULL,
        [FloorCount] int NULL,
        [MeetingRoomCount] int NULL,
        [HasExecutiveSuite] bit NULL,
        [DivisionName] nvarchar(max) NULL,
        [StoreCode] nvarchar(max) NULL,
        [ShelfAreaSquareMeters] decimal(18,2) NULL,
        [ColdStorageCapacityLiters] decimal(18,2) NULL,
        [BusinessHours] nvarchar(max) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_OrganizationalUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationalUnits_Employees_ManagerEmployeeId] FOREIGN KEY ([ManagerEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OrganizationalUnits_OrganizationalUnits_ParentUnitId] FOREIGN KEY ([ParentUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    DECLARE @historyTableSchema3 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [PaySlips] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [PayrollPeriod] nvarchar(20) NOT NULL,
        [StandardWorkDays] decimal(5,2) NOT NULL,
        [ActualWorkDays] decimal(5,2) NOT NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [Allowance] decimal(18,2) NOT NULL,
        [LunchAllowance] decimal(18,2) NOT NULL,
        [OvertimeHours] decimal(5,2) NOT NULL,
        [OvertimePay] decimal(18,2) NOT NULL,
        [NightShiftHours] decimal(5,2) NOT NULL,
        [NightShiftPay] decimal(18,2) NOT NULL,
        [HazardousAllowance] decimal(18,2) NOT NULL,
        [SalesCommission] decimal(18,2) NOT NULL,
        [KpiBonus] decimal(18,2) NOT NULL,
        [OtherBonuses] decimal(18,2) NOT NULL,
        [GrossSalary] decimal(18,2) NOT NULL,
        [SocialInsuranceDeduction] decimal(18,2) NOT NULL,
        [HealthInsuranceDeduction] decimal(18,2) NOT NULL,
        [UnemploymentInsuranceDeduction] decimal(18,2) NOT NULL,
        [DependentCount] int NOT NULL,
        [PersonalDeduction] decimal(18,2) NOT NULL,
        [DependentDeduction] decimal(18,2) NOT NULL,
        [TaxableIncome] decimal(18,2) NOT NULL,
        [PersonalIncomeTax] decimal(18,2) NOT NULL,
        [TotalDeductions] decimal(18,2) NOT NULL,
        [NetSalary] decimal(18,2) NOT NULL,
        [Status] int NOT NULL,
        [PaymentDate] datetime2 NULL,
        [TransactionReference] nvarchar(100) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PaySlips] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PaySlips_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema3 + N'].[PaySlipsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    DECLARE @historyTableSchema4 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [SalaryProposals] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [ProposedById] int NOT NULL,
        [CurrentSalary] decimal(18,2) NOT NULL,
        [ProposedSalary] decimal(18,2) NOT NULL,
        [CurrentAllowance] decimal(18,2) NOT NULL,
        [ProposedAllowance] decimal(18,2) NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [EffectiveDate] datetime2 NOT NULL,
        [Status] int NOT NULL,
        [DepartmentHeadApprovedById] int NULL,
        [DepartmentHeadNotes] nvarchar(500) NULL,
        [HRDirectorApprovedById] int NULL,
        [HRDirectorNotes] nvarchar(500) NULL,
        [FinalDecisionDate] datetime2 NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SalaryProposals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryProposals_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryProposals_Employees_ProposedById] FOREIGN KEY ([ProposedById]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema4 + N'].[SalaryProposalsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE TABLE [Shifts] (
        [Id] int NOT NULL IDENTITY,
        [ShiftCode] nvarchar(50) NOT NULL,
        [ShiftName] nvarchar(100) NOT NULL,
        [ShiftType] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [BreakMinutes] int NOT NULL,
        [MultiplierRate] decimal(5,2) NOT NULL,
        [OrganizationalUnitId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Shifts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Shifts_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetRoleClaims_RoleId] ON [AspNetRoleClaims] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [RoleNameIndex] ON [AspNetRoles] ([NormalizedName]) WHERE [NormalizedName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserClaims_UserId] ON [AspNetUserClaims] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserLogins_UserId] ON [AspNetUserLogins] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUserRoles_RoleId] ON [AspNetUserRoles] ([RoleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [EmailIndex] ON [AspNetUsers] ([NormalizedEmail]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AspNetUsers_EmployeeId] ON [AspNetUsers] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UserNameIndex] ON [AspNetUsers] ([NormalizedUserName]) WHERE [NormalizedUserName] IS NOT NULL');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Attendances_AttendanceDate] ON [Attendances] ([AttendanceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Attendances_EmployeeId_AttendanceDate] ON [Attendances] ([EmployeeId], [AttendanceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Attendances_ShiftId] ON [Attendances] ([ShiftId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_PerformedAt] ON [AuditLogs] ([PerformedAt]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_TableName] ON [AuditLogs] ([TableName]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CandidateApplications_HiredEmployeeId] ON [CandidateApplications] ([HiredEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CandidateApplications_JobPostingId] ON [CandidateApplications] ([JobPostingId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_CompanyAssets_AssetCode] ON [CompanyAssets] ([AssetCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_CompanyAssets_AssignedToEmployeeId] ON [CompanyAssets] ([AssignedToEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DisciplinaryWarnings_EmployeeId_IsResolved] ON [DisciplinaryWarnings] ([EmployeeId], [IsResolved]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DisciplinaryWarnings_WarningDate] ON [DisciplinaryWarnings] ([WarningDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ElectronicContractAppendices_AppendixNumber] ON [ElectronicContractAppendices] ([AppendixNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ElectronicContractAppendices_EmployeeId] ON [ElectronicContractAppendices] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ElectronicContractAppendices_LaborContractId] ON [ElectronicContractAppendices] ([LaborContractId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_CitizenId] ON [Employees] ([CitizenId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeCode] ON [Employees] ([EmployeeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_ManagerId] ON [Employees] ([ManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_OrganizationalUnitId] ON [Employees] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobPostings_JobCode] ON [JobPostings] ([JobCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobPostings_Slug] ON [JobPostings] ([Slug]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_KpiAssignments_EmployeeId_Period] ON [KpiAssignments] ([EmployeeId], [Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_KpiAssignments_EvaluatorId] ON [KpiAssignments] ([EvaluatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LaborContracts_ContractNumber] ON [LaborContracts] ([ContractNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LaborContracts_EmployeeId] ON [LaborContracts] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LaborContracts_MentorEmployeeId] ON [LaborContracts] ([MentorEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_ApprovedById] ON [LeaveRequests] ([ApprovedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_EmployeeId] ON [LeaveRequests] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrganizationalUnits_ManagerEmployeeId] ON [OrganizationalUnits] ([ManagerEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrganizationalUnits_ParentUnitId] ON [OrganizationalUnits] ([ParentUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrganizationalUnits_Path] ON [OrganizationalUnits] ([Path]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_OrganizationalUnits_UnitCode] ON [OrganizationalUnits] ([UnitCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PaySlips_EmployeeId_PayrollPeriod] ON [PaySlips] ([EmployeeId], [PayrollPeriod]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PaySlips_PayrollPeriod] ON [PaySlips] ([PayrollPeriod]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_TokenHash] ON [RefreshTokens] ([TokenHash]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RefreshTokens_UserId] ON [RefreshTokens] ([UserId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SalaryProposals_EmployeeId] ON [SalaryProposals] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SalaryProposals_ProposedById] ON [SalaryProposals] ([ProposedById]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Shifts_OrganizationalUnitId] ON [Shifts] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Shifts_ShiftCode] ON [Shifts] ([ShiftCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserClaims] ADD CONSTRAINT [FK_AspNetUserClaims_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserLogins] ADD CONSTRAINT [FK_AspNetUserLogins_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUserRoles] ADD CONSTRAINT [FK_AspNetUserRoles_AspNetUsers_UserId] FOREIGN KEY ([UserId]) REFERENCES [AspNetUsers] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [AspNetUsers] ADD CONSTRAINT [FK_AspNetUsers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [Attendances] ADD CONSTRAINT [FK_Attendances_Shifts_ShiftId] FOREIGN KEY ([ShiftId]) REFERENCES [Shifts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [CandidateApplications] ADD CONSTRAINT [FK_CandidateApplications_Employees_HiredEmployeeId] FOREIGN KEY ([HiredEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [CompanyAssets] ADD CONSTRAINT [FK_CompanyAssets_Employees_AssignedToEmployeeId] FOREIGN KEY ([AssignedToEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [DisciplinaryWarnings] ADD CONSTRAINT [FK_DisciplinaryWarnings_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [ElectronicContractAppendices] ADD CONSTRAINT [FK_ElectronicContractAppendices_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [ElectronicContractAppendices] ADD CONSTRAINT [FK_ElectronicContractAppendices_LaborContracts_LaborContractId] FOREIGN KEY ([LaborContractId]) REFERENCES [LaborContracts] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012200903_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012200903_InitialCreate', N'9.0.2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DROP INDEX [IX_Employees_CitizenId] ON [Employees];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var5 sysname;
    SELECT @var5 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'UnemploymentInsuranceDeduction');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var5 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [UnemploymentInsuranceDeduction] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var6 sysname;
    SELECT @var6 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'TotalDeductions');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var6 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [TotalDeductions] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var7 sysname;
    SELECT @var7 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'TaxableIncome');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var7 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [TaxableIncome] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var8 sysname;
    SELECT @var8 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'SocialInsuranceDeduction');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var8 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [SocialInsuranceDeduction] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var9 sysname;
    SELECT @var9 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'SalesCommission');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var9 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [SalesCommission] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var10 sysname;
    SELECT @var10 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'PersonalIncomeTax');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var10 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [PersonalIncomeTax] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var11 sysname;
    SELECT @var11 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'PersonalDeduction');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var11 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [PersonalDeduction] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var12 sysname;
    SELECT @var12 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'OvertimePay');
    IF @var12 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var12 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [OvertimePay] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var13 sysname;
    SELECT @var13 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'OtherBonuses');
    IF @var13 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var13 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [OtherBonuses] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var14 sysname;
    SELECT @var14 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'NightShiftPay');
    IF @var14 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var14 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [NightShiftPay] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var15 sysname;
    SELECT @var15 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'NetSalary');
    IF @var15 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var15 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [NetSalary] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var16 sysname;
    SELECT @var16 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'LunchAllowance');
    IF @var16 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var16 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [LunchAllowance] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var17 sysname;
    SELECT @var17 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'KpiBonus');
    IF @var17 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var17 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [KpiBonus] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var18 sysname;
    SELECT @var18 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'HealthInsuranceDeduction');
    IF @var18 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var18 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [HealthInsuranceDeduction] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var19 sysname;
    SELECT @var19 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'HazardousAllowance');
    IF @var19 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var19 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [HazardousAllowance] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var20 sysname;
    SELECT @var20 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'GrossSalary');
    IF @var20 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var20 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [GrossSalary] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var21 sysname;
    SELECT @var21 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'DependentDeduction');
    IF @var21 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var21 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [DependentDeduction] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var22 sysname;
    SELECT @var22 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'BaseSalary');
    IF @var22 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var22 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [BaseSalary] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var23 sysname;
    SELECT @var23 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[PaySlips]') AND [c].[name] = N'Allowance');
    IF @var23 IS NOT NULL EXEC(N'ALTER TABLE [PaySlips] DROP CONSTRAINT [' + @var23 + '];');
    ALTER TABLE [PaySlips] ALTER COLUMN [Allowance] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [PaySlips] ADD [IsImported] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [PaySlips] ADD [LegacyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [BranchTerritory] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [CostCenterCode] nvarchar(50) NOT NULL DEFAULT N'';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [DesignedCapacityPerHour] decimal(18,2) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [FarmSpecialty] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [LegacyId] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [LineNumber] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [MinimumWageRegion] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [PlannedHeadcount] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [WorkPattern] int NOT NULL DEFAULT 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var24 sysname;
    SELECT @var24 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'InsuranceSalary');
    IF @var24 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var24 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [InsuranceSalary] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var25 sysname;
    SELECT @var25 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'CitizenId');
    IF @var25 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var25 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [CitizenId] nvarchar(250) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var26 sysname;
    SELECT @var26 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'BaseSalary');
    IF @var26 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var26 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [BaseSalary] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    DECLARE @var27 sysname;
    SELECT @var27 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'Allowance');
    IF @var27 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var27 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [Allowance] decimal(18,0) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [BankAccountNumber] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [BankBranch] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [BankName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [CitizenIdIssueDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [CitizenIdIssuePlace] nvarchar(250) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [EmergencyContactName] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [EmergencyContactPhone] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [EmergencyContactRelationship] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [JobGradeId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [LegacyId] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [SalaryStep] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [StandardJobTitleId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD [TaxCode] nvarchar(50) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Attendances] ADD [IsImported] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Attendances] ADD [LegacyId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [AllowanceTypes] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsInsurable] bit NOT NULL,
        [IsTaxable] bit NOT NULL,
        [TaxExemptCap] decimal(18,0) NOT NULL DEFAULT 0.0,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AllowanceTypes] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [Dependents] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Relationship] nvarchar(50) NOT NULL,
        [TaxCode] nvarchar(50) NULL,
        [CitizenId] nvarchar(50) NULL,
        [DateOfBirth] date NULL,
        [FromMonth] date NOT NULL,
        [ToMonth] date NULL,
        [IsActive] bit NOT NULL,
        [Note] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Dependents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Dependents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [EmployeeAttachments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [FileName] nvarchar(250) NOT NULL,
        [DocumentType] nvarchar(50) NOT NULL,
        [FilePath] nvarchar(500) NOT NULL,
        [FileSizeBytes] bigint NOT NULL,
        [ChecksumSha256] nvarchar(64) NOT NULL,
        [UploadedAt] datetime2 NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmployeeAttachments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAttachments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [EmployeeCertificates] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [CertificateName] nvarchar(250) NOT NULL,
        [CertificateCode] nvarchar(100) NULL,
        [IssuingAuthority] nvarchar(250) NOT NULL,
        [IssueDate] date NOT NULL,
        [ExpiryDate] date NULL,
        [ScoreOrGrade] nvarchar(50) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmployeeCertificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeCertificates_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [EmployeeEducations] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [DegreeLevel] nvarchar(100) NOT NULL,
        [Major] nvarchar(200) NOT NULL,
        [SchoolName] nvarchar(250) NOT NULL,
        [GraduationYear] int NULL,
        [Classification] nvarchar(50) NULL,
        [EducationType] nvarchar(100) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmployeeEducations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeEducations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [EmployeeWorkHistories] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        [CompanyName] nvarchar(250) NOT NULL,
        [JobTitle] nvarchar(150) NULL,
        [MainResponsibilities] nvarchar(1000) NULL,
        [ReferenceContact] nvarchar(250) NULL,
        [Note] nvarchar(500) NULL,
        [LegacyId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_EmployeeWorkHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeWorkHistories_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [InsuranceRates] (
        [Id] int NOT NULL IDENTITY,
        [Kind] int NOT NULL,
        [EmployeeRate] decimal(5,4) NOT NULL,
        [EmployerRate] decimal(5,4) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [LegalReference] nvarchar(250) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_InsuranceRates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [JobFamilies] (
        [Id] int NOT NULL IDENTITY,
        [FamilyCode] nvarchar(50) NOT NULL,
        [FamilyName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JobFamilies] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [JobGrades] (
        [Id] int NOT NULL IDENTITY,
        [GradeNumber] int NOT NULL,
        [GradeCode] nvarchar(20) NOT NULL,
        [GradeName] nvarchar(200) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [LegacyId] nvarchar(50) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JobGrades] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [LookupCategories] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsSystem] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LookupCategories] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [MinimumWages] (
        [Id] int NOT NULL IDENTITY,
        [Region] int NOT NULL,
        [MonthlyAmount] decimal(18,0) NOT NULL,
        [HourlyAmount] decimal(18,0) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [LegalReference] nvarchar(250) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MinimumWages] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [OvertimeRates] (
        [Id] int NOT NULL IDENTITY,
        [DayKind] int NOT NULL,
        [Night] bit NOT NULL,
        [Multiplier] decimal(4,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_OvertimeRates] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [PublicHolidays] (
        [Id] int NOT NULL IDENTITY,
        [Date] date NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [IsPaid] bit NOT NULL,
        [Year] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PublicHolidays] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [StatutoryParameters] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Value] decimal(18,0) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [LegalReference] nvarchar(250) NULL,
        [Description] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_StatutoryParameters] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [TaxBrackets] (
        [Id] int NOT NULL IDENTITY,
        [ScheduleCode] nvarchar(50) NOT NULL,
        [Order] int NOT NULL,
        [UpperBoundMonthly] decimal(18,0) NULL,
        [Rate] decimal(5,4) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Description] nvarchar(250) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_TaxBrackets] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [UnitManagerHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationalUnitId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        [AppointmentDecisionNumber] nvarchar(100) NULL,
        [Note] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_UnitManagerHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitManagerHistories_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UnitManagerHistories_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [UnitMinimumWageRegionHistories] (
        [Id] int NOT NULL IDENTITY,
        [OrganizationalUnitId] int NOT NULL,
        [MinimumWageRegion] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Reason] nvarchar(500) NULL,
        [LegalReference] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_UnitMinimumWageRegionHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UnitMinimumWageRegionHistories_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [ContractAllowances] (
        [Id] int NOT NULL IDENTITY,
        [LaborContractId] int NOT NULL,
        [AllowanceTypeId] int NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [Note] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ContractAllowances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractAllowances_AllowanceTypes_AllowanceTypeId] FOREIGN KEY ([AllowanceTypeId]) REFERENCES [AllowanceTypes] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ContractAllowances_LaborContracts_LaborContractId] FOREIGN KEY ([LaborContractId]) REFERENCES [LaborContracts] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [JobTitles] (
        [Id] int NOT NULL IDENTITY,
        [JobTitleCode] nvarchar(50) NOT NULL,
        [JobTitleName] nvarchar(200) NOT NULL,
        [JobFamilyId] int NOT NULL,
        [LeadershipLevel] int NOT NULL,
        [StandardJobGradeId] int NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [LegacyId] nvarchar(50) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_JobTitles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobTitles_JobFamilies_JobFamilyId] FOREIGN KEY ([JobFamilyId]) REFERENCES [JobFamilies] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobTitles_JobGrades_StandardJobGradeId] FOREIGN KEY ([StandardJobGradeId]) REFERENCES [JobGrades] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [SalaryScales] (
        [Id] int NOT NULL IDENTITY,
        [JobGradeId] int NOT NULL,
        [Step] int NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [LegalReference] nvarchar(200) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_SalaryScales] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryScales_JobGrades_JobGradeId] FOREIGN KEY ([JobGradeId]) REFERENCES [JobGrades] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE TABLE [LookupItems] (
        [Id] int NOT NULL IDENTITY,
        [CategoryId] int NOT NULL,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(200) NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [Description] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_LookupItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LookupItems_LookupCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [LookupCategories] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_PaySlips_LegacyId] ON [PaySlips] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_OrganizationalUnits_LegacyId] ON [OrganizationalUnits] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Employees_JobGradeId] ON [Employees] ([JobGradeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Employees_LegacyId] ON [Employees] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Employees_StandardJobTitleId] ON [Employees] ([StandardJobTitleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Attendances_LegacyId] ON [Attendances] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_AllowanceTypes_Code] ON [AllowanceTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_ContractAllowances_AllowanceTypeId] ON [ContractAllowances] ([AllowanceTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_ContractAllowances_LaborContractId_AllowanceTypeId] ON [ContractAllowances] ([LaborContractId], [AllowanceTypeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_Dependents_EmployeeId_FromMonth] ON [Dependents] ([EmployeeId], [FromMonth]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_EmployeeAttachments_EmployeeId] ON [EmployeeAttachments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_EmployeeCertificates_EmployeeId_ExpiryDate] ON [EmployeeCertificates] ([EmployeeId], [ExpiryDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_EmployeeEducations_EmployeeId] ON [EmployeeEducations] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_EmployeeWorkHistories_EmployeeId_FromDate] ON [EmployeeWorkHistories] ([EmployeeId], [FromDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_EmployeeWorkHistories_LegacyId] ON [EmployeeWorkHistories] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_InsuranceRates_Kind_EffectiveFrom] ON [InsuranceRates] ([Kind], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobFamilies_FamilyCode] ON [JobFamilies] ([FamilyCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobGrades_GradeCode] ON [JobGrades] ([GradeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobGrades_GradeNumber] ON [JobGrades] ([GradeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_JobGrades_LegacyId] ON [JobGrades] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_JobTitles_JobFamilyId] ON [JobTitles] ([JobFamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_JobTitles_JobTitleCode] ON [JobTitles] ([JobTitleCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_JobTitles_LegacyId] ON [JobTitles] ([LegacyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_JobTitles_StandardJobGradeId] ON [JobTitles] ([StandardJobGradeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LookupCategories_Code] ON [LookupCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_LookupItems_CategoryId_Code] ON [LookupItems] ([CategoryId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_MinimumWages_Region_EffectiveFrom] ON [MinimumWages] ([Region], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_OvertimeRates_DayKind_Night_EffectiveFrom] ON [OvertimeRates] ([DayKind], [Night], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE UNIQUE INDEX [IX_PublicHolidays_Date] ON [PublicHolidays] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_PublicHolidays_Year] ON [PublicHolidays] ([Year]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_SalaryScales_JobGradeId_Step_EffectiveFrom] ON [SalaryScales] ([JobGradeId], [Step], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_StatutoryParameters_Code_EffectiveFrom] ON [StatutoryParameters] ([Code], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_TaxBrackets_ScheduleCode_Order_EffectiveFrom] ON [TaxBrackets] ([ScheduleCode], [Order], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_UnitManagerHistories_EmployeeId] ON [UnitManagerHistories] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_UnitManagerHistories_OrganizationalUnitId_FromDate] ON [UnitManagerHistories] ([OrganizationalUnitId], [FromDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    CREATE INDEX [IX_UnitMinimumWageRegionHistories_OrganizationalUnitId_EffectiveFrom] ON [UnitMinimumWageRegionHistories] ([OrganizationalUnitId], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_JobGrades_JobGradeId] FOREIGN KEY ([JobGradeId]) REFERENCES [JobGrades] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    ALTER TABLE [Employees] ADD CONSTRAINT [FK_Employees_JobTitles_StandardJobTitleId] FOREIGN KEY ([StandardJobTitleId]) REFERENCES [JobTitles] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012205007_AddPhase2Entities'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012205007_AddPhase2Entities', N'9.0.2');
END;

COMMIT;
GO

