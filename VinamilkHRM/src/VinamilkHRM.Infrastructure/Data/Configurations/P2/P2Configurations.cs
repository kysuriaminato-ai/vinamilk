using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.P2;

namespace VinamilkHRM.Infrastructure.Data.Configurations.P2;

public class JobRequisitionConfiguration : IEntityTypeConfiguration<JobRequisition>
{
    public void Configure(EntityTypeBuilder<JobRequisition> builder)
    {
        builder.ToTable("JobRequisitions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.RequisitionCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(r => r.RequisitionCode).IsUnique();
        builder.Property(r => r.BudgetSalary).HasPrecision(18, 0);
    }
}

public class TrainingCourseConfiguration : IEntityTypeConfiguration<TrainingCourse>
{
    public void Configure(EntityTypeBuilder<TrainingCourse> builder)
    {
        builder.ToTable("TrainingCourses");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.CourseCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(c => c.CourseCode).IsUnique();
        builder.Property(c => c.CostPerPerson).HasPrecision(18, 0);
    }
}

public class EmployeeCertificationConfiguration : IEntityTypeConfiguration<EmployeeCertification>
{
    public void Configure(EntityTypeBuilder<EmployeeCertification> builder)
    {
        builder.ToTable("EmployeeCertifications");
        builder.HasKey(c => c.Id);
        builder.HasOne(c => c.Employee).WithMany().HasForeignKey(c => c.EmployeeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasIndex(c => new { c.EmployeeId, c.ExpiryDate, c.Status });
    }
}

public class LegalEntityConfiguration : IEntityTypeConfiguration<LegalEntity>
{
    public void Configure(EntityTypeBuilder<LegalEntity> builder)
    {
        builder.ToTable("LegalEntities");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.TaxCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(e => e.TaxCode).IsUnique();
    }
}

public class SafetyIncidentConfiguration : IEntityTypeConfiguration<SafetyIncident>
{
    public void Configure(EntityTypeBuilder<SafetyIncident> builder)
    {
        builder.ToTable("SafetyIncidents");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.IncidentCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(s => s.IncidentCode).IsUnique();
    }
}