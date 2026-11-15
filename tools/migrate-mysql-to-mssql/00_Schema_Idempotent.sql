IF OBJECT_ID(N'[dbo].[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [dbo].[__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'system') IS NULL EXEC(N'CREATE SCHEMA [system];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'talent') IS NULL EXEC(N'CREATE SCHEMA [talent];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'time') IS NULL EXEC(N'CREATE SCHEMA [time];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'core') IS NULL EXEC(N'CREATE SCHEMA [core];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'identity') IS NULL EXEC(N'CREATE SCHEMA [identity];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    IF SCHEMA_ID(N'payroll') IS NULL EXEC(N'CREATE SCHEMA [payroll];');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[Assets] (
        [Id] int NOT NULL IDENTITY,
        [AssetCode] varchar(50) NOT NULL,
        [AssetName] nvarchar(200) NOT NULL,
        [AssetCategory] varchar(50) NOT NULL,
        [SerialNumber] varchar(100) NULL,
        [PurchaseValue] decimal(18,2) NOT NULL,
        [PurchaseDate] date NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Available',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Assets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[AuditLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] int NULL,
        [Username] nvarchar(100) NOT NULL,
        [ModuleName] varchar(50) NOT NULL,
        [ActionType] varchar(30) NOT NULL,
        [EntityName] varchar(100) NOT NULL,
        [EntityId] varchar(50) NOT NULL,
        [OldValuesJson] nvarchar(max) NULL,
        [NewValuesJson] nvarchar(max) NULL,
        [AffectedColumns] nvarchar(500) NULL,
        [IpAddress] varchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[EducationLevels] (
        [Id] int NOT NULL IDENTITY,
        [LevelCode] varchar(30) NOT NULL,
        [LevelName] nvarchar(100) NOT NULL,
        [RankOrder] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EducationLevels] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[EvaluationCycles] (
        [Id] int NOT NULL IDENTITY,
        [CycleCode] varchar(30) NOT NULL,
        [CycleName] nvarchar(200) NOT NULL,
        [CycleType] varchar(20) NOT NULL,
        [Year] int NOT NULL,
        [PeriodNumber] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [SelfEvalDeadline] date NOT NULL,
        [ManagerEvalDeadline] date NOT NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Closed',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EvaluationCycles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[Holidays] (
        [Id] int NOT NULL IDENTITY,
        [HolidayDate] date NOT NULL,
        [HolidayName] nvarchar(150) NOT NULL,
        [Year] int NOT NULL,
        [IsPaidHoliday] bit NOT NULL,
        [OvertimeMultiplier] decimal(5,2) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Holidays] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[LeaveTypes] (
        [Id] int NOT NULL IDENTITY,
        [LeaveTypeCode] varchar(30) NOT NULL,
        [LeaveTypeName] nvarchar(150) NOT NULL,
        [IsPaidByCompany] bit NOT NULL,
        [IsPaidBySocialInsurance] bit NOT NULL,
        [MaxDaysPerYear] decimal(5,2) NOT NULL,
        [AllowCarryForward] bit NOT NULL,
        [AttendanceSymbol] varchar(10) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LeaveTypes] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[MenuItems] (
        [Id] int NOT NULL IDENTITY,
        [MenuCode] varchar(50) NOT NULL,
        [Title] nvarchar(150) NOT NULL,
        [Icon] varchar(50) NULL,
        [RouteUrl] nvarchar(200) NULL,
        [RequiredPermissionCode] varchar(100) NULL,
        [ParentId] int NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MenuItems] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[OnboardingTemplates] (
        [Id] int NOT NULL IDENTITY,
        [TemplateCode] varchar(30) NOT NULL,
        [TaskName] nvarchar(200) NOT NULL,
        [ProcessType] varchar(20) NOT NULL DEFAULT 'Onboarding',
        [AssignedDepartmentRole] varchar(30) NOT NULL DEFAULT 'HR',
        [DueDaysOffset] int NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OnboardingTemplates] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[OrganizationUnits] (
        [Id] int NOT NULL IDENTITY,
        [UnitCode] varchar(30) NOT NULL,
        [UnitName] nvarchar(200) NOT NULL,
        [UnitType] varchar(30) NOT NULL DEFAULT 'Department',
        [UnitLevel] int NOT NULL,
        [ParentUnitId] int NULL,
        [TreePath] varchar(250) NULL,
        [ManagerEmployeeId] int NULL,
        [Address] nvarchar(300) NULL,
        [Phone] varchar(30) NULL,
        [CostCenterCode] varchar(20) NULL,
        [PlannedHeadcount] int NOT NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OrganizationUnits] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OrganizationUnits_OrganizationUnits_ParentUnitId] FOREIGN KEY ([ParentUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[PayrollParameters] (
        [Id] int NOT NULL IDENTITY,
        [ParamCode] varchar(50) NOT NULL,
        [ParamName] nvarchar(200) NOT NULL,
        [ParamValue] decimal(18,4) NOT NULL,
        [ValueUnit] varchar(30) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [EffectiveTo] date NULL,
        [LegalReference] nvarchar(300) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PayrollParameters] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[PayrollPeriods] (
        [Id] int NOT NULL IDENTITY,
        [PeriodCode] varchar(30) NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [PeriodTitle] nvarchar(200) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [StandardWorkdays] decimal(5,2) NOT NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Draft',
        [CalculatedByUserId] int NULL,
        [CalculatedAt] datetime2 NULL,
        [ApprovedByUserId] int NULL,
        [ApprovedAt] datetime2 NULL,
        [IsLocked] bit NOT NULL,
        [PayslipSentAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PayrollPeriods] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[Permissions] (
        [Id] int NOT NULL IDENTITY,
        [PermissionCode] varchar(100) NOT NULL,
        [ModuleName] varchar(50) NOT NULL,
        [ActionName] varchar(30) NOT NULL,
        [DisplayName] nvarchar(200) NOT NULL,
        [SortOrder] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Permissions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[Positions] (
        [Id] int NOT NULL IDENTITY,
        [PositionCode] varchar(30) NOT NULL,
        [PositionName] nvarchar(150) NOT NULL,
        [PositionGroup] varchar(30) NOT NULL DEFAULT 'Staff',
        [PositionLevel] int NOT NULL,
        [DefaultPositionAllowance] decimal(18,2) NOT NULL,
        [JobDescription] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Positions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[RewardDisciplineCategories] (
        [Id] int NOT NULL IDENTITY,
        [CategoryCode] varchar(30) NOT NULL,
        [CategoryName] nvarchar(150) NOT NULL,
        [CategoryType] varchar(20) NOT NULL,
        [DefaultAmount] decimal(18,2) NOT NULL,
        [Description] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RewardDisciplineCategories] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[Roles] (
        [Id] int NOT NULL IDENTITY,
        [RoleCode] varchar(50) NOT NULL,
        [RoleName] nvarchar(100) NOT NULL,
        [Description] nvarchar(500) NULL,
        [DefaultDataScope] varchar(30) NOT NULL DEFAULT 'SelfOnly',
        [IsSystemRole] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Roles] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[SalaryComponents] (
        [Id] int NOT NULL IDENTITY,
        [ComponentCode] varchar(50) NOT NULL,
        [ComponentName] nvarchar(150) NOT NULL,
        [ComponentType] varchar(30) NOT NULL,
        [IsTaxable] bit NOT NULL,
        [TaxExemptLimit] decimal(18,2) NOT NULL,
        [IsSocialInsuranceBase] bit NOT NULL,
        [FormulaExpression] nvarchar(500) NULL,
        [SortOrder] int NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SalaryComponents] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[SalaryScales] (
        [Id] int NOT NULL IDENTITY,
        [ScaleCode] varchar(30) NOT NULL,
        [ScaleName] nvarchar(150) NOT NULL,
        [ReviewCycleMonths] int NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SalaryScales] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[TaxBrackets] (
        [Id] int NOT NULL IDENTITY,
        [BracketLevel] int NOT NULL,
        [FromIncome] decimal(18,2) NOT NULL,
        [ToIncome] decimal(18,2) NULL,
        [TaxRatePercent] decimal(5,2) NOT NULL,
        [QuickSubtractionAmount] decimal(18,2) NOT NULL,
        [EffectiveFrom] date NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TaxBrackets] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[TrainingCourses] (
        [Id] int NOT NULL IDENTITY,
        [CourseCode] varchar(30) NOT NULL,
        [CourseName] nvarchar(200) NOT NULL,
        [CourseCategory] varchar(50) NOT NULL,
        [DurationHours] int NOT NULL,
        [StandardCostPerPerson] decimal(18,2) NOT NULL,
        [CommitmentMonths] int NOT NULL,
        [Description] nvarchar(1000) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TrainingCourses] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[WorkflowDefinitions] (
        [Id] int NOT NULL IDENTITY,
        [WorkflowCode] varchar(50) NOT NULL,
        [WorkflowName] nvarchar(150) NOT NULL,
        [EntityType] varchar(50) NOT NULL,
        [Description] nvarchar(500) NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowDefinitions] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[WorkShifts] (
        [Id] int NOT NULL IDENTITY,
        [ShiftCode] varchar(30) NOT NULL,
        [ShiftName] nvarchar(100) NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [BreakStartTime] time NULL,
        [BreakEndTime] time NULL,
        [IsOvernight] bit NOT NULL,
        [StandardHours] decimal(5,2) NOT NULL,
        [WorkdayValue] decimal(5,2) NOT NULL,
        [NightShiftAllowanceRate] decimal(5,2) NOT NULL,
        [GraceLateMinutes] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkShifts] PRIMARY KEY ([Id])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[RecruitmentRequests] (
        [Id] int NOT NULL IDENTITY,
        [RequestCode] varchar(30) NOT NULL,
        [OrganizationUnitId] int NOT NULL,
        [PositionId] int NOT NULL,
        [RequestedByEmployeeId] int NULL,
        [Quantity] int NOT NULL,
        [HiredCount] int NOT NULL,
        [EmploymentType] varchar(30) NOT NULL DEFAULT 'FullTime',
        [ReasonType] varchar(30) NOT NULL DEFAULT 'NewHeadcount',
        [BudgetMinSalary] decimal(18,2) NOT NULL,
        [BudgetMaxSalary] decimal(18,2) NOT NULL,
        [ExpectedOnboardDate] date NULL,
        [JobRequirements] nvarchar(max) NULL,
        [WorkflowInstanceId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Approved',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RecruitmentRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RecruitmentRequests_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RecruitmentRequests_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[RolePermissions] (
        [Id] int NOT NULL IDENTITY,
        [RoleId] int NOT NULL,
        [PermissionId] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RolePermissions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RolePermissions_Permissions_PermissionId] FOREIGN KEY ([PermissionId]) REFERENCES [identity].[Permissions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RolePermissions_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[SalarySteps] (
        [Id] int NOT NULL IDENTITY,
        [SalaryScaleId] int NOT NULL,
        [StepOrder] int NOT NULL,
        [StepName] nvarchar(50) NOT NULL,
        [StandardSalary] decimal(18,2) NOT NULL,
        [SalaryCoefficient] decimal(8,4) NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SalarySteps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalarySteps_SalaryScales_SalaryScaleId] FOREIGN KEY ([SalaryScaleId]) REFERENCES [core].[SalaryScales] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[TrainingClasses] (
        [Id] int NOT NULL IDENTITY,
        [ClassCode] varchar(30) NOT NULL,
        [TrainingCourseId] int NOT NULL,
        [ClassName] nvarchar(200) NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [InstructorName] nvarchar(150) NULL,
        [Location] nvarchar(200) NULL,
        [MaxCapacity] int NOT NULL,
        [TotalActualCost] decimal(18,2) NOT NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Completed',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TrainingClasses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingClasses_TrainingCourses_TrainingCourseId] FOREIGN KEY ([TrainingCourseId]) REFERENCES [talent].[TrainingCourses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[WorkflowSteps] (
        [Id] int NOT NULL IDENTITY,
        [WorkflowDefinitionId] int NOT NULL,
        [StepOrder] int NOT NULL,
        [StepName] nvarchar(150) NOT NULL,
        [ApproverType] varchar(30) NOT NULL,
        [ApproverRoleId] int NULL,
        [ApproverUserId] int NULL,
        [ConditionExpression] nvarchar(300) NULL,
        [SlaHours] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowSteps] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowSteps_WorkflowDefinitions_WorkflowDefinitionId] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [system].[WorkflowDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[JobPostings] (
        [Id] int NOT NULL IDENTITY,
        [PostingCode] varchar(30) NOT NULL,
        [RecruitmentRequestId] int NULL,
        [Title] nvarchar(250) NOT NULL,
        [OrganizationUnitId] int NOT NULL,
        [PositionId] int NOT NULL,
        [Vacancies] int NOT NULL,
        [SalaryRangeText] nvarchar(100) NULL,
        [WorkLocation] nvarchar(200) NULL,
        [PublishDate] date NOT NULL,
        [ClosingDate] date NOT NULL,
        [JobDescription] nvarchar(max) NULL,
        [JobRequirements] nvarchar(max) NULL,
        [Benefits] nvarchar(max) NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Open',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_JobPostings] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobPostings_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobPostings_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobPostings_RecruitmentRequests_RecruitmentRequestId] FOREIGN KEY ([RecruitmentRequestId]) REFERENCES [talent].[RecruitmentRequests] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeCode] varchar(30) NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Gender] nvarchar(10) NOT NULL,
        [DateOfBirth] date NOT NULL,
        [CitizenIdEncrypted] nvarchar(512) NOT NULL,
        [CitizenIdHash] char(64) NOT NULL,
        [CitizenIdIssueDate] date NULL,
        [CitizenIdIssuePlace] nvarchar(200) NULL,
        [Ethnicity] nvarchar(50) NOT NULL,
        [Religion] nvarchar(50) NOT NULL,
        [MaritalStatus] varchar(20) NOT NULL,
        [PlaceOfOrigin] nvarchar(200) NULL,
        [PermanentAddress] nvarchar(300) NULL,
        [CurrentAddress] nvarchar(300) NULL,
        [PhoneNumber] varchar(20) NULL,
        [WorkEmail] varchar(150) NULL,
        [PersonalEmail] varchar(150) NULL,
        [BankAccountEncrypted] nvarchar(512) NULL,
        [BankName] nvarchar(150) NULL,
        [BankBranch] nvarchar(150) NULL,
        [TaxCodeEncrypted] nvarchar(512) NULL,
        [SocialInsuranceNumber] varchar(30) NULL,
        [HealthInsuranceNumber] varchar(30) NULL,
        [HealthInsurancePlace] nvarchar(200) NULL,
        [OrganizationUnitId] int NOT NULL,
        [PositionId] int NOT NULL,
        [DirectManagerId] int NULL,
        [EducationLevelId] int NULL,
        [Major] nvarchar(150) NULL,
        [SalaryStepId] int NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [PositionAllowance] decimal(18,2) NOT NULL,
        [OtherAllowance] decimal(18,2) NOT NULL,
        [HireDate] date NOT NULL,
        [OfficialDate] date NULL,
        [TerminationDate] date NULL,
        [WorkStatus] varchar(30) NOT NULL DEFAULT 'Official',
        [CurrentContractType] varchar(30) NOT NULL DEFAULT 'Indefinite',
        [AvatarUrl] nvarchar(300) NULL,
        [IsUnionMember] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_EducationLevels_EducationLevelId] FOREIGN KEY ([EducationLevelId]) REFERENCES [core].[EducationLevels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Employees_DirectManagerId] FOREIGN KEY ([DirectManagerId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Employees_SalarySteps_SalaryStepId] FOREIGN KEY ([SalaryStepId]) REFERENCES [core].[SalarySteps] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[AiRecommendations] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NULL,
        [OrganizationUnitId] int NULL,
        [RecommendationType] varchar(50) NOT NULL,
        [PriorityLevel] varchar(20) NOT NULL,
        [ConfidenceScore] decimal(5,2) NOT NULL,
        [Title] nvarchar(250) NOT NULL,
        [AnalysisContent] nvarchar(max) NOT NULL,
        [SuggestedAction] nvarchar(1000) NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'New',
        [ReviewedByUserId] int NULL,
        [ReviewedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AiRecommendations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AiRecommendations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_AiRecommendations_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[AttendanceLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [PunchTime] datetime2 NOT NULL,
        [PunchType] varchar(20) NOT NULL,
        [SourceType] varchar(30) NOT NULL,
        [DeviceId] varchar(50) NULL,
        [Latitude] decimal(10,7) NULL,
        [Longitude] decimal(10,7) NULL,
        [LocationAddress] nvarchar(200) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_AttendanceLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_AttendanceLogs_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[Candidates] (
        [Id] int NOT NULL IDENTITY,
        [CandidateCode] varchar(30) NOT NULL,
        [JobPostingId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [DateOfBirth] date NULL,
        [Gender] nvarchar(10) NOT NULL,
        [PhoneNumber] varchar(20) NOT NULL,
        [Email] varchar(150) NOT NULL,
        [EducationLevelId] int NULL,
        [Major] nvarchar(150) NULL,
        [YearsOfExperience] decimal(4,1) NOT NULL,
        [ExpectedSalary] decimal(18,2) NOT NULL,
        [SourceChannel] varchar(50) NOT NULL,
        [ResumeUrl] nvarchar(300) NULL,
        [Stage] varchar(30) NOT NULL DEFAULT 'Applied',
        [OverallScore] decimal(5,2) NOT NULL,
        [ConvertedEmployeeId] int NULL,
        [AppliedAt] datetime2 NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Candidates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Candidates_EducationLevels_EducationLevelId] FOREIGN KEY ([EducationLevelId]) REFERENCES [core].[EducationLevels] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Candidates_Employees_ConvertedEmployeeId] FOREIGN KEY ([ConvertedEmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Candidates_JobPostings_JobPostingId] FOREIGN KEY ([JobPostingId]) REFERENCES [talent].[JobPostings] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[DailyAttendances] (
        [Id] bigint NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [AttendanceDate] date NOT NULL,
        [WorkShiftId] int NULL,
        [CheckInTime] datetime2 NULL,
        [CheckOutTime] datetime2 NULL,
        [AttendanceSymbol] varchar(10) NOT NULL,
        [WorkingHours] decimal(5,2) NOT NULL,
        [WorkdayCount] decimal(5,2) NOT NULL,
        [LateMinutes] int NOT NULL,
        [EarlyLeaveMinutes] int NOT NULL,
        [OvertimeHours150] decimal(5,2) NOT NULL,
        [OvertimeHours200] decimal(5,2) NOT NULL,
        [OvertimeHours300] decimal(5,2) NOT NULL,
        [NightShiftHours] decimal(5,2) NOT NULL,
        [IsManualEdited] bit NOT NULL,
        [EditReason] nvarchar(300) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DailyAttendances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DailyAttendances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_DailyAttendances_WorkShifts_WorkShiftId] FOREIGN KEY ([WorkShiftId]) REFERENCES [time].[WorkShifts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[DataPrivacyConsents] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [PolicyVersion] varchar(30) NOT NULL,
        [IsConsented] bit NOT NULL,
        [ConsentedAt] datetime2 NOT NULL,
        [IpAddress] varchar(64) NULL,
        [ConsentNote] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_DataPrivacyConsents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_DataPrivacyConsents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[EmployeeAssetAssignments] (
        [Id] int NOT NULL IDENTITY,
        [AssetId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [AssignedDate] date NOT NULL,
        [ReturnedDate] date NULL,
        [ConditionOnAssign] nvarchar(200) NULL,
        [ConditionOnReturn] nvarchar(200) NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Active',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeAssetAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeAssetAssignments_Assets_AssetId] FOREIGN KEY ([AssetId]) REFERENCES [talent].[Assets] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeAssetAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[EmployeeCertificates] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [CertificateName] nvarchar(200) NOT NULL,
        [CertificateType] varchar(30) NOT NULL,
        [IssuedBy] nvarchar(200) NULL,
        [IssueDate] date NULL,
        [ExpiryDate] date NULL,
        [ScoreOrGrade] nvarchar(50) NULL,
        [AttachmentUrl] nvarchar(300) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeCertificates] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeCertificates_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[EmployeeDependents] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [FullName] nvarchar(150) NOT NULL,
        [Relationship] varchar(30) NOT NULL,
        [DateOfBirth] date NULL,
        [CitizenIdEncrypted] nvarchar(512) NULL,
        [TaxCodeEncrypted] nvarchar(512) NULL,
        [IsTaxDeductible] bit NOT NULL,
        [DeductionFromDate] date NULL,
        [DeductionToDate] date NULL,
        [ProofDocumentUrl] nvarchar(300) NULL,
        [IsEmergencyContact] bit NOT NULL,
        [EmergencyPhone] varchar(20) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeDependents] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeDependents_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[EmployeeDocuments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [DocumentCategory] varchar(50) NOT NULL,
        [DocumentTitle] nvarchar(200) NOT NULL,
        [FilePath] nvarchar(300) NOT NULL,
        [ContentType] varchar(100) NULL,
        [FileSizeBytes] bigint NOT NULL,
        [ExpiryDate] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeDocuments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeDocuments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[EmployeeKpiEvaluations] (
        [Id] int NOT NULL IDENTITY,
        [EvaluationCycleId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [ReviewerEmployeeId] int NULL,
        [TotalWeight] decimal(5,2) NOT NULL,
        [SelfTotalScore] decimal(5,2) NOT NULL,
        [ManagerTotalScore] decimal(5,2) NOT NULL,
        [FinalScore] decimal(5,2) NOT NULL,
        [RatingGrade] varchar(10) NOT NULL,
        [BonusMultiplier] decimal(5,2) NOT NULL,
        [EmployeeComment] nvarchar(1000) NULL,
        [ReviewerComment] nvarchar(1000) NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Approved',
        [ApprovedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeKpiEvaluations] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeKpiEvaluations_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeKpiEvaluations_Employees_ReviewerEmployeeId] FOREIGN KEY ([ReviewerEmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeKpiEvaluations_EvaluationCycles_EvaluationCycleId] FOREIGN KEY ([EvaluationCycleId]) REFERENCES [talent].[EvaluationCycles] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[EmployeeOnboardingTasks] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [OnboardingTemplateId] int NULL,
        [ProcessType] varchar(20) NOT NULL DEFAULT 'Onboarding',
        [TaskName] nvarchar(200) NOT NULL,
        [PicEmployeeId] int NULL,
        [DueDate] date NOT NULL,
        [IsCompleted] bit NOT NULL,
        [CompletedAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeOnboardingTasks] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeOnboardingTasks_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_EmployeeOnboardingTasks_OnboardingTemplates_OnboardingTemplateId] FOREIGN KEY ([OnboardingTemplateId]) REFERENCES [talent].[OnboardingTemplates] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[EmploymentHistories] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [FromDate] date NOT NULL,
        [ToDate] date NULL,
        [CompanyName] nvarchar(200) NOT NULL,
        [DepartmentName] nvarchar(200) NULL,
        [PositionName] nvarchar(150) NULL,
        [IsInternal] bit NOT NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmploymentHistories] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmploymentHistories_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[LaborContracts] (
        [Id] int NOT NULL IDENTITY,
        [ContractNumber] varchar(50) NOT NULL,
        [EmployeeId] int NOT NULL,
        [ContractType] varchar(30) NOT NULL,
        [SignDate] date NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NULL,
        [SalaryStepId] int NULL,
        [BaseSalary] decimal(18,2) NOT NULL,
        [PositionAllowance] decimal(18,2) NOT NULL,
        [OtherAllowance] decimal(18,2) NOT NULL,
        [ProbationSalaryRate] decimal(5,2) NOT NULL DEFAULT 100.0,
        [SignerEmployeeId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Active',
        [SignedFileUrl] nvarchar(300) NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LaborContracts] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LaborContracts_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LaborContracts_SalarySteps_SalaryStepId] FOREIGN KEY ([SalaryStepId]) REFERENCES [core].[SalarySteps] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[LeaveBalances] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [Year] int NOT NULL,
        [StandardEntitlement] decimal(5,2) NOT NULL,
        [SeniorityExtraDays] decimal(5,2) NOT NULL,
        [CarriedForwardDays] decimal(5,2) NOT NULL,
        [UsedDays] decimal(5,2) NOT NULL,
        [PendingDays] decimal(5,2) NOT NULL,
        [RemainingDays] decimal(5,2) NOT NULL,
        [CarryForwardExpiryDate] date NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LeaveBalances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LeaveBalances_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveBalances_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [time].[LeaveTypes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[LeaveRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [LeaveTypeId] int NOT NULL,
        [StartDate] date NOT NULL,
        [EndDate] date NOT NULL,
        [StartSession] varchar(20) NOT NULL DEFAULT 'FullDay',
        [EndSession] varchar(20) NOT NULL DEFAULT 'FullDay',
        [TotalDays] decimal(5,2) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [HandoverToEmployeeId] int NULL,
        [AttachmentUrl] nvarchar(300) NULL,
        [ApprovedByEmployeeId] int NULL,
        [WorkflowInstanceId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Pending',
        [ApproverComment] nvarchar(500) NULL,
        [ApprovedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_LeaveRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_LeaveRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_LeaveRequests_LeaveTypes_LeaveTypeId] FOREIGN KEY ([LeaveTypeId]) REFERENCES [time].[LeaveTypes] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[MonthlyAttendanceSummaries] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [Month] int NOT NULL,
        [Year] int NOT NULL,
        [StandardWorkdays] decimal(5,2) NOT NULL,
        [ActualWorkdays] decimal(5,2) NOT NULL,
        [PaidLeaveDays] decimal(5,2) NOT NULL,
        [HolidayDays] decimal(5,2) NOT NULL,
        [SocialInsuranceLeaveDays] decimal(5,2) NOT NULL,
        [UnpaidLeaveDays] decimal(5,2) NOT NULL,
        [TotalLateCount] int NOT NULL,
        [TotalLateMinutes] int NOT NULL,
        [TotalOtHours150] decimal(5,2) NOT NULL,
        [TotalOtHours200] decimal(5,2) NOT NULL,
        [TotalOtHours300] decimal(5,2) NOT NULL,
        [TotalNightHours] decimal(5,2) NOT NULL,
        [TotalPayableWorkdays] decimal(5,2) NOT NULL,
        [IsLocked] bit NOT NULL,
        [LockedAt] datetime2 NULL,
        [LockedBy] nvarchar(100) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_MonthlyAttendanceSummaries] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_MonthlyAttendanceSummaries_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[OvertimeRequests] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [OvertimeDate] date NOT NULL,
        [StartTime] time NOT NULL,
        [EndTime] time NOT NULL,
        [RequestedHours] decimal(5,2) NOT NULL,
        [ApprovedHours] decimal(5,2) NOT NULL,
        [OvertimeRateType] varchar(20) NOT NULL,
        [Reason] nvarchar(500) NULL,
        [ApprovedByEmployeeId] int NULL,
        [WorkflowInstanceId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Pending',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_OvertimeRequests] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_OvertimeRequests_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[PersonnelTransfers] (
        [Id] int NOT NULL IDENTITY,
        [DecisionNumber] varchar(50) NOT NULL,
        [EmployeeId] int NOT NULL,
        [TransferType] varchar(30) NOT NULL,
        [FromUnitId] int NOT NULL,
        [ToUnitId] int NOT NULL,
        [FromPositionId] int NOT NULL,
        [ToPositionId] int NOT NULL,
        [DecisionDate] date NOT NULL,
        [EffectiveDate] date NOT NULL,
        [Reason] nvarchar(500) NULL,
        [ApprovedByEmployeeId] int NULL,
        [WorkflowInstanceId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Approved',
        [AppliedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PersonnelTransfers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PersonnelTransfers_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelTransfers_OrganizationUnits_FromUnitId] FOREIGN KEY ([FromUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelTransfers_OrganizationUnits_ToUnitId] FOREIGN KEY ([ToUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelTransfers_Positions_FromPositionId] FOREIGN KEY ([FromPositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PersonnelTransfers_Positions_ToPositionId] FOREIGN KEY ([ToPositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[RewardDisciplines] (
        [Id] int NOT NULL IDENTITY,
        [DecisionNumber] varchar(50) NOT NULL,
        [EmployeeId] int NOT NULL,
        [CategoryId] int NOT NULL,
        [Type] varchar(20) NOT NULL,
        [DecisionDate] date NOT NULL,
        [EffectiveDate] date NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [Reason] nvarchar(1000) NULL,
        [SignedByEmployeeId] int NULL,
        [PayrollPeriodId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Approved',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_RewardDisciplines] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_RewardDisciplines_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_RewardDisciplines_RewardDisciplineCategories_CategoryId] FOREIGN KEY ([CategoryId]) REFERENCES [core].[RewardDisciplineCategories] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[SalaryProgressions] (
        [Id] int NOT NULL IDENTITY,
        [DecisionNumber] varchar(50) NOT NULL,
        [EmployeeId] int NOT NULL,
        [OldSalaryStepId] int NULL,
        [NewSalaryStepId] int NULL,
        [OldBaseSalary] decimal(18,2) NOT NULL,
        [NewBaseSalary] decimal(18,2) NOT NULL,
        [OldAllowance] decimal(18,2) NOT NULL,
        [NewAllowance] decimal(18,2) NOT NULL,
        [EffectiveDate] date NOT NULL,
        [NextReviewDate] date NULL,
        [Reason] nvarchar(500) NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Applied',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_SalaryProgressions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_SalaryProgressions_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryProgressions_SalarySteps_NewSalaryStepId] FOREIGN KEY ([NewSalaryStepId]) REFERENCES [core].[SalarySteps] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_SalaryProgressions_SalarySteps_OldSalaryStepId] FOREIGN KEY ([OldSalaryStepId]) REFERENCES [core].[SalarySteps] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [time].[ShiftSchedules] (
        [Id] bigint NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [WorkShiftId] int NOT NULL,
        [WorkDate] date NOT NULL,
        [IsDayOff] bit NOT NULL,
        [Notes] nvarchar(200) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ShiftSchedules] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ShiftSchedules_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_ShiftSchedules_WorkShifts_WorkShiftId] FOREIGN KEY ([WorkShiftId]) REFERENCES [time].[WorkShifts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[TrainingEnrollments] (
        [Id] int NOT NULL IDENTITY,
        [TrainingClassId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [EnrolledDate] date NOT NULL,
        [AttendanceRatePercent] decimal(5,2) NOT NULL,
        [FinalScore] decimal(5,2) NOT NULL,
        [Result] varchar(30) NOT NULL DEFAULT 'Passed',
        [CertificateNumber] nvarchar(100) NULL,
        [FeedbackRating] int NULL,
        [FeedbackComment] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_TrainingEnrollments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingEnrollments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_TrainingEnrollments_TrainingClasses_TrainingClassId] FOREIGN KEY ([TrainingClassId]) REFERENCES [talent].[TrainingClasses] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[Users] (
        [Id] int NOT NULL IDENTITY,
        [Username] varchar(50) NOT NULL,
        [Email] nvarchar(150) NOT NULL,
        [PasswordHash] nvarchar(500) NOT NULL,
        [EmployeeId] int NULL,
        [IsActive] bit NOT NULL,
        [TwoFactorEnabled] bit NOT NULL,
        [TwoFactorSecret] nvarchar(256) NULL,
        [AccessFailedCount] int NOT NULL,
        [LockoutEnd] datetime2 NULL,
        [LastLoginAt] datetime2 NULL,
        [PasswordChangedAt] datetime2 NULL,
        [MustChangePassword] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Users_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[WorkflowInstances] (
        [Id] int NOT NULL IDENTITY,
        [WorkflowDefinitionId] int NOT NULL,
        [EntityType] varchar(50) NOT NULL,
        [EntityId] int NOT NULL,
        [InitiatorEmployeeId] int NOT NULL,
        [CurrentStepOrder] int NOT NULL,
        [CurrentApproverUserId] int NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'InProgress',
        [CompletedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowInstances] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowInstances_Employees_InitiatorEmployeeId] FOREIGN KEY ([InitiatorEmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowInstances_WorkflowDefinitions_WorkflowDefinitionId] FOREIGN KEY ([WorkflowDefinitionId]) REFERENCES [system].[WorkflowDefinitions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[Interviews] (
        [Id] int NOT NULL IDENTITY,
        [CandidateId] int NOT NULL,
        [RoundNumber] int NOT NULL,
        [RoundTitle] nvarchar(150) NOT NULL,
        [ScheduledStartTime] datetime2 NOT NULL,
        [ScheduledEndTime] datetime2 NOT NULL,
        [InterviewMode] varchar(30) NOT NULL DEFAULT 'InPerson',
        [LocationOrLink] nvarchar(300) NULL,
        [InterviewerEmployeeId] int NULL,
        [TechnicalScore] decimal(5,2) NOT NULL,
        [CultureFitScore] decimal(5,2) NOT NULL,
        [TotalScore] decimal(5,2) NOT NULL,
        [EvaluationNotes] nvarchar(1000) NULL,
        [Result] varchar(30) NOT NULL DEFAULT 'Pending',
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Interviews] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Interviews_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [talent].[Candidates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Interviews_Employees_InterviewerEmployeeId] FOREIGN KEY ([InterviewerEmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[JobOffers] (
        [Id] int NOT NULL IDENTITY,
        [OfferCode] varchar(30) NOT NULL,
        [CandidateId] int NOT NULL,
        [ProposedPositionId] int NOT NULL,
        [ProposedUnitId] int NOT NULL,
        [OfferedProbationSalary] decimal(18,2) NOT NULL,
        [OfferedOfficialSalary] decimal(18,2) NOT NULL,
        [ExpectedStartDate] date NOT NULL,
        [OfferExpiryDate] date NOT NULL,
        [Status] varchar(30) NOT NULL DEFAULT 'Sent',
        [CandidateFeedback] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_JobOffers] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_JobOffers_Candidates_CandidateId] FOREIGN KEY ([CandidateId]) REFERENCES [talent].[Candidates] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobOffers_OrganizationUnits_ProposedUnitId] FOREIGN KEY ([ProposedUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_JobOffers_Positions_ProposedPositionId] FOREIGN KEY ([ProposedPositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [talent].[EmployeeKpiItems] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeKpiEvaluationId] int NOT NULL,
        [GoalTitle] nvarchar(300) NOT NULL,
        [Perspective] varchar(30) NOT NULL,
        [MeasureUnit] nvarchar(100) NOT NULL,
        [WeightPercent] decimal(5,2) NOT NULL,
        [TargetValue] decimal(18,2) NOT NULL,
        [ActualValue] decimal(18,2) NOT NULL,
        [CompletionRatePercent] decimal(5,2) NOT NULL,
        [SelfScore] decimal(5,2) NOT NULL,
        [ManagerScore] decimal(5,2) NOT NULL,
        [EvidenceNote] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_EmployeeKpiItems] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_EmployeeKpiItems_EmployeeKpiEvaluations_EmployeeKpiEvaluationId] FOREIGN KEY ([EmployeeKpiEvaluationId]) REFERENCES [talent].[EmployeeKpiEvaluations] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [core].[ContractAddendums] (
        [Id] int NOT NULL IDENTITY,
        [LaborContractId] int NOT NULL,
        [AddendumNumber] varchar(50) NOT NULL,
        [SignDate] date NOT NULL,
        [EffectiveDate] date NOT NULL,
        [ChangeType] nvarchar(200) NOT NULL,
        [NewBaseSalary] decimal(18,2) NULL,
        [NewAllowance] decimal(18,2) NULL,
        [ContentSummary] nvarchar(1000) NULL,
        [SignedFileUrl] nvarchar(300) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_ContractAddendums] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_ContractAddendums_LaborContracts_LaborContractId] FOREIGN KEY ([LaborContractId]) REFERENCES [core].[LaborContracts] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[PayrollRecords] (
        [Id] bigint NOT NULL IDENTITY,
        [PayrollPeriodId] int NOT NULL,
        [EmployeeId] int NOT NULL,
        [OrganizationUnitId] int NOT NULL,
        [PositionId] int NOT NULL,
        [MonthlyAttendanceSummaryId] int NULL,
        [StandardWorkdays] decimal(5,2) NOT NULL,
        [PayableWorkdays] decimal(5,2) NOT NULL,
        [TotalOtHours] decimal(5,2) NOT NULL,
        [ContractBaseSalary] decimal(18,2) NOT NULL,
        [ActualBaseSalary] decimal(18,2) NOT NULL,
        [PositionAllowance] decimal(18,2) NOT NULL,
        [OtherAllowance] decimal(18,2) NOT NULL,
        [OvertimePay] decimal(18,2) NOT NULL,
        [KpiBonus] decimal(18,2) NOT NULL,
        [GrossIncome] decimal(18,2) NOT NULL,
        [InsuranceSalaryBase] decimal(18,2) NOT NULL,
        [EmployeeSocialInsurance] decimal(18,2) NOT NULL,
        [EmployeeHealthInsurance] decimal(18,2) NOT NULL,
        [EmployeeUnemploymentInsurance] decimal(18,2) NOT NULL,
        [TotalEmployeeInsurance] decimal(18,2) NOT NULL,
        [CompanySocialInsurance] decimal(18,2) NOT NULL,
        [CompanyHealthInsurance] decimal(18,2) NOT NULL,
        [CompanyUnemploymentInsurance] decimal(18,2) NOT NULL,
        [CompanyUnionFee] decimal(18,2) NOT NULL,
        [DependentCount] int NOT NULL,
        [SelfDeductionAmount] decimal(18,2) NOT NULL,
        [DependentDeductionAmount] decimal(18,2) NOT NULL,
        [TaxableIncome] decimal(18,2) NOT NULL,
        [AssessableIncome] decimal(18,2) NOT NULL,
        [PersonalIncomeTax] decimal(18,2) NOT NULL,
        [DisciplineDeduction] decimal(18,2) NOT NULL,
        [NetSalary] decimal(18,2) NOT NULL,
        [PaymentStatus] varchar(30) NOT NULL DEFAULT 'Paid',
        [PaidAt] datetime2 NULL,
        [Notes] nvarchar(500) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PayrollRecords] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollRecords_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [core].[Employees] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRecords_MonthlyAttendanceSummaries_MonthlyAttendanceSummaryId] FOREIGN KEY ([MonthlyAttendanceSummaryId]) REFERENCES [time].[MonthlyAttendanceSummaries] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRecords_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRecords_PayrollPeriods_PayrollPeriodId] FOREIGN KEY ([PayrollPeriodId]) REFERENCES [payroll].[PayrollPeriods] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRecords_Positions_PositionId] FOREIGN KEY ([PositionId]) REFERENCES [core].[Positions] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[Notifications] (
        [Id] bigint NOT NULL IDENTITY,
        [RecipientUserId] int NOT NULL,
        [Title] nvarchar(200) NOT NULL,
        [Message] nvarchar(1000) NOT NULL,
        [NotificationType] varchar(50) NOT NULL,
        [TargetUrl] nvarchar(300) NULL,
        [IsRead] bit NOT NULL,
        [ReadAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Notifications_Users_RecipientUserId] FOREIGN KEY ([RecipientUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[UserDataScopes] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [OrganizationUnitId] int NOT NULL,
        [IncludeSubUnits] bit NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserDataScopes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserDataScopes_OrganizationUnits_OrganizationUnitId] FOREIGN KEY ([OrganizationUnitId]) REFERENCES [core].[OrganizationUnits] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserDataScopes_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[UserRoles] (
        [Id] int NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [RoleId] int NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserRoles_Roles_RoleId] FOREIGN KEY ([RoleId]) REFERENCES [identity].[Roles] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_UserRoles_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [identity].[UserSessions] (
        [Id] bigint NOT NULL IDENTITY,
        [UserId] int NOT NULL,
        [RefreshTokenHash] varchar(500) NOT NULL,
        [IpAddress] varchar(64) NULL,
        [UserAgent] nvarchar(500) NULL,
        [ExpiresAt] datetime2 NOT NULL,
        [RevokedAt] datetime2 NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_UserSessions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_UserSessions_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [system].[WorkflowApprovalLogs] (
        [Id] bigint NOT NULL IDENTITY,
        [WorkflowInstanceId] int NOT NULL,
        [StepOrder] int NOT NULL,
        [StepName] nvarchar(150) NOT NULL,
        [ApproverUserId] int NOT NULL,
        [Action] varchar(20) NOT NULL,
        [Comment] nvarchar(500) NULL,
        [ActionAt] datetime2 NOT NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_WorkflowApprovalLogs] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_WorkflowApprovalLogs_Users_ApproverUserId] FOREIGN KEY ([ApproverUserId]) REFERENCES [identity].[Users] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_WorkflowApprovalLogs_WorkflowInstances_WorkflowInstanceId] FOREIGN KEY ([WorkflowInstanceId]) REFERENCES [system].[WorkflowInstances] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE TABLE [payroll].[PayrollRecordDetails] (
        [Id] bigint NOT NULL IDENTITY,
        [PayrollRecordId] bigint NOT NULL,
        [SalaryComponentId] int NOT NULL,
        [ComponentCode] varchar(50) NOT NULL,
        [ComponentName] nvarchar(150) NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [CalculationNote] nvarchar(300) NULL,
        [CreatedAt] datetime2(7) NOT NULL DEFAULT (SYSUTCDATETIME()),
        [CreatedBy] nvarchar(100) NOT NULL DEFAULT N'SYSTEM',
        [UpdatedAt] datetime2(7) NULL,
        [UpdatedBy] nvarchar(100) NULL,
        [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
        [RowVersion] rowversion NOT NULL,
        CONSTRAINT [PK_PayrollRecordDetails] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_PayrollRecordDetails_PayrollRecords_PayrollRecordId] FOREIGN KEY ([PayrollRecordId]) REFERENCES [payroll].[PayrollRecords] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_PayrollRecordDetails_SalaryComponents_SalaryComponentId] FOREIGN KEY ([SalaryComponentId]) REFERENCES [payroll].[SalaryComponents] ([Id]) ON DELETE NO ACTION
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendations_EmployeeId] ON [system].[AiRecommendations] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendations_OrganizationUnitId] ON [system].[AiRecommendations] ([OrganizationUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AiRecommendations_Type_Priority] ON [system].[AiRecommendations] ([RecommendationType], [PriorityLevel], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Assets_AssetCode] ON [talent].[Assets] ([AssetCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AttendanceLogs_Emp_PunchTime] ON [time].[AttendanceLogs] ([EmployeeId], [PunchTime]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_AuditLogs_Entity_CreatedAt] ON [system].[AuditLogs] ([EntityName], [EntityId], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Candidates_ConvertedEmployeeId] ON [talent].[Candidates] ([ConvertedEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Candidates_EducationLevelId] ON [talent].[Candidates] ([EducationLevelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Candidates_Posting_Stage] ON [talent].[Candidates] ([JobPostingId], [Stage]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Candidates_CandidateCode] ON [talent].[Candidates] ([CandidateCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ContractAddendums_LaborContractId] ON [core].[ContractAddendums] ([LaborContractId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_ContractAddendums_AddendumNumber] ON [core].[ContractAddendums] ([AddendumNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DailyAttendances_Date_Symbol] ON [time].[DailyAttendances] ([AttendanceDate], [AttendanceSymbol]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_DailyAttendances_WorkShiftId] ON [time].[DailyAttendances] ([WorkShiftId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_DailyAttendances_Employee_Date] ON [time].[DailyAttendances] ([EmployeeId], [AttendanceDate]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_DataPrivacyConsents_Emp_Version] ON [identity].[DataPrivacyConsents] ([EmployeeId], [PolicyVersion]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_EducationLevels_LevelCode] ON [core].[EducationLevels] ([LevelCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssetAssignments_AssetId] ON [talent].[EmployeeAssetAssignments] ([AssetId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeAssetAssignments_Emp_Status] ON [talent].[EmployeeAssetAssignments] ([EmployeeId], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeCertificates_EmployeeId] ON [core].[EmployeeCertificates] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeDependents_Emp_Tax] ON [core].[EmployeeDependents] ([EmployeeId], [IsTaxDeductible]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeDocuments_EmployeeId] ON [core].[EmployeeDocuments] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeKpiEvaluations_EmployeeId] ON [talent].[EmployeeKpiEvaluations] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeKpiEvaluations_ReviewerEmployeeId] ON [talent].[EmployeeKpiEvaluations] ([ReviewerEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_EmployeeKpiEvaluations_Cycle_Emp] ON [talent].[EmployeeKpiEvaluations] ([EvaluationCycleId], [EmployeeId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeKpiItems_EvaluationId] ON [talent].[EmployeeKpiItems] ([EmployeeKpiEvaluationId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeOnboardingTasks_Emp_Type] ON [talent].[EmployeeOnboardingTasks] ([EmployeeId], [ProcessType]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmployeeOnboardingTasks_OnboardingTemplateId] ON [talent].[EmployeeOnboardingTasks] ([OnboardingTemplateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DirectManagerId] ON [core].[Employees] ([DirectManagerId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_EducationLevelId] ON [core].[Employees] ([EducationLevelId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_FullName] ON [core].[Employees] ([FullName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_PositionId] ON [core].[Employees] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_SalaryStepId] ON [core].[Employees] ([SalaryStepId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_Unit_Status_HireDate] ON [core].[Employees] ([OrganizationUnitId], [WorkStatus], [HireDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Employees_CitizenIdHash] ON [core].[Employees] ([CitizenIdHash]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Employees_EmployeeCode] ON [core].[Employees] ([EmployeeCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Employees_WorkEmail] ON [core].[Employees] ([WorkEmail]) WHERE [WorkEmail] IS NOT NULL AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_EmploymentHistories_EmployeeId] ON [core].[EmploymentHistories] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_EvaluationCycles_CycleCode] ON [talent].[EvaluationCycles] ([CycleCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Holidays_HolidayDate] ON [time].[Holidays] ([HolidayDate]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Interviews_CandidateId] ON [talent].[Interviews] ([CandidateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Interviews_InterviewerEmployeeId] ON [talent].[Interviews] ([InterviewerEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobOffers_CandidateId] ON [talent].[JobOffers] ([CandidateId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobOffers_ProposedPositionId] ON [talent].[JobOffers] ([ProposedPositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobOffers_ProposedUnitId] ON [talent].[JobOffers] ([ProposedUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_JobOffers_OfferCode] ON [talent].[JobOffers] ([OfferCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobPostings_OrganizationUnitId] ON [talent].[JobPostings] ([OrganizationUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobPostings_PositionId] ON [talent].[JobPostings] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_JobPostings_RecruitmentRequestId] ON [talent].[JobPostings] ([RecruitmentRequestId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_JobPostings_PostingCode] ON [talent].[JobPostings] ([PostingCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LaborContracts_EmployeeId] ON [core].[LaborContracts] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LaborContracts_SalaryStepId] ON [core].[LaborContracts] ([SalaryStepId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LaborContracts_Status_EndDate] ON [core].[LaborContracts] ([Status], [EndDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_LaborContracts_ContractNumber] ON [core].[LaborContracts] ([ContractNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveBalances_LeaveTypeId] ON [time].[LeaveBalances] ([LeaveTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_LeaveBalances_Emp_Type_Year] ON [time].[LeaveBalances] ([EmployeeId], [LeaveTypeId], [Year]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_Emp_Start_Status] ON [time].[LeaveRequests] ([EmployeeId], [StartDate], [Status]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_LeaveRequests_LeaveTypeId] ON [time].[LeaveRequests] ([LeaveTypeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_LeaveTypes_Code] ON [time].[LeaveTypes] ([LeaveTypeCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_MenuItems_MenuCode] ON [system].[MenuItems] ([MenuCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_MonthlyAttendanceSummaries_Emp_Month_Year] ON [time].[MonthlyAttendanceSummaries] ([EmployeeId], [Year], [Month]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Notifications_Recipient_Read] ON [system].[Notifications] ([RecipientUserId], [IsRead], [CreatedAt]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OrganizationUnits_ParentUnitId] ON [core].[OrganizationUnits] ([ParentUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_OrganizationUnits_UnitCode] ON [core].[OrganizationUnits] ([UnitCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_OvertimeRequests_Emp_Date] ON [time].[OvertimeRequests] ([EmployeeId], [OvertimeDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_PayrollParameters_ParamCode] ON [payroll].[PayrollParameters] ([ParamCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_PayrollPeriods_PeriodCode] ON [payroll].[PayrollPeriods] ([PeriodCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_PayrollPeriods_Year_Month] ON [payroll].[PayrollPeriods] ([Year], [Month]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecordDetails_PayrollRecordId] ON [payroll].[PayrollRecordDetails] ([PayrollRecordId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecordDetails_SalaryComponentId] ON [payroll].[PayrollRecordDetails] ([SalaryComponentId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecords_EmployeeId] ON [payroll].[PayrollRecords] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecords_MonthlyAttendanceSummaryId] ON [payroll].[PayrollRecords] ([MonthlyAttendanceSummaryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecords_PositionId] ON [payroll].[PayrollRecords] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PayrollRecords_Unit_Period] ON [payroll].[PayrollRecords] ([OrganizationUnitId], [PayrollPeriodId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_PayrollRecords_Period_Employee] ON [payroll].[PayrollRecords] ([PayrollPeriodId], [EmployeeId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Permissions_PermissionCode] ON [identity].[Permissions] ([PermissionCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonnelTransfers_Emp_Date] ON [core].[PersonnelTransfers] ([EmployeeId], [EffectiveDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonnelTransfers_FromPositionId] ON [core].[PersonnelTransfers] ([FromPositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonnelTransfers_FromUnitId] ON [core].[PersonnelTransfers] ([FromUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonnelTransfers_ToPositionId] ON [core].[PersonnelTransfers] ([ToPositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_PersonnelTransfers_ToUnitId] ON [core].[PersonnelTransfers] ([ToUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_PersonnelTransfers_DecisionNumber] ON [core].[PersonnelTransfers] ([DecisionNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Positions_PositionCode] ON [core].[Positions] ([PositionCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecruitmentRequests_OrganizationUnitId] ON [talent].[RecruitmentRequests] ([OrganizationUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RecruitmentRequests_PositionId] ON [talent].[RecruitmentRequests] ([PositionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_RecruitmentRequests_RequestCode] ON [talent].[RecruitmentRequests] ([RequestCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_RewardDisciplineCategories_Code] ON [core].[RewardDisciplineCategories] ([CategoryCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RewardDisciplines_CategoryId] ON [core].[RewardDisciplines] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RewardDisciplines_Emp_Type_Date] ON [core].[RewardDisciplines] ([EmployeeId], [Type], [EffectiveDate]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_RewardDisciplines_DecisionNumber] ON [core].[RewardDisciplines] ([DecisionNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_RolePermissions_PermissionId] ON [identity].[RolePermissions] ([PermissionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_RolePermissions_Role_Perm] ON [identity].[RolePermissions] ([RoleId], [PermissionId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Roles_RoleCode] ON [identity].[Roles] ([RoleCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_SalaryComponents_ComponentCode] ON [payroll].[SalaryComponents] ([ComponentCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SalaryProgressions_EmployeeId] ON [core].[SalaryProgressions] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SalaryProgressions_NewSalaryStepId] ON [core].[SalaryProgressions] ([NewSalaryStepId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_SalaryProgressions_OldSalaryStepId] ON [core].[SalaryProgressions] ([OldSalaryStepId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_SalaryProgressions_DecisionNumber] ON [core].[SalaryProgressions] ([DecisionNumber]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_SalaryScales_ScaleCode] ON [core].[SalaryScales] ([ScaleCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_SalarySteps_Scale_Order] ON [core].[SalarySteps] ([SalaryScaleId], [StepOrder]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_ShiftSchedules_WorkShiftId] ON [time].[ShiftSchedules] ([WorkShiftId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_ShiftSchedules_Emp_Date] ON [time].[ShiftSchedules] ([EmployeeId], [WorkDate]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_TaxBrackets_BracketLevel] ON [payroll].[TaxBrackets] ([BracketLevel]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrainingClasses_TrainingCourseId] ON [talent].[TrainingClasses] ([TrainingCourseId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_TrainingClasses_ClassCode] ON [talent].[TrainingClasses] ([ClassCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_TrainingCourses_CourseCode] ON [talent].[TrainingCourses] ([CourseCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrainingEnrollments_EmployeeId] ON [talent].[TrainingEnrollments] ([EmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_TrainingEnrollments_Class_Employee] ON [talent].[TrainingEnrollments] ([TrainingClassId], [EmployeeId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserDataScopes_OrganizationUnitId] ON [identity].[UserDataScopes] ([OrganizationUnitId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_UserDataScopes_User_Unit] ON [identity].[UserDataScopes] ([UserId], [OrganizationUnitId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserRoles_RoleId] ON [identity].[UserRoles] ([RoleId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_UserRoles_User_Role] ON [identity].[UserRoles] ([UserId], [RoleId]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Users_Email] ON [identity].[Users] ([Email]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Users_EmployeeId] ON [identity].[Users] ([EmployeeId]) WHERE [EmployeeId] IS NOT NULL AND [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_Users_Username] ON [identity].[Users] ([Username]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_UserSessions_UserId] ON [identity].[UserSessions] ([UserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowApprovalLogs_ApproverUserId] ON [system].[WorkflowApprovalLogs] ([ApproverUserId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowApprovalLogs_InstanceId] ON [system].[WorkflowApprovalLogs] ([WorkflowInstanceId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_WorkflowDefinitions_WorkflowCode] ON [system].[WorkflowDefinitions] ([WorkflowCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstances_Entity] ON [system].[WorkflowInstances] ([EntityType], [EntityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstances_InitiatorEmployeeId] ON [system].[WorkflowInstances] ([InitiatorEmployeeId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_WorkflowInstances_WorkflowDefinitionId] ON [system].[WorkflowInstances] ([WorkflowDefinitionId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_WorkflowSteps_Def_Order] ON [system].[WorkflowSteps] ([WorkflowDefinitionId], [StepOrder]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [UQ_WorkShifts_ShiftCode] ON [time].[WorkShifts] ([ShiftCode]) WHERE [IsDeleted] = 0');
END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN

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
        SUM(CASE WHEN e.Gender = N'Nam' OR e.Gender = 'Male' THEN 1 ELSE 0 END) AS MaleCount,
        SUM(CASE WHEN e.Gender = N'Nữ' OR e.Gender = 'Female' THEN 1 ELSE 0 END) AS FemaleCount,
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

END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN

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

END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN

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

END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN

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

END;
GO

IF NOT EXISTS (
    SELECT * FROM [dbo].[__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261115072302_InitialCreate'
)
BEGIN
    INSERT INTO [dbo].[__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261115072302_InitialCreate', N'8.0.11');
END;
GO

COMMIT;
GO

