using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EmployeeCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(e => e.EmployeeCode)
            .IsUnique();

        builder.Property(e => e.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.LastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.CitizenId)
            .IsRequired()
            .HasMaxLength(250); // Mở rộng để chứa chuỗi mã hóa Data Protection

        builder.Property(e => e.CitizenIdIssuePlace)
            .HasMaxLength(150);

        builder.Property(e => e.TaxCode)
            .HasMaxLength(50);

        builder.Property(e => e.Email)
            .HasMaxLength(150);

        builder.HasIndex(e => e.Email);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.BankAccountNumber)
            .HasMaxLength(250); // Mở rộng để chứa chuỗi mã hóa Data Protection

        builder.Property(e => e.BankName)
            .HasMaxLength(150);

        builder.Property(e => e.BankBranch)
            .HasMaxLength(150);

        builder.Property(e => e.EmergencyContactName)
            .HasMaxLength(150);

        builder.Property(e => e.EmergencyContactPhone)
            .HasMaxLength(50);

        builder.Property(e => e.EmergencyContactRelationship)
            .HasMaxLength(50);

        builder.Property(e => e.BaseSalary)
            .HasColumnType("decimal(18,0)");

        builder.Property(e => e.Allowance)
            .HasColumnType("decimal(18,0)");

        builder.Property(e => e.InsuranceSalary)
            .HasColumnType("decimal(18,0)");

        builder.Property(e => e.AnnualLeaveBalance)
            .HasPrecision(5, 2);

        builder.Property(e => e.DiligenceScore)
            .HasPrecision(5, 2);

        builder.Property(e => e.LegacyId)
            .HasMaxLength(50);

        builder.HasIndex(e => e.LegacyId);

        builder.Property(e => e.SalaryStep)
            .IsRequired()
            .HasDefaultValue(1);

        builder.Property(e => e.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(e => e.UpdatedBy)
            .HasMaxLength(100);

        // Foreign Key: Unit
        builder.HasOne(e => e.OrganizationalUnit)
            .WithMany(u => u.Employees)
            .HasForeignKey(e => e.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Foreign Key: Manager
        builder.HasOne(e => e.Manager)
            .WithMany(m => m.Subordinates)
            .HasForeignKey(e => e.ManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Job & Grade
        builder.HasOne(e => e.StandardJobTitle)
            .WithMany()
            .HasForeignKey(e => e.StandardJobTitleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.StandardJobTitleId);

        builder.HasOne(e => e.JobGrade)
            .WithMany()
            .HasForeignKey(e => e.JobGradeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.JobGradeId);

        // Collections
        builder.HasMany(e => e.Dependents)
            .WithOne(d => d.Employee)
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Educations)
            .WithOne(ed => ed.Employee)
            .HasForeignKey(ed => ed.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Certificates)
            .WithOne(c => c.Employee)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.WorkHistories)
            .WithOne(w => w.Employee)
            .HasForeignKey(w => w.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.Attachments)
            .WithOne(a => a.Employee)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        // TPH Discriminator Mapping
        builder.HasDiscriminator<EmployeeType>("EmployeeType")
            .HasValue<OfficeEmployee>(EmployeeType.Office)
            .HasValue<FactoryWorker>(EmployeeType.FactoryWorker)
            .HasValue<FarmSpecialist>(EmployeeType.FarmSpecialist)
            .HasValue<StoreAssociate>(EmployeeType.StoreAssociate);
    }
}
