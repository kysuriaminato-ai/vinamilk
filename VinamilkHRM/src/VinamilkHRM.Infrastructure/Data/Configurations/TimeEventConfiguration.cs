using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class TimeEventConfiguration : IEntityTypeConfiguration<TimeEvent>
{
    public void Configure(EntityTypeBuilder<TimeEvent> builder)
    {
        builder.ToTable("TimeEvents");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.DeviceId)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(t => new { t.DeviceId, t.EmployeeId, t.Timestamp })
            .IsUnique();

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(t => t.IpAddress)
            .HasMaxLength(50);
    }
}
