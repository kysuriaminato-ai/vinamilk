using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class AttendanceConfiguration : IEntityTypeConfiguration<Attendance>
{
    public void Configure(EntityTypeBuilder<Attendance> builder)
    {
        builder.ToTable("Attendances");

        builder.HasKey(a => a.Id);

        builder.HasIndex(a => a.AttendanceDate);

        builder.HasIndex(a => new { a.EmployeeId, a.AttendanceDate })
            .IsUnique();

        builder.Property(a => a.WorkHours)
            .HasPrecision(5, 2);

        builder.Property(a => a.OvertimeHours)
            .HasPrecision(5, 2);

        builder.Property(a => a.Note)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(a => a.Employee)
            .WithMany(e => e.Attendances)
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.Shift)
            .WithMany()
            .HasForeignKey(a => a.ShiftId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
