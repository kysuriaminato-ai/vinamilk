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
            .HasMaxLength(20);

        builder.HasIndex(e => e.CitizenId)
            .IsUnique();

        builder.Property(e => e.Email)
            .HasMaxLength(150);

        builder.Property(e => e.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(e => e.Address)
            .HasMaxLength(500);

        builder.Property(e => e.BaseSalary)
            .HasPrecision(18, 2);

        builder.Property(e => e.Allowance)
            .HasPrecision(18, 2);

        builder.Property(e => e.InsuranceSalary)
            .HasPrecision(18, 2);

        builder.Property(e => e.AnnualLeaveBalance)
            .HasPrecision(5, 2);

        builder.Property(e => e.DiligenceScore)
            .HasPrecision(5, 2);

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

        // TPH Discriminator Mapping
        builder.HasDiscriminator<EmployeeType>("EmployeeType")
            .HasValue<OfficeEmployee>(EmployeeType.Office)
            .HasValue<FactoryWorker>(EmployeeType.FactoryWorker)
            .HasValue<FarmSpecialist>(EmployeeType.FarmSpecialist)
            .HasValue<StoreAssociate>(EmployeeType.StoreAssociate);
    }
}
