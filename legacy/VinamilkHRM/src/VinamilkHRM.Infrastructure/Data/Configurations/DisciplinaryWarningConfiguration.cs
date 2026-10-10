using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Attendances;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class DisciplinaryWarningConfiguration : IEntityTypeConfiguration<DisciplinaryWarning>
{
    public void Configure(EntityTypeBuilder<DisciplinaryWarning> builder)
    {
        builder.ToTable("DisciplinaryWarnings");

        builder.HasKey(d => d.Id);

        builder.Property(d => d.ViolationTitle)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(d => d.Details)
            .HasMaxLength(1000);

        builder.Property(d => d.ResolutionNotes)
            .HasMaxLength(1000);

        builder.Property(d => d.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(d => d.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(d => d.WarningDate);
        builder.HasIndex(d => new { d.EmployeeId, d.IsResolved });
    }
}
