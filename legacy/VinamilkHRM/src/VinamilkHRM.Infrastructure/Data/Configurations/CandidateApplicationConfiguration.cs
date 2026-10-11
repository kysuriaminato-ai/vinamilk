using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Careers;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class CandidateApplicationConfiguration : IEntityTypeConfiguration<CandidateApplication>
{
    public void Configure(EntityTypeBuilder<CandidateApplication> builder)
    {
        builder.ToTable("CandidateApplications");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.FullName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.Email)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.PhoneNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.CitizenId)
            .HasMaxLength(20);

        builder.Property(a => a.Address)
            .HasMaxLength(300);

        builder.Property(a => a.ResumeFileName)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(a => a.CoverLetter)
            .HasMaxLength(2000);

        builder.Property(a => a.PortfolioUrl)
            .HasMaxLength(300);

        builder.Property(a => a.InterviewScore)
            .HasPrecision(5, 2);

        builder.Property(a => a.EvaluationNotes)
            .HasMaxLength(1000);

        builder.Property(a => a.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(a => a.JobPosting)
            .WithMany(j => j.Applications)
            .HasForeignKey(a => a.JobPostingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(a => a.HiredEmployee)
            .WithMany()
            .HasForeignKey(a => a.HiredEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
