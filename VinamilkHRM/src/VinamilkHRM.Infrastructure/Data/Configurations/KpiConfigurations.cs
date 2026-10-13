using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Kpis;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class KpiDefinitionConfiguration : IEntityTypeConfiguration<KpiDefinition>
{
    public void Configure(EntityTypeBuilder<KpiDefinition> builder)
    {
        builder.ToTable("KpiDefinitions", b => b.IsTemporal());
        builder.HasKey(k => k.Id);

        builder.Property(k => k.Code).IsRequired().HasMaxLength(50);
        builder.HasIndex(k => k.Code).IsUnique();

        builder.Property(k => k.Name).IsRequired().HasMaxLength(250);
        builder.Property(k => k.Unit).IsRequired().HasMaxLength(50);
        builder.Property(k => k.Description).HasMaxLength(1000);
        builder.Property(k => k.MeasurementGuide).HasMaxLength(2000);

        builder.HasOne(k => k.JobFamily)
            .WithMany()
            .HasForeignKey(k => k.JobFamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(k => k.Thresholds)
            .WithOne(t => t.KpiDefinition)
            .HasForeignKey(t => t.KpiDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KpiThresholdConfiguration : IEntityTypeConfiguration<KpiThreshold>
{
    public void Configure(EntityTypeBuilder<KpiThreshold> builder)
    {
        builder.ToTable("KpiThresholds");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.MinThreshold).HasPrecision(18, 4);
        builder.Property(t => t.MaxThreshold).HasPrecision(18, 4);
        builder.Property(t => t.Score).HasPrecision(6, 2);
    }
}

public class KpiSchemeConfiguration : IEntityTypeConfiguration<KpiScheme>
{
    public void Configure(EntityTypeBuilder<KpiScheme> builder)
    {
        builder.ToTable("KpiSchemes", b => b.IsTemporal());
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UnitResultWeight).HasPrecision(5, 2);
        builder.Property(s => s.IndividualWorkWeight).HasPrecision(5, 2);
        builder.Property(s => s.PeopleManagementWeight).HasPrecision(5, 2);
        builder.Property(s => s.ComplianceWeight).HasPrecision(5, 2);

        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => new { s.LeadershipLevel, s.EffectiveFrom });
    }
}

public class KpiPeriodConfiguration : IEntityTypeConfiguration<KpiPeriod>
{
    public void Configure(EntityTypeBuilder<KpiPeriod> builder)
    {
        builder.ToTable("KpiPeriods", b => b.IsTemporal());
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Period).IsRequired().HasMaxLength(10);
        builder.HasIndex(p => p.Period).IsUnique();

        builder.Property(p => p.ClosedBy).HasMaxLength(100);

        builder.HasMany(p => p.Sheets)
            .WithOne(s => s.KpiPeriod)
            .HasForeignKey(s => s.KpiPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiSheetConfiguration : IEntityTypeConfiguration<KpiSheet>
{
    public void Configure(EntityTypeBuilder<KpiSheet> builder)
    {
        builder.ToTable("KpiSheets", b => b.IsTemporal());
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Period).IsRequired().HasMaxLength(10);
        builder.HasIndex(s => new { s.EmployeeId, s.Period }).IsUnique();

        builder.Property(s => s.StandardWorkDays).HasPrecision(6, 2);
        builder.Property(s => s.ActualWorkDays).HasPrecision(6, 2);

        builder.Property(s => s.SelfReviewLatePenalty).HasPrecision(6, 2);
        builder.Property(s => s.ManagerReviewLatePenalty).HasPrecision(6, 2);

        builder.Property(s => s.UnitResultScore).HasPrecision(6, 2);
        builder.Property(s => s.IndividualWorkScore).HasPrecision(6, 2);
        builder.Property(s => s.PeopleManagementScore).HasPrecision(6, 2);
        builder.Property(s => s.ComplianceScore).HasPrecision(6, 2);
        builder.Property(s => s.TotalScore).HasPrecision(6, 2);
        builder.Property(s => s.BonusCoefficient).HasPrecision(4, 2);

        builder.Property(s => s.ManagerFeedback).HasMaxLength(2000);
        builder.Property(s => s.CalibrationNotes).HasMaxLength(1000);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Evaluator)
            .WithMany()
            .HasForeignKey(s => s.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(s => s.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.JobFamily)
            .WithMany()
            .HasForeignKey(s => s.JobFamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(s => s.JobTitle)
            .WithMany()
            .HasForeignKey(s => s.JobTitleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(s => s.Items)
            .WithOne(i => i.KpiSheet)
            .HasForeignKey(i => i.KpiSheetId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KpiItemConfiguration : IEntityTypeConfiguration<KpiItem>
{
    public void Configure(EntityTypeBuilder<KpiItem> builder)
    {
        builder.ToTable("KpiItems", b => b.IsTemporal());
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title).IsRequired().HasMaxLength(250);
        builder.Property(i => i.Description).HasMaxLength(1000);
        builder.Property(i => i.Unit).IsRequired().HasMaxLength(50);

        builder.Property(i => i.TargetValue).HasPrecision(18, 4);
        builder.Property(i => i.ActualValue).HasPrecision(18, 4);
        builder.Property(i => i.WeightPercentage).HasPrecision(5, 2);

        builder.Property(i => i.SelfScore).HasPrecision(6, 2);
        builder.Property(i => i.ManagerScore).HasPrecision(6, 2);
        builder.Property(i => i.Adjustment).HasPrecision(6, 2);
        builder.Property(i => i.FinalScore).HasPrecision(6, 2);

        builder.Property(i => i.AdjustmentReason).HasMaxLength(1000);
        builder.Property(i => i.EvidenceUrl).HasMaxLength(500);
        builder.Property(i => i.EvidenceDescription).HasMaxLength(2000);

        builder.HasOne(i => i.KpiDefinition)
            .WithMany()
            .HasForeignKey(i => i.KpiDefinitionId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(i => i.ExtensionRequests)
            .WithOne(r => r.KpiItem)
            .HasForeignKey(r => r.KpiItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KpiExtensionRequestConfiguration : IEntityTypeConfiguration<KpiExtensionRequest>
{
    public void Configure(EntityTypeBuilder<KpiExtensionRequest> builder)
    {
        builder.ToTable("KpiExtensionRequests");
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Reason).IsRequired().HasMaxLength(1000);
        builder.Property(r => r.Comment).HasMaxLength(1000);
    }
}

public class KpiResultConfiguration : IEntityTypeConfiguration<KpiResult>
{
    public void Configure(EntityTypeBuilder<KpiResult> builder)
    {
        builder.ToTable("KpiResults", b => b.IsTemporal());
        builder.HasKey(r => r.Id);

        builder.Property(r => r.Period).IsRequired().HasMaxLength(10);
        builder.HasIndex(r => new { r.EmployeeId, r.Period }).IsUnique();

        builder.Property(r => r.TotalScore).HasPrecision(6, 2);
        builder.Property(r => r.BonusCoefficient).HasPrecision(4, 2);

        builder.Property(r => r.UnitResultScore).HasPrecision(6, 2);
        builder.Property(r => r.IndividualWorkScore).HasPrecision(6, 2);
        builder.Property(r => r.PeopleManagementScore).HasPrecision(6, 2);
        builder.Property(r => r.ComplianceScore).HasPrecision(6, 2);

        builder.Property(r => r.LockedBy).IsRequired().HasMaxLength(100);

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiAppealConfiguration : IEntityTypeConfiguration<KpiAppeal>
{
    public void Configure(EntityTypeBuilder<KpiAppeal> builder)
    {
        builder.ToTable("KpiAppeals");
        builder.HasKey(a => a.Id);

        builder.Property(a => a.Period).IsRequired().HasMaxLength(10);
        builder.Property(a => a.Reason).IsRequired().HasMaxLength(2000);
        builder.Property(a => a.ReviewerComment).HasMaxLength(2000);

        builder.Property(a => a.CurrentScore).HasPrecision(6, 2);
        builder.Property(a => a.ProposedScore).HasPrecision(6, 2);
        builder.Property(a => a.AdjustedScore).HasPrecision(6, 2);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.KpiSheet)
            .WithMany()
            .HasForeignKey(a => a.KpiSheetId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiMetricConfiguration : IEntityTypeConfiguration<KpiMetric>
{
    public void Configure(EntityTypeBuilder<KpiMetric> builder)
    {
        builder.ToTable("KpiMetrics");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MetricCode).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Period).IsRequired().HasMaxLength(10);
        builder.Property(m => m.Unit).IsRequired().HasMaxLength(50);
        builder.Property(m => m.Value).HasPrecision(18, 4);
        builder.Property(m => m.BatchReference).HasMaxLength(100);
        builder.Property(m => m.Notes).HasMaxLength(500);

        builder.HasIndex(m => new { m.MetricCode, m.Period, m.OrganizationalUnitId, m.EmployeeId });

        builder.HasOne(m => m.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(m => m.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Employee)
            .WithMany()
            .HasForeignKey(m => m.EmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class KpiTemplateConfiguration : IEntityTypeConfiguration<KpiTemplate>
{
    public void Configure(EntityTypeBuilder<KpiTemplate> builder)
    {
        builder.ToTable("KpiTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.TemplateCode).IsRequired().HasMaxLength(50);
        builder.HasIndex(t => t.TemplateCode).IsUnique();
        builder.Property(t => t.TemplateName).IsRequired().HasMaxLength(250);

        builder.HasOne(t => t.JobTitle)
            .WithMany()
            .HasForeignKey(t => t.JobTitleId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(t => t.JobFamily)
            .WithMany()
            .HasForeignKey(t => t.JobFamilyId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.KpiTemplate)
            .HasForeignKey(i => i.KpiTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class KpiTemplateItemConfiguration : IEntityTypeConfiguration<KpiTemplateItem>
{
    public void Configure(EntityTypeBuilder<KpiTemplateItem> builder)
    {
        builder.ToTable("KpiTemplateItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.DefaultTarget).HasPrecision(18, 4);
        builder.Property(i => i.WeightPercentage).HasPrecision(5, 2);

        builder.HasOne(i => i.KpiDefinition)
            .WithMany()
            .HasForeignKey(i => i.KpiDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class KpiConfigurationEntityConfiguration : IEntityTypeConfiguration<KpiConfiguration>
{
    public void Configure(EntityTypeBuilder<KpiConfiguration> builder)
    {
        builder.ToTable("KpiConfigurations", b => b.IsTemporal());
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Code).IsRequired().HasMaxLength(100);
        builder.Property(c => c.Value).HasPrecision(18, 4);
        builder.Property(c => c.StringValue).HasMaxLength(500);
        builder.Property(c => c.Description).HasMaxLength(500);

        builder.HasIndex(c => new { c.Code, c.EffectiveFrom });
    }
}
