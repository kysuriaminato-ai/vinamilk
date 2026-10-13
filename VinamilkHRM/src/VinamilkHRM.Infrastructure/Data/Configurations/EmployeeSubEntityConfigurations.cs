using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class AllowanceTypeConfiguration : IEntityTypeConfiguration<AllowanceType>
{
    public void Configure(EntityTypeBuilder<AllowanceType> builder)
    {
        builder.ToTable("AllowanceTypes");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.TaxExemptCap)
            .HasColumnType("decimal(18,0)")
            .HasDefaultValue(0m);

        builder.HasIndex(a => a.Code);
    }
}

public class ContractAllowanceConfiguration : IEntityTypeConfiguration<ContractAllowance>
{
    public void Configure(EntityTypeBuilder<ContractAllowance> builder)
    {
        builder.ToTable("ContractAllowances");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Amount)
            .HasColumnType("decimal(18,0)");

        builder.Property(c => c.Note)
            .HasMaxLength(500);

        builder.HasOne(c => c.LaborContract)
            .WithMany()
            .HasForeignKey(c => c.LaborContractId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(c => c.AllowanceType)
            .WithMany()
            .HasForeignKey(c => c.AllowanceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.AllowanceTypeId);
        builder.HasIndex(c => new { c.LaborContractId, c.AllowanceTypeId });
    }
}

public class DependentConfiguration : IEntityTypeConfiguration<Dependent>
{
    public void Configure(EntityTypeBuilder<Dependent> builder)
    {
        builder.ToTable("Dependents");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(d => d.Relationship)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(d => d.TaxCode)
            .HasMaxLength(50);

        builder.Property(d => d.CitizenId)
            .HasMaxLength(50);

        builder.Property(d => d.Note)
            .HasMaxLength(500);

        builder.HasOne(d => d.Employee)
            .WithMany(e => e.Dependents)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => new { d.EmployeeId, d.FromMonth });
    }
}

public class EmployeeEducationConfiguration : IEntityTypeConfiguration<EmployeeEducation>
{
    public void Configure(EntityTypeBuilder<EmployeeEducation> builder)
    {
        builder.ToTable("EmployeeEducations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.DegreeLevel)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.Major)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.SchoolName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(e => e.Classification)
            .HasMaxLength(50);

        builder.Property(e => e.EducationType)
            .HasMaxLength(100);

        builder.HasOne(e => e.Employee)
            .WithMany(emp => emp.Educations)
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(e => e.EmployeeId);
    }
}

public class EmployeeCertificateConfiguration : IEntityTypeConfiguration<EmployeeCertificate>
{
    public void Configure(EntityTypeBuilder<EmployeeCertificate> builder)
    {
        builder.ToTable("EmployeeCertificates");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.CertificateName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(c => c.CertificateCode)
            .HasMaxLength(100);

        builder.Property(c => c.IssuingAuthority)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(c => c.ScoreOrGrade)
            .HasMaxLength(50);

        builder.HasOne(c => c.Employee)
            .WithMany(e => e.Certificates)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => new { c.EmployeeId, c.ExpiryDate });
    }
}

public class EmployeeWorkHistoryConfiguration : IEntityTypeConfiguration<EmployeeWorkHistory>
{
    public void Configure(EntityTypeBuilder<EmployeeWorkHistory> builder)
    {
        builder.ToTable("EmployeeWorkHistories");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.CompanyName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(w => w.JobTitle)
            .HasMaxLength(150);

        builder.Property(w => w.MainResponsibilities)
            .HasMaxLength(1000);

        builder.Property(w => w.ReferenceContact)
            .HasMaxLength(250);

        builder.Property(w => w.Note)
            .HasMaxLength(500);

        builder.HasOne(w => w.Employee)
            .WithMany(e => e.WorkHistories)
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.EmployeeId, w.FromDate });
        builder.HasIndex(w => w.LegacyId);
    }
}

public class EmployeeAttachmentConfiguration : IEntityTypeConfiguration<EmployeeAttachment>
{
    public void Configure(EntityTypeBuilder<EmployeeAttachment> builder)
    {
        builder.ToTable("EmployeeAttachments");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.FileName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(a => a.DocumentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.FilePath)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.ChecksumSha256)
            .IsRequired()
            .HasMaxLength(64);

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(a => a.EmployeeId);
    }
}
