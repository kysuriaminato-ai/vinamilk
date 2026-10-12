using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Careers;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class JobPostingConfiguration : IEntityTypeConfiguration<JobPosting>
{
    public void Configure(EntityTypeBuilder<JobPosting> builder)
    {
        builder.ToTable("JobPostings");

        builder.HasKey(j => j.Id);

        builder.Property(j => j.JobCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(j => j.JobCode)
            .IsUnique();

        builder.Property(j => j.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(j => j.Slug)
            .IsRequired()
            .HasMaxLength(250);

        builder.HasIndex(j => j.Slug)
            .IsUnique();

        builder.Property(j => j.Department)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(j => j.WorkLocation)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(j => j.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.EmploymentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.ExperienceLevel)
            .HasMaxLength(100);

        builder.Property(j => j.EducationRequirement)
            .HasMaxLength(100);

        builder.Property(j => j.SalaryMin)
            .HasPrecision(18, 2);

        builder.Property(j => j.SalaryMax)
            .HasPrecision(18, 2);

        builder.Property(j => j.SalaryDisplay)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(j => j.Description)
            .IsRequired();

        builder.Property(j => j.Requirements)
            .IsRequired();

        builder.Property(j => j.Benefits)
            .IsRequired();

        builder.Property(j => j.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(j => j.UpdatedBy)
            .HasMaxLength(100);

        builder.HasMany(j => j.Applications)
            .WithOne(a => a.JobPosting)
            .HasForeignKey(a => a.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
