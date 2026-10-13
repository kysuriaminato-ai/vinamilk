using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class RosterConfiguration : IEntityTypeConfiguration<RosterEntry>
{
    public void Configure(EntityTypeBuilder<RosterEntry> builder)
    {
        builder.ToTable("RosterEntries");
        builder.HasKey(r => r.Id);

        builder.HasIndex(r => new { r.EmployeeId, r.WorkDate })
            .IsUnique();

        builder.HasOne(r => r.Employee)
            .WithMany()
            .HasForeignKey(r => r.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ShiftDefinition)
            .WithMany()
            .HasForeignKey(r => r.ShiftDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.Note)
            .HasMaxLength(250);
    }
}
