using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.Core;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class OrganizationUnitConfiguration : IEntityTypeConfiguration<OrganizationUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationUnit> builder)
    {
        builder.ConfigureBaseAuditable<OrganizationUnit, int>("OrganizationUnits", "core");

        builder.Property(x => x.UnitCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.UnitName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.UnitType).HasColumnType("varchar(30)").HasDefaultValue("Department");
        builder.Property(x => x.TreePath).HasColumnType("varchar(250)");
        builder.Property(x => x.Address).HasMaxLength(300);
        builder.Property(x => x.Phone).HasColumnType("varchar(30)");
        builder.Property(x => x.CostCenterCode).HasColumnType("varchar(20)");
        builder.Property(x => x.WageRegion).HasDefaultValue(1);

        builder.HasIndex(x => x.UnitCode)
            .IsUnique()
            .HasDatabaseName("UQ_OrganizationUnits_UnitCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.ParentUnitId).HasDatabaseName("IX_OrganizationUnits_ParentUnitId");

        builder.HasOne(x => x.ParentUnit)
            .WithMany(p => p.SubUnits)
            .HasForeignKey(x => x.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ConfigureBaseAuditable<Position, int>("Positions", "core");

        builder.Property(x => x.PositionCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.PositionName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.PositionGroup).HasColumnType("varchar(30)").HasDefaultValue("Staff");
        builder.Property(x => x.DefaultPositionAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.JobDescription).HasMaxLength(1000);

        builder.HasIndex(x => x.PositionCode)
            .IsUnique()
            .HasDatabaseName("UQ_Positions_PositionCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class SalaryScaleConfiguration : IEntityTypeConfiguration<SalaryScale>
{
    public void Configure(EntityTypeBuilder<SalaryScale> builder)
    {
        builder.ConfigureBaseAuditable<SalaryScale, int>("SalaryScales", "core");

        builder.Property(x => x.ScaleCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.ScaleName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.ScaleCode)
            .IsUnique()
            .HasDatabaseName("UQ_SalaryScales_ScaleCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class SalaryStepConfiguration : IEntityTypeConfiguration<SalaryStep>
{
    public void Configure(EntityTypeBuilder<SalaryStep> builder)
    {
        builder.ConfigureBaseAuditable<SalaryStep, int>("SalarySteps", "core");

        builder.Property(x => x.StepName).HasMaxLength(50).IsRequired();
        builder.Property(x => x.StandardSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SalaryCoefficient).HasColumnType("decimal(8,4)");

        builder.HasIndex(x => new { x.SalaryScaleId, x.StepOrder })
            .IsUnique()
            .HasDatabaseName("UQ_SalarySteps_Scale_Order")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.SalaryScale)
            .WithMany(s => s.Steps)
            .HasForeignKey(x => x.SalaryScaleId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EducationLevelConfiguration : IEntityTypeConfiguration<EducationLevel>
{
    public void Configure(EntityTypeBuilder<EducationLevel> builder)
    {
        builder.ConfigureBaseAuditable<EducationLevel, int>("EducationLevels", "core");

        builder.Property(x => x.LevelCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.LevelName).HasMaxLength(100).IsRequired();

        builder.HasIndex(x => x.LevelCode)
            .IsUnique()
            .HasDatabaseName("UQ_EducationLevels_LevelCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ConfigureBaseAuditable<Employee, int>("Employees", "core");

        builder.Property(x => x.EmployeeCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Gender).HasMaxLength(10).IsRequired();
        builder.Property(x => x.CitizenIdEncrypted).HasMaxLength(512).IsRequired();
        builder.Property(x => x.CitizenIdHash).HasColumnType("char(64)").IsRequired();
        builder.Property(x => x.CitizenIdIssuePlace).HasMaxLength(200);
        builder.Property(x => x.Ethnicity).HasMaxLength(50);
        builder.Property(x => x.Religion).HasMaxLength(50);
        builder.Property(x => x.MaritalStatus).HasColumnType("varchar(20)");
        builder.Property(x => x.PlaceOfOrigin).HasMaxLength(200);
        builder.Property(x => x.PermanentAddress).HasMaxLength(300);
        builder.Property(x => x.CurrentAddress).HasMaxLength(300);
        builder.Property(x => x.PhoneNumber).HasColumnType("varchar(20)");
        builder.Property(x => x.WorkEmail).HasColumnType("varchar(150)");
        builder.Property(x => x.PersonalEmail).HasColumnType("varchar(150)");
        builder.Property(x => x.BankAccountEncrypted).HasMaxLength(512);
        builder.Property(x => x.BankName).HasMaxLength(150);
        builder.Property(x => x.BankBranch).HasMaxLength(150);
        builder.Property(x => x.TaxCodeEncrypted).HasMaxLength(512);
        builder.Property(x => x.SocialInsuranceNumber).HasColumnType("varchar(30)");
        builder.Property(x => x.HealthInsuranceNumber).HasColumnType("varchar(30)");
        builder.Property(x => x.HealthInsurancePlace).HasMaxLength(200);
        builder.Property(x => x.Major).HasMaxLength(150);
        builder.Property(x => x.BaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PositionAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.WorkStatus).HasColumnType("varchar(30)").HasDefaultValue("Official");
        builder.Property(x => x.CurrentContractType).HasColumnType("varchar(30)").HasDefaultValue("Indefinite");
        builder.Property(x => x.AvatarUrl).HasMaxLength(300);

        // Unique constraints cho Mã NV, CCCD Hash và Email công vụ
        builder.HasIndex(x => x.EmployeeCode)
            .IsUnique()
            .HasDatabaseName("UQ_Employees_EmployeeCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.CitizenIdHash)
            .IsUnique()
            .HasDatabaseName("UQ_Employees_CitizenIdHash")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => x.WorkEmail)
            .IsUnique()
            .HasDatabaseName("UQ_Employees_WorkEmail")
            .HasFilter("[WorkEmail] IS NOT NULL AND [IsDeleted] = 0");

        // Index tìm kiếm tổng hợp và báo cáo
        builder.HasIndex(x => new { x.OrganizationUnitId, x.WorkStatus, x.HireDate })
            .HasDatabaseName("IX_Employees_Unit_Status_HireDate");

        builder.HasIndex(x => x.FullName)
            .HasDatabaseName("IX_Employees_FullName");

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany(u => u.Employees)
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany(p => p.Employees)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.DirectManager)
            .WithMany()
            .HasForeignKey(x => x.DirectManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.EducationLevel)
            .WithMany(e => e.Employees)
            .HasForeignKey(x => x.EducationLevelId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryStep)
            .WithMany(s => s.Employees)
            .HasForeignKey(x => x.SalaryStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeDependentConfiguration : IEntityTypeConfiguration<EmployeeDependent>
{
    public void Configure(EntityTypeBuilder<EmployeeDependent> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeDependent, int>("EmployeeDependents", "core");

        builder.Property(x => x.FullName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Relationship).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.CitizenIdEncrypted).HasMaxLength(512);
        builder.Property(x => x.TaxCodeEncrypted).HasMaxLength(512);
        builder.Property(x => x.ProofDocumentUrl).HasMaxLength(300);
        builder.Property(x => x.EmergencyPhone).HasColumnType("varchar(20)");

        builder.HasIndex(x => new { x.EmployeeId, x.IsTaxDeductible })
            .HasDatabaseName("IX_EmployeeDependents_Emp_Tax");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Dependents)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeCertificateConfiguration : IEntityTypeConfiguration<EmployeeCertificate>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificate> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeCertificate, int>("EmployeeCertificates", "core");

        builder.Property(x => x.CertificateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.CertificateType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.IssuedBy).HasMaxLength(200);
        builder.Property(x => x.ScoreOrGrade).HasMaxLength(50);
        builder.Property(x => x.AttachmentUrl).HasMaxLength(300);

        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeeCertificates_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Certificates)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeDocumentConfiguration : IEntityTypeConfiguration<EmployeeDocument>
{
    public void Configure(EntityTypeBuilder<EmployeeDocument> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeDocument, int>("EmployeeDocuments", "core");

        builder.Property(x => x.DocumentCategory).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.DocumentTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.FilePath).HasMaxLength(300).IsRequired();
        builder.Property(x => x.ContentType).HasColumnType("varchar(100)");

        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmployeeDocuments_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Documents)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class LaborContractConfiguration : IEntityTypeConfiguration<LaborContract>
{
    public void Configure(EntityTypeBuilder<LaborContract> builder)
    {
        builder.ConfigureBaseAuditable<LaborContract, int>("LaborContracts", "core");

        builder.Property(x => x.ContractNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ContractType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.BaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PositionAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ProbationSalaryRate).HasColumnType("decimal(5,2)").HasDefaultValue(100m);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Active");
        builder.Property(x => x.SignedFileUrl).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.ContractNumber)
            .IsUnique()
            .HasDatabaseName("UQ_LaborContracts_ContractNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.Status, x.EndDate })
            .HasDatabaseName("IX_LaborContracts_Status_EndDate");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Contracts)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryStep)
            .WithMany()
            .HasForeignKey(x => x.SalaryStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ContractAddendumConfiguration : IEntityTypeConfiguration<ContractAddendum>
{
    public void Configure(EntityTypeBuilder<ContractAddendum> builder)
    {
        builder.ConfigureBaseAuditable<ContractAddendum, int>("ContractAddendums", "core");

        builder.Property(x => x.AddendumNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ChangeType).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NewBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ContentSummary).HasMaxLength(1000);
        builder.Property(x => x.SignedFileUrl).HasMaxLength(300);

        builder.HasIndex(x => x.AddendumNumber)
            .IsUnique()
            .HasDatabaseName("UQ_ContractAddendums_AddendumNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.LaborContract)
            .WithMany(c => c.Addendums)
            .HasForeignKey(x => x.LaborContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmploymentHistoryConfiguration : IEntityTypeConfiguration<EmploymentHistory>
{
    public void Configure(EntityTypeBuilder<EmploymentHistory> builder)
    {
        builder.ConfigureBaseAuditable<EmploymentHistory, int>("EmploymentHistories", "core");

        builder.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.DepartmentName).HasMaxLength(200);
        builder.Property(x => x.PositionName).HasMaxLength(150);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => x.EmployeeId).HasDatabaseName("IX_EmploymentHistories_EmployeeId");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.EmploymentHistories)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PersonnelTransferConfiguration : IEntityTypeConfiguration<PersonnelTransfer>
{
    public void Configure(EntityTypeBuilder<PersonnelTransfer> builder)
    {
        builder.ConfigureBaseAuditable<PersonnelTransfer, int>("PersonnelTransfers", "core");

        builder.Property(x => x.DecisionNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.TransferType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => x.DecisionNumber)
            .IsUnique()
            .HasDatabaseName("UQ_PersonnelTransfers_DecisionNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveDate })
            .HasDatabaseName("IX_PersonnelTransfers_Emp_Date");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.Transfers)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromUnit)
            .WithMany()
            .HasForeignKey(x => x.FromUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToUnit)
            .WithMany()
            .HasForeignKey(x => x.ToUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.FromPosition)
            .WithMany()
            .HasForeignKey(x => x.FromPositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ToPosition)
            .WithMany()
            .HasForeignKey(x => x.ToPositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryProgressionConfiguration : IEntityTypeConfiguration<SalaryProgression>
{
    public void Configure(EntityTypeBuilder<SalaryProgression> builder)
    {
        builder.ConfigureBaseAuditable<SalaryProgression, int>("SalaryProgressions", "core");

        builder.Property(x => x.DecisionNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.OldBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OldAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NewAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).HasMaxLength(500);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Applied");

        builder.HasIndex(x => x.DecisionNumber)
            .IsUnique()
            .HasDatabaseName("UQ_SalaryProgressions_DecisionNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.SalaryProgressions)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OldSalaryStep)
            .WithMany()
            .HasForeignKey(x => x.OldSalaryStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.NewSalaryStep)
            .WithMany()
            .HasForeignKey(x => x.NewSalaryStepId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class RewardDisciplineCategoryConfiguration : IEntityTypeConfiguration<RewardDisciplineCategory>
{
    public void Configure(EntityTypeBuilder<RewardDisciplineCategory> builder)
    {
        builder.ConfigureBaseAuditable<RewardDisciplineCategory, int>("RewardDisciplineCategories", "core");

        builder.Property(x => x.CategoryCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.CategoryName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CategoryType).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.DefaultAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.CategoryCode)
            .IsUnique()
            .HasDatabaseName("UQ_RewardDisciplineCategories_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class RewardDisciplineConfiguration : IEntityTypeConfiguration<RewardDiscipline>
{
    public void Configure(EntityTypeBuilder<RewardDiscipline> builder)
    {
        builder.ConfigureBaseAuditable<RewardDiscipline, int>("RewardDisciplines", "core");

        builder.Property(x => x.DecisionNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Type).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.Reason).HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => x.DecisionNumber)
            .IsUnique()
            .HasDatabaseName("UQ_RewardDisciplines_DecisionNumber")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EmployeeId, x.Type, x.EffectiveDate })
            .HasDatabaseName("IX_RewardDisciplines_Emp_Type_Date");

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.RewardDisciplines)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Category)
            .WithMany(c => c.RewardDisciplines)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

