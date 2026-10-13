using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Analytics;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class ExitSurveyConfiguration : IEntityTypeConfiguration<ExitSurvey>
{
    public void Configure(EntityTypeBuilder<ExitSurvey> builder)
    {
        builder.ToTable("ExitSurveys", "rpt");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.DetailedReason).HasMaxLength(1000);
        builder.Property(s => s.ImprovementSuggestions).HasMaxLength(1000);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}