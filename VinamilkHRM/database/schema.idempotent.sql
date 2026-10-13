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

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_PublicHolidays_Date] ON [PublicHolidays];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_LookupItems_CategoryId_Code] ON [LookupItems];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_LookupCategories_Code] ON [LookupCategories];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_JobTitles_JobTitleCode] ON [JobTitles];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_JobGrades_GradeCode] ON [JobGrades];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_JobGrades_GradeNumber] ON [JobGrades];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_JobFamilies_FamilyCode] ON [JobFamilies];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DROP INDEX [IX_AllowanceTypes_Code] ON [AllowanceTypes];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    ALTER TABLE [OvertimeRates] ADD [LegalReference] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var28 sysname;
    SELECT @var28 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationalUnits]') AND [c].[name] = N'WorkPattern');
    IF @var28 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationalUnits] DROP CONSTRAINT [' + @var28 + '];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var29 sysname;
    SELECT @var29 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationalUnits]') AND [c].[name] = N'MinimumWageRegion');
    IF @var29 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationalUnits] DROP CONSTRAINT [' + @var29 + '];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var30 sysname;
    SELECT @var30 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationalUnits]') AND [c].[name] = N'LineNumber');
    IF @var30 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationalUnits] DROP CONSTRAINT [' + @var30 + '];');
    ALTER TABLE [OrganizationalUnits] ALTER COLUMN [LineNumber] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var31 sysname;
    SELECT @var31 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[OrganizationalUnits]') AND [c].[name] = N'CostCenterCode');
    IF @var31 IS NOT NULL EXEC(N'ALTER TABLE [OrganizationalUnits] DROP CONSTRAINT [' + @var31 + '];');
    ALTER TABLE [OrganizationalUnits] ADD DEFAULT N'CC_DEFAULT' FOR [CostCenterCode];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [DivisionScope] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    ALTER TABLE [OrganizationalUnits] ADD [FunctionDescription] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var32 sysname;
    SELECT @var32 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'SalaryStep');
    IF @var32 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var32 + '];');
    ALTER TABLE [Employees] ADD DEFAULT 1 FOR [SalaryStep];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    DECLARE @var33 sysname;
    SELECT @var33 = [d].[name]
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'CitizenIdIssuePlace');
    IF @var33 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT [' + @var33 + '];');
    ALTER TABLE [Employees] ALTER COLUMN [CitizenIdIssuePlace] nvarchar(150) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_PublicHolidays_Date] ON [PublicHolidays] ([Date]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_LookupItems_CategoryId_Code] ON [LookupItems] ([CategoryId], [Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_LookupCategories_Code] ON [LookupCategories] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_JobTitles_JobTitleCode] ON [JobTitles] ([JobTitleCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_JobGrades_GradeCode] ON [JobGrades] ([GradeCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_JobGrades_GradeNumber] ON [JobGrades] ([GradeNumber]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_JobFamilies_FamilyCode] ON [JobFamilies] ([FamilyCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    CREATE INDEX [IX_AllowanceTypes_Code] ON [AllowanceTypes] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261012221550_Phase2_OrgHierarchy_Jobs_Statutory_EmployeeProfile', N'9.0.2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    ALTER TABLE [Employees] ADD [ExpectedDueDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    ALTER TABLE [Employees] ADD [YoungestChildBirthDate] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [AttendanceAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [OriginalPeriod] nvarchar(10) NOT NULL,
        [TargetPeriod] nvarchar(10) NOT NULL,
        [EmployeeId] int NOT NULL,
        [AdjustedWorkDays] decimal(18,2) NOT NULL,
        [AdjustedOtHours] decimal(18,2) NOT NULL,
        [AdjustedNightHours] decimal(18,2) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AttendanceAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceAdjustments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [AttendanceExplanations] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [AttendanceDate] date NOT NULL,
        [MissingPunchType] int NOT NULL,
        [ActualCheckIn] time NULL,
        [ActualCheckOut] time NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [ApproverId] int NULL,
        [ApprovedAt] datetimeoffset NULL,
        [RejectionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_AttendanceExplanations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceExplanations_Employees_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AttendanceExplanations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [Crews] (
        [Id] int NOT NULL IDENTITY,
        [CrewCode] nvarchar(50) NOT NULL,
        [CrewName] nvarchar(150) NOT NULL,
        [OrganizationalUnitId] int NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_Crews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Crews_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema34 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiConfigurations] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(100) NOT NULL,
        [Value] decimal(18,4) NOT NULL,
        [StringValue] nvarchar(500) NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Description] nvarchar(500) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiConfigurations] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema34 + N'].[KpiConfigurationsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema35 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(250) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [Group] int NOT NULL,
        [ScoringMethod] int NOT NULL,
        [DataSource] int NOT NULL,
        [Description] nvarchar(1000) NULL,
        [MeasurementGuide] nvarchar(2000) NULL,
        [JobFamilyId] int NULL,
        [ApplicableLeadershipLevel] int NULL,
        [IsActive] bit NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiDefinitions_JobFamilies_JobFamilyId] FOREIGN KEY ([JobFamilyId]) REFERENCES [JobFamilies] ([Id]) ON DELETE SET NULL,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema35 + N'].[KpiDefinitionsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiMetrics] (
        [Id] int NOT NULL IDENTITY,
        [MetricCode] nvarchar(50) NOT NULL,
        [Period] nvarchar(10) NOT NULL,
        [OrganizationalUnitId] int NULL,
        [EmployeeId] int NULL,
        [Value] decimal(18,4) NOT NULL,
        [Unit] nvarchar(50) NOT NULL,
        [Source] int NOT NULL,
        [BatchReference] nvarchar(100) NULL,
        [Notes] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiMetrics] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiMetrics_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_KpiMetrics_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema36 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiPeriods] (
        [Id] int NOT NULL IDENTITY,
        [Period] nvarchar(10) NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [AssignStartDate] date NOT NULL,
        [AssignDeadline] date NOT NULL,
        [MidTermDate] date NOT NULL,
        [SelfReviewDeadline] date NOT NULL,
        [ManagerReviewDeadline] date NOT NULL,
        [CalibrationDeadline] date NOT NULL,
        [Status] int NOT NULL,
        [ClosedAt] datetime2 NULL,
        [ClosedBy] nvarchar(100) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiPeriods] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema36 + N'].[KpiPeriodsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema37 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiResults] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Period] nvarchar(10) NOT NULL,
        [TotalScore] decimal(6,2) NOT NULL,
        [Grade] int NOT NULL,
        [BonusCoefficient] decimal(4,2) NOT NULL,
        [UnitResultScore] decimal(6,2) NOT NULL,
        [IndividualWorkScore] decimal(6,2) NOT NULL,
        [PeopleManagementScore] decimal(6,2) NOT NULL,
        [ComplianceScore] decimal(6,2) NOT NULL,
        [IsUnrated] bit NOT NULL,
        [LockedAt] datetime2 NOT NULL,
        [LockedBy] nvarchar(100) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KpiResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiResults_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema37 + N'].[KpiResultsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema38 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiSchemes] (
        [Id] int NOT NULL IDENTITY,
        [LeadershipLevel] int NOT NULL,
        [UnitResultWeight] decimal(5,2) NOT NULL,
        [IndividualWorkWeight] decimal(5,2) NOT NULL,
        [PeopleManagementWeight] decimal(5,2) NOT NULL,
        [ComplianceWeight] decimal(5,2) NOT NULL,
        [IsComplianceGating] bit NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [Description] nvarchar(500) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiSchemes] PRIMARY KEY ([Id]),
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema38 + N'].[KpiSchemesHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiTemplates] (
        [Id] int NOT NULL IDENTITY,
        [TemplateCode] nvarchar(50) NOT NULL,
        [TemplateName] nvarchar(250) NOT NULL,
        [JobTitleId] int NULL,
        [JobFamilyId] int NULL,
        [LeadershipLevel] int NOT NULL,
        [IsActive] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiTemplates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiTemplates_JobFamilies_JobFamilyId] FOREIGN KEY ([JobFamilyId]) REFERENCES [JobFamilies] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_KpiTemplates_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [JobTitles] ([Id]) ON DELETE SET NULL
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [MonthlyTimesheetPeriods] (
        [Id] int NOT NULL IDENTITY,
        [Period] nvarchar(10) NOT NULL,
        [OrganizationalUnitId] int NOT NULL,
        [Status] int NOT NULL,
        [ConfirmedBy] int NULL,
        [ConfirmedAt] datetimeoffset NULL,
        [ApprovedBy] int NULL,
        [ApprovedAt] datetimeoffset NULL,
        [LockedBy] int NULL,
        [LockedAt] datetimeoffset NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MonthlyTimesheetPeriods] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MonthlyTimesheetPeriods_Employees_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MonthlyTimesheetPeriods_Employees_ConfirmedBy] FOREIGN KEY ([ConfirmedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MonthlyTimesheetPeriods_Employees_LockedBy] FOREIGN KEY ([LockedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_MonthlyTimesheetPeriods_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [MonthlyTimesheetSummaries] (
        [Id] int NOT NULL IDENTITY,
        [Period] nvarchar(10) NOT NULL,
        [EmployeeId] int NOT NULL,
        [OrganizationalUnitId] int NOT NULL,
        [StandardWorkDays] decimal(18,2) NOT NULL,
        [ActualWorkDays] decimal(18,2) NOT NULL,
        [PaidLeaveDays] decimal(18,2) NOT NULL,
        [SickLeaveDays] decimal(18,2) NOT NULL,
        [MaternityLeaveDays] decimal(18,2) NOT NULL,
        [HolidayDays] decimal(18,2) NOT NULL,
        [CompensatoryLeaveDays] decimal(18,2) NOT NULL,
        [BusinessTripDays] decimal(18,2) NOT NULL,
        [TrainingDays] decimal(18,2) NOT NULL,
        [UnpaidLeaveDays] decimal(18,2) NOT NULL,
        [UnexcusedAbsenceDays] decimal(18,2) NOT NULL,
        [ConsecutiveUnexcusedDays] int NOT NULL,
        [TotalWorkHours] decimal(18,2) NOT NULL,
        [NightHours] decimal(18,2) NOT NULL,
        [TotalOtHours] decimal(18,2) NOT NULL,
        [RegularOtHours] decimal(18,2) NOT NULL,
        [RestDayOtHours] decimal(18,2) NOT NULL,
        [HolidayOtHours] decimal(18,2) NOT NULL,
        [NightOtHours] decimal(18,2) NOT NULL,
        [LateTimes] int NOT NULL,
        [EarlyLeaveTimes] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_MonthlyTimesheetSummaries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MonthlyTimesheetSummaries_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_MonthlyTimesheetSummaries_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [OvertimeRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [WorkDate] date NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [PlannedHours] decimal(18,2) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [TeamLeaderId] int NULL,
        [TeamLeaderApprovedAt] datetimeoffset NULL,
        [UnitManagerId] int NULL,
        [UnitManagerApprovedAt] datetimeoffset NULL,
        [RejectionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_OvertimeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OvertimeRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_OvertimeRequests_Employees_TeamLeaderId] FOREIGN KEY ([TeamLeaderId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_OvertimeRequests_Employees_UnitManagerId] FOREIGN KEY ([UnitManagerId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [RotationPatterns] (
        [Id] int NOT NULL IDENTITY,
        [PatternCode] nvarchar(50) NOT NULL,
        [PatternName] nvarchar(150) NOT NULL,
        [CycleLengthDays] int NOT NULL,
        [PatternSequence] nvarchar(500) NOT NULL,
        [Description] nvarchar(max) NULL,
        [OrganizationalUnitId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_RotationPatterns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RotationPatterns_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [ShiftDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [Code] nvarchar(50) NOT NULL,
        [Name] nvarchar(150) NOT NULL,
        [CrossesMidnight] bit NOT NULL,
        [TotalPlannedWorkHours] decimal(18,2) NOT NULL,
        [IsActive] bit NOT NULL,
        [OrganizationalUnitId] int NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ShiftDefinitions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftDefinitions_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [ShiftSwapRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequesterEmployeeId] int NOT NULL,
        [RequesterDate] date NOT NULL,
        [TargetEmployeeId] int NOT NULL,
        [TargetDate] date NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [Status] int NOT NULL,
        [ApproverId] int NULL,
        [ApprovedAt] datetimeoffset NULL,
        [RejectionReason] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_ShiftSwapRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftSwapRequests_Employees_ApproverId] FOREIGN KEY ([ApproverId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftSwapRequests_Employees_RequesterEmployeeId] FOREIGN KEY ([RequesterEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftSwapRequests_Employees_TargetEmployeeId] FOREIGN KEY ([TargetEmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [TimeEvents] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Timestamp] datetimeoffset NOT NULL,
        [DeviceId] nvarchar(100) NOT NULL,
        [Method] int NOT NULL,
        [LocationLat] float NULL,
        [LocationLong] float NULL,
        [IpAddress] nvarchar(50) NULL,
        [IsProcessed] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TimeEvents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TimeEvents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [CrewMembers] (
        [Id] int NOT NULL IDENTITY,
        [CrewId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_CrewMembers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CrewMembers_Crews_CrewId] FOREIGN KEY ([CrewId]) REFERENCES [Crews] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CrewMembers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiThresholds] (
        [Id] int NOT NULL IDENTITY,
        [KpiDefinitionId] int NOT NULL,
        [MinThreshold] decimal(18,4) NOT NULL,
        [MaxThreshold] decimal(18,4) NOT NULL,
        [Score] decimal(6,2) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KpiThresholds] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiThresholds_KpiDefinitions_KpiDefinitionId] FOREIGN KEY ([KpiDefinitionId]) REFERENCES [KpiDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema39 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiSheets] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [KpiPeriodId] int NOT NULL,
        [Period] nvarchar(10) NOT NULL,
        [LeadershipLevel] int NOT NULL,
        [JobFamilyId] int NULL,
        [JobTitleId] int NULL,
        [OrganizationalUnitId] int NOT NULL,
        [EvaluatorId] int NOT NULL,
        [Status] int NOT NULL,
        [StandardWorkDays] decimal(6,2) NOT NULL,
        [ActualWorkDays] decimal(6,2) NOT NULL,
        [IsStandardWorkingDaysMet] bit NOT NULL,
        [SelfReviewSubmittedAt] datetime2 NULL,
        [ManagerReviewSubmittedAt] datetime2 NULL,
        [SelfReviewLatePenalty] decimal(6,2) NOT NULL,
        [ManagerReviewLatePenalty] decimal(6,2) NOT NULL,
        [HasSevereViolation] bit NOT NULL,
        [UnitResultScore] decimal(6,2) NOT NULL,
        [IndividualWorkScore] decimal(6,2) NOT NULL,
        [PeopleManagementScore] decimal(6,2) NOT NULL,
        [ComplianceScore] decimal(6,2) NOT NULL,
        [TotalScore] decimal(6,2) NOT NULL,
        [FinalGrade] int NOT NULL,
        [BonusCoefficient] decimal(4,2) NOT NULL,
        [ManagerFeedback] nvarchar(2000) NULL,
        [CalibrationNotes] nvarchar(1000) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiSheets] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiSheets_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiSheets_Employees_EvaluatorId] FOREIGN KEY ([EvaluatorId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiSheets_JobFamilies_JobFamilyId] FOREIGN KEY ([JobFamilyId]) REFERENCES [JobFamilies] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_KpiSheets_JobTitles_JobTitleId] FOREIGN KEY ([JobTitleId]) REFERENCES [JobTitles] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_KpiSheets_KpiPeriods_KpiPeriodId] FOREIGN KEY ([KpiPeriodId]) REFERENCES [KpiPeriods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiSheets_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema39 + N'].[KpiSheetsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiTemplateItems] (
        [Id] int NOT NULL IDENTITY,
        [KpiTemplateId] int NOT NULL,
        [KpiDefinitionId] int NOT NULL,
        [DefaultTarget] decimal(18,4) NOT NULL,
        [WeightPercentage] decimal(5,2) NOT NULL,
        [DefaultDueDate] date NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_KpiTemplateItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiTemplateItems_KpiDefinitions_KpiDefinitionId] FOREIGN KEY ([KpiDefinitionId]) REFERENCES [KpiDefinitions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiTemplateItems_KpiTemplates_KpiTemplateId] FOREIGN KEY ([KpiTemplateId]) REFERENCES [KpiTemplates] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [CrewRotations] (
        [Id] int NOT NULL IDENTITY,
        [CrewId] int NOT NULL,
        [RotationPatternId] int NOT NULL,
        [AnchorDate] date NOT NULL,
        [StartDayOffset] int NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_CrewRotations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_CrewRotations_Crews_CrewId] FOREIGN KEY ([CrewId]) REFERENCES [Crews] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_CrewRotations_RotationPatterns_RotationPatternId] FOREIGN KEY ([RotationPatternId]) REFERENCES [RotationPatterns] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [RosterEntries] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [WorkDate] date NOT NULL,
        [ShiftDefinitionId] int NULL,
        [IsOff] bit NOT NULL,
        [Source] int NOT NULL,
        [Status] int NOT NULL,
        [Note] nvarchar(250) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_RosterEntries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RosterEntries_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_RosterEntries_ShiftDefinitions_ShiftDefinitionId] FOREIGN KEY ([ShiftDefinitionId]) REFERENCES [ShiftDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [ShiftSegments] (
        [Id] int NOT NULL IDENTITY,
        [ShiftDefinitionId] int NOT NULL,
        [SegmentIndex] int NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [BreakMinutes] int NOT NULL,
        [IsPaidBreak] bit NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ShiftSegments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftSegments_ShiftDefinitions_ShiftDefinitionId] FOREIGN KEY ([ShiftDefinitionId]) REFERENCES [ShiftDefinitions] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiAppeals] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Period] nvarchar(10) NOT NULL,
        [KpiSheetId] int NOT NULL,
        [Reason] nvarchar(2000) NOT NULL,
        [CurrentScore] decimal(6,2) NOT NULL,
        [ProposedScore] decimal(6,2) NOT NULL,
        [Status] int NOT NULL,
        [ReviewerId] int NULL,
        [ReviewerComment] nvarchar(2000) NULL,
        [AdjustedScore] decimal(6,2) NULL,
        [ReviewedAt] datetime2 NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiAppeals] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiAppeals_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_KpiAppeals_KpiSheets_KpiSheetId] FOREIGN KEY ([KpiSheetId]) REFERENCES [KpiSheets] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    DECLARE @historyTableSchema40 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [KpiItems] (
        [Id] int NOT NULL IDENTITY,
        [KpiSheetId] int NOT NULL,
        [KpiDefinitionId] int NULL,
        [Group] int NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [Description] nvarchar(1000) NULL,
        [Unit] nvarchar(50) NOT NULL,
        [ScoringMethod] int NOT NULL,
        [TargetValue] decimal(18,4) NOT NULL,
        [ActualValue] decimal(18,4) NOT NULL,
        [WeightPercentage] decimal(5,2) NOT NULL,
        [DueDate] date NULL,
        [ExtendedDueDate] date NULL,
        [CompletedDate] date NULL,
        [EvidenceUrl] nvarchar(500) NULL,
        [EvidenceDescription] nvarchar(2000) NULL,
        [SelfScore] decimal(6,2) NULL,
        [ManagerScore] decimal(6,2) NULL,
        [Adjustment] decimal(6,2) NOT NULL,
        [AdjustmentReason] nvarchar(1000) NULL,
        [FinalScore] decimal(6,2) NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiItems_KpiDefinitions_KpiDefinitionId] FOREIGN KEY ([KpiDefinitionId]) REFERENCES [KpiDefinitions] ([Id]) ON DELETE SET NULL,
        CONSTRAINT [FK_KpiItems_KpiSheets_KpiSheetId] FOREIGN KEY ([KpiSheetId]) REFERENCES [KpiSheets] ([Id]) ON DELETE CASCADE,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema40 + N'].[KpiItemsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [DailyAttendances] (
        [Id] int NOT NULL IDENTITY,
        [RosterEntryId] int NULL,
        [EmployeeId] int NOT NULL,
        [AttendanceDate] date NOT NULL,
        [ShiftDefinitionId] int NULL,
        [TotalWorkHours] decimal(18,2) NOT NULL,
        [LateMinutes] int NOT NULL,
        [EarlyLeaveMinutes] int NOT NULL,
        [NightHours] decimal(18,2) NOT NULL,
        [RegularOtHours] decimal(18,2) NOT NULL,
        [RestDayOtHours] decimal(18,2) NOT NULL,
        [HolidayOtHours] decimal(18,2) NOT NULL,
        [NightOtHours] decimal(18,2) NOT NULL,
        [UnapprovedOtHours] decimal(18,2) NOT NULL,
        [AttendanceCode] nvarchar(10) NOT NULL,
        [WorkDayCredit] decimal(18,2) NOT NULL,
        [IsComplete] bit NOT NULL,
        [Status] int NOT NULL,
        [HasWarning] bit NOT NULL,
        [WarningReason] nvarchar(500) NULL,
        [Remarks] nvarchar(500) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_DailyAttendances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyAttendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_DailyAttendances_RosterEntries_RosterEntryId] FOREIGN KEY ([RosterEntryId]) REFERENCES [RosterEntries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyAttendances_ShiftDefinitions_ShiftDefinitionId] FOREIGN KEY ([ShiftDefinitionId]) REFERENCES [ShiftDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [KpiExtensionRequests] (
        [Id] int NOT NULL IDENTITY,
        [KpiItemId] int NOT NULL,
        [OriginalDueDate] date NOT NULL,
        [RequestedDueDate] date NOT NULL,
        [Reason] nvarchar(1000) NOT NULL,
        [ObjectiveReason] bit NOT NULL,
        [Status] int NOT NULL,
        [ApprovedBy] int NULL,
        [ApprovedAt] datetime2 NULL,
        [Comment] nvarchar(1000) NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(max) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(max) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_KpiExtensionRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_KpiExtensionRequests_KpiItems_KpiItemId] FOREIGN KEY ([KpiItemId]) REFERENCES [KpiItems] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE TABLE [AttendanceSegments] (
        [Id] int NOT NULL IDENTITY,
        [DailyAttendanceId] int NOT NULL,
        [SegmentIndex] int NOT NULL,
        [CheckInTime] datetimeoffset NULL,
        [CheckOutTime] datetimeoffset NULL,
        [LateMinutes] int NOT NULL,
        [EarlyLeaveMinutes] int NOT NULL,
        [WorkHours] decimal(18,2) NOT NULL,
        [NightHours] decimal(18,2) NOT NULL,
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AttendanceSegments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceSegments_DailyAttendances_DailyAttendanceId] FOREIGN KEY ([DailyAttendanceId]) REFERENCES [DailyAttendances] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_AttendanceAdjustments_EmployeeId] ON [AttendanceAdjustments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_AttendanceExplanations_ApproverId] ON [AttendanceExplanations] ([ApproverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_AttendanceExplanations_EmployeeId_AttendanceDate] ON [AttendanceExplanations] ([EmployeeId], [AttendanceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_AttendanceSegments_DailyAttendanceId_SegmentIndex] ON [AttendanceSegments] ([DailyAttendanceId], [SegmentIndex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_CrewMembers_CrewId_EmployeeId] ON [CrewMembers] ([CrewId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_CrewMembers_EmployeeId] ON [CrewMembers] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_CrewRotations_CrewId_RotationPatternId] ON [CrewRotations] ([CrewId], [RotationPatternId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_CrewRotations_RotationPatternId] ON [CrewRotations] ([RotationPatternId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_Crews_CrewCode] ON [Crews] ([CrewCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_Crews_OrganizationalUnitId] ON [Crews] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_DailyAttendances_EmployeeId_AttendanceDate] ON [DailyAttendances] ([EmployeeId], [AttendanceDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_DailyAttendances_RosterEntryId] ON [DailyAttendances] ([RosterEntryId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_DailyAttendances_ShiftDefinitionId] ON [DailyAttendances] ([ShiftDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiAppeals_EmployeeId] ON [KpiAppeals] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiAppeals_KpiSheetId] ON [KpiAppeals] ([KpiSheetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiConfigurations_Code_EffectiveFrom] ON [KpiConfigurations] ([Code], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KpiDefinitions_Code] ON [KpiDefinitions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiDefinitions_JobFamilyId] ON [KpiDefinitions] ([JobFamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiExtensionRequests_KpiItemId] ON [KpiExtensionRequests] ([KpiItemId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiItems_KpiDefinitionId] ON [KpiItems] ([KpiDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiItems_KpiSheetId] ON [KpiItems] ([KpiSheetId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiMetrics_EmployeeId] ON [KpiMetrics] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiMetrics_MetricCode_Period_OrganizationalUnitId_EmployeeId] ON [KpiMetrics] ([MetricCode], [Period], [OrganizationalUnitId], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiMetrics_OrganizationalUnitId] ON [KpiMetrics] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KpiPeriods_Period] ON [KpiPeriods] ([Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KpiResults_EmployeeId_Period] ON [KpiResults] ([EmployeeId], [Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSchemes_LeadershipLevel_EffectiveFrom] ON [KpiSchemes] ([LeadershipLevel], [EffectiveFrom]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KpiSheets_EmployeeId_Period] ON [KpiSheets] ([EmployeeId], [Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSheets_EvaluatorId] ON [KpiSheets] ([EvaluatorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSheets_JobFamilyId] ON [KpiSheets] ([JobFamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSheets_JobTitleId] ON [KpiSheets] ([JobTitleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSheets_KpiPeriodId] ON [KpiSheets] ([KpiPeriodId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiSheets_OrganizationalUnitId] ON [KpiSheets] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiTemplateItems_KpiDefinitionId] ON [KpiTemplateItems] ([KpiDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiTemplateItems_KpiTemplateId] ON [KpiTemplateItems] ([KpiTemplateId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiTemplates_JobFamilyId] ON [KpiTemplates] ([JobFamilyId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiTemplates_JobTitleId] ON [KpiTemplates] ([JobTitleId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_KpiTemplates_TemplateCode] ON [KpiTemplates] ([TemplateCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_KpiThresholds_KpiDefinitionId] ON [KpiThresholds] ([KpiDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetPeriods_ApprovedBy] ON [MonthlyTimesheetPeriods] ([ApprovedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetPeriods_ConfirmedBy] ON [MonthlyTimesheetPeriods] ([ConfirmedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetPeriods_LockedBy] ON [MonthlyTimesheetPeriods] ([LockedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetPeriods_OrganizationalUnitId] ON [MonthlyTimesheetPeriods] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MonthlyTimesheetPeriods_Period_OrganizationalUnitId] ON [MonthlyTimesheetPeriods] ([Period], [OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetSummaries_EmployeeId] ON [MonthlyTimesheetSummaries] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_MonthlyTimesheetSummaries_OrganizationalUnitId] ON [MonthlyTimesheetSummaries] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_MonthlyTimesheetSummaries_Period_EmployeeId] ON [MonthlyTimesheetSummaries] ([Period], [EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_OvertimeRequests_EmployeeId_WorkDate] ON [OvertimeRequests] ([EmployeeId], [WorkDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_OvertimeRequests_TeamLeaderId] ON [OvertimeRequests] ([TeamLeaderId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_OvertimeRequests_UnitManagerId] ON [OvertimeRequests] ([UnitManagerId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RosterEntries_EmployeeId_WorkDate] ON [RosterEntries] ([EmployeeId], [WorkDate]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_RosterEntries_ShiftDefinitionId] ON [RosterEntries] ([ShiftDefinitionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_RotationPatterns_OrganizationalUnitId] ON [RotationPatterns] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_RotationPatterns_PatternCode] ON [RotationPatterns] ([PatternCode]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_ShiftDefinitions_Code] ON [ShiftDefinitions] ([Code]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_ShiftDefinitions_OrganizationalUnitId] ON [ShiftDefinitions] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_ShiftSegments_ShiftDefinitionId_SegmentIndex] ON [ShiftSegments] ([ShiftDefinitionId], [SegmentIndex]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_ShiftSwapRequests_ApproverId] ON [ShiftSwapRequests] ([ApproverId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_ShiftSwapRequests_RequesterEmployeeId] ON [ShiftSwapRequests] ([RequesterEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_ShiftSwapRequests_TargetEmployeeId] ON [ShiftSwapRequests] ([TargetEmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TimeEvents_DeviceId_EmployeeId_Timestamp] ON [TimeEvents] ([DeviceId], [EmployeeId], [Timestamp]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    CREATE INDEX [IX_TimeEvents_EmployeeId] ON [TimeEvents] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013000308_Phase4_PerformanceManagement_KpiEngine'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013000308_Phase4_PerformanceManagement_KpiEngine', N'9.0.2');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    ALTER TABLE [PaySlips] ADD [EmployerCost] decimal(18,0) NOT NULL DEFAULT 0.0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    ALTER TABLE [PaySlips] ADD [FormulaTraceJson] nvarchar(4000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    ALTER TABLE [PaySlips] ADD [PayrollRunId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    DECLARE @historyTableSchema41 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [PayrollAdjustments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [SourcePeriod] nvarchar(20) NOT NULL,
        [TargetPeriod] nvarchar(20) NOT NULL,
        [AdjustmentType] nvarchar(50) NOT NULL,
        [Amount] decimal(18,0) NOT NULL,
        [Reason] nvarchar(500) NOT NULL,
        [ApprovedBy] int NULL,
        [ApprovedAt] datetimeoffset NULL,
        [IsApplied] bit NOT NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PayrollAdjustments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollAdjustments_Employees_ApprovedBy] FOREIGN KEY ([ApprovedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollAdjustments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema41 + N'].[PayrollAdjustmentsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    DECLARE @historyTableSchema42 sysname = SCHEMA_NAME()
    EXEC(N'CREATE TABLE [PayrollRuns] (
        [Id] int NOT NULL IDENTITY,
        [Period] nvarchar(20) NOT NULL,
        [OrganizationalUnitId] int NULL,
        [Status] int NOT NULL,
        [TotalGross] decimal(18,0) NOT NULL,
        [TotalNet] decimal(18,0) NOT NULL,
        [TotalEmployerCost] decimal(18,0) NOT NULL,
        [TotalPersonalIncomeTax] decimal(18,0) NOT NULL,
        [TotalEmployeeInsurance] decimal(18,0) NOT NULL,
        [TotalEmployerInsurance] decimal(18,0) NOT NULL,
        [EmployeeCount] int NOT NULL,
        [SubmittedBy] int NULL,
        [SubmittedAt] datetimeoffset NULL,
        [ApprovedByChiefAccountant] int NULL,
        [ApprovedAccountantAt] datetimeoffset NULL,
        [ApprovedByHrDirector] int NULL,
        [ApprovedDirectorAt] datetimeoffset NULL,
        [LockedBy] int NULL,
        [LockedAt] datetimeoffset NULL,
        [PaidBy] int NULL,
        [PaidAt] datetimeoffset NULL,
        [PaymentReference] nvarchar(100) NULL,
        [PeriodEnd] datetime2 GENERATED ALWAYS AS ROW END HIDDEN NOT NULL,
        [PeriodStart] datetime2 GENERATED ALWAYS AS ROW START HIDDEN NOT NULL,
        [RowVersion] rowversion NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [CreatedBy] nvarchar(100) NOT NULL,
        [UpdatedAt] datetime2 NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL,
        CONSTRAINT [PK_PayrollRuns] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollRuns_Employees_ApprovedByChiefAccountant] FOREIGN KEY ([ApprovedByChiefAccountant]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRuns_Employees_ApprovedByHrDirector] FOREIGN KEY ([ApprovedByHrDirector]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRuns_Employees_LockedBy] FOREIGN KEY ([LockedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRuns_Employees_PaidBy] FOREIGN KEY ([PaidBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRuns_Employees_SubmittedBy] FOREIGN KEY ([SubmittedBy]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRuns_OrganizationalUnits_OrganizationalUnitId] FOREIGN KEY ([OrganizationalUnitId]) REFERENCES [OrganizationalUnits] ([Id]) ON DELETE SET NULL,
        PERIOD FOR SYSTEM_TIME([PeriodStart], [PeriodEnd])
    ) WITH (SYSTEM_VERSIONING = ON (HISTORY_TABLE = [' + @historyTableSchema42 + N'].[PayrollRunsHistory]))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PaySlips_PayrollRunId] ON [PaySlips] ([PayrollRunId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollAdjustments_ApprovedBy] ON [PayrollAdjustments] ([ApprovedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollAdjustments_EmployeeId] ON [PayrollAdjustments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollAdjustments_TargetPeriod] ON [PayrollAdjustments] ([TargetPeriod]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_ApprovedByChiefAccountant] ON [PayrollRuns] ([ApprovedByChiefAccountant]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_ApprovedByHrDirector] ON [PayrollRuns] ([ApprovedByHrDirector]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_LockedBy] ON [PayrollRuns] ([LockedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_OrganizationalUnitId] ON [PayrollRuns] ([OrganizationalUnitId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_PaidBy] ON [PayrollRuns] ([PaidBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_Period] ON [PayrollRuns] ([Period]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    CREATE INDEX [IX_PayrollRuns_SubmittedBy] ON [PayrollRuns] ([SubmittedBy]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    ALTER TABLE [PaySlips] ADD CONSTRAINT [FK_PaySlips_PayrollRuns_PayrollRunId] FOREIGN KEY ([PayrollRunId]) REFERENCES [PayrollRuns] ([Id]) ON DELETE SET NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013024503_Phase6_PayrollEngine_3P_Statutory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013024503_Phase6_PayrollEngine_3P_Statutory', N'9.0.2');
END;

COMMIT;
GO

