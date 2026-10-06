using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Shifts;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> builder)
    {
        builder.ToTable("Shifts");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.ShiftCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(s => s.ShiftCode)
            .IsUnique();

        builder.Property(s => s.ShiftName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.MultiplierRate)
            .HasPrecision(5, 2);

        builder.Property(s => s.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(s => s.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(s => s.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(s => s.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
