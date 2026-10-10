using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.LeaveRequests;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class LeaveRequestConfiguration : IEntityTypeConfiguration<LeaveRequest>
{
    public void Configure(EntityTypeBuilder<LeaveRequest> builder)
    {
        builder.ToTable("LeaveRequests");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.TotalDays)
            .HasPrecision(5, 2);

        builder.Property(l => l.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(l => l.ApprovalNotes)
            .HasMaxLength(500);

        builder.Property(l => l.SocialInsuranceBenefitRate)
            .HasPrecision(5, 2);

        builder.Property(l => l.SocialInsuranceFormCode)
            .HasMaxLength(100);

        builder.Property(l => l.SocialInsuranceFormContent)
            .HasMaxLength(4000);

        builder.Property(l => l.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(l => l.Employee)
            .WithMany(e => e.LeaveRequests)
            .HasForeignKey(l => l.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(l => l.ApprovedBy)
            .WithMany()
            .HasForeignKey(l => l.ApprovedById)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
