using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Jobs;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class JobFamilyConfiguration : IEntityTypeConfiguration<JobFamily>
{
    public void Configure(EntityTypeBuilder<JobFamily> builder)
    {
        builder.ToTable("JobFamilies");
        builder.HasKey(j => j.Id);

        builder.Property(j => j.FamilyCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(j => j.FamilyName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(j => j.Description)
            .HasMaxLength(500);

        builder.Property(j => j.CreatedBy)
            .HasMaxLength(100);

        builder.Property(j => j.UpdatedBy)
            .HasMaxLength(100);

        builder.HasIndex(j => j.FamilyCode);
    }
}

public class JobGradeConfiguration : IEntityTypeConfiguration<JobGrade>
{
    public void Configure(EntityTypeBuilder<JobGrade> builder)
    {
        builder.ToTable("JobGrades");
        builder.HasKey(g => g.Id);

        builder.Property(g => g.GradeCode)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(g => g.GradeName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(g => g.Description)
            .HasMaxLength(500);

        builder.Property(g => g.LegacyId)
            .HasMaxLength(50);

        builder.HasIndex(g => g.GradeCode);
        builder.HasIndex(g => g.GradeNumber);
        builder.HasIndex(g => g.LegacyId);
    }
}

public class JobTitleConfiguration : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.ToTable("JobTitles");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.JobTitleCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.JobTitleName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.Description)
            .HasMaxLength(500);

        builder.Property(t => t.LegacyId)
            .HasMaxLength(50);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100);

        builder.Property(t => t.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(t => t.JobFamily)
            .WithMany(f => f.JobTitles)
            .HasForeignKey(t => t.JobFamilyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.StandardJobGrade)
            .WithMany()
            .HasForeignKey(t => t.StandardJobGradeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(t => t.JobTitleCode);
        builder.HasIndex(t => t.JobFamilyId);
        builder.HasIndex(t => t.StandardJobGradeId);
        builder.HasIndex(t => t.LegacyId);
    }
}

public class SalaryScaleConfiguration : IEntityTypeConfiguration<SalaryScale>
{
    public void Configure(EntityTypeBuilder<SalaryScale> builder)
    {
        builder.ToTable("SalaryScales");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Amount)
            .HasColumnType("decimal(18,0)");

        builder.Property(s => s.LegalReference)
            .HasMaxLength(200);

        builder.HasOne(s => s.JobGrade)
            .WithMany(g => g.SalaryScales)
            .HasForeignKey(s => s.JobGradeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.JobGradeId, s.Step, s.EffectiveFrom });
    }
}
