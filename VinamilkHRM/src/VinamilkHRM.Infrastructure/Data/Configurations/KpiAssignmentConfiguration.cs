using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Kpis;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class KpiAssignmentConfiguration : IEntityTypeConfiguration<KpiAssignment>
{
    public void Configure(EntityTypeBuilder<KpiAssignment> builder)
    {
        builder.ToTable("KpiAssignments", b => b.IsTemporal());

        builder.HasKey(k => k.Id);

        builder.Property(k => k.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(k => new { k.EmployeeId, k.Period });

        builder.Property(k => k.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(k => k.Description)
            .HasMaxLength(1000);

        builder.Property(k => k.TargetMetric)
            .HasMaxLength(500);

        builder.Property(k => k.WeightPercentage)
            .HasPrecision(5, 2);

        builder.Property(k => k.ActualResult)
            .HasMaxLength(2000);

        builder.Property(k => k.SelfEvaluationScore)
            .HasPrecision(5, 2);

        builder.Property(k => k.FinalScore)
            .HasPrecision(5, 2);

        builder.Property(k => k.BaseScore)
            .HasPrecision(5, 2);

        builder.Property(k => k.SlaBonusPenaltyScore)
            .HasPrecision(5, 2);

        builder.Property(k => k.ScoreAdjustment)
            .HasPrecision(5, 2);

        builder.Property(k => k.SlaCalculationDetail)
            .HasMaxLength(500);

        builder.Property(k => k.Feedback)
            .HasMaxLength(1000);

        builder.Property(k => k.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(k => k.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(k => k.Employee)
            .WithMany(e => e.KpiAssignments)
            .HasForeignKey(k => k.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(k => k.Evaluator)
            .WithMany()
            .HasForeignKey(k => k.EvaluatorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
