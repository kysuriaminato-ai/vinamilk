using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class OvertimeRequestConfiguration : IEntityTypeConfiguration<OvertimeRequest>
{
    public void Configure(EntityTypeBuilder<OvertimeRequest> builder)
    {
        builder.ToTable("OvertimeRequests");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.PlannedHours).HasPrecision(18, 2);
        builder.Property(o => o.Reason).IsRequired().HasMaxLength(500);
        builder.Property(o => o.RejectionReason).HasMaxLength(500);

        builder.HasOne(o => o.Employee)
            .WithMany()
            .HasForeignKey(o => o.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.TeamLeader)
            .WithMany()
            .HasForeignKey(o => o.TeamLeaderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(o => o.UnitManager)
            .WithMany()
            .HasForeignKey(o => o.UnitManagerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.EmployeeId, o.WorkDate });
    }
}

public class ShiftSwapRequestConfiguration : IEntityTypeConfiguration<ShiftSwapRequest>
{
    public void Configure(EntityTypeBuilder<ShiftSwapRequest> builder)
    {
        builder.ToTable("ShiftSwapRequests");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.Reason).IsRequired().HasMaxLength(500);
        builder.Property(s => s.RejectionReason).HasMaxLength(500);

        builder.HasOne(s => s.RequesterEmployee)
            .WithMany()
            .HasForeignKey(s => s.RequesterEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.TargetEmployee)
            .WithMany()
            .HasForeignKey(s => s.TargetEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Approver)
            .WithMany()
            .HasForeignKey(s => s.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AttendanceExplanationConfiguration : IEntityTypeConfiguration<AttendanceExplanation>
{
    public void Configure(EntityTypeBuilder<AttendanceExplanation> builder)
    {
        builder.ToTable("AttendanceExplanations");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Reason).IsRequired().HasMaxLength(500);
        builder.Property(e => e.RejectionReason).HasMaxLength(500);

        builder.HasOne(e => e.Employee)
            .WithMany()
            .HasForeignKey(e => e.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.Approver)
            .WithMany()
            .HasForeignKey(e => e.ApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.EmployeeId, e.AttendanceDate });
    }
}
