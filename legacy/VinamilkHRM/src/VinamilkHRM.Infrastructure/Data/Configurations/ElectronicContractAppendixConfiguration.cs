using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Contracts;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class ElectronicContractAppendixConfiguration : IEntityTypeConfiguration<ElectronicContractAppendix>
{
    public void Configure(EntityTypeBuilder<ElectronicContractAppendix> builder)
    {
        builder.ToTable("ElectronicContractAppendices");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AppendixNumber)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(a => a.AppendixNumber)
            .IsUnique();

        builder.Property(a => a.PreviousSalary)
            .HasPrecision(18, 2);

        builder.Property(a => a.NewSalary)
            .HasPrecision(18, 2);

        builder.Property(a => a.PreviousAllowance)
            .HasPrecision(18, 2);

        builder.Property(a => a.NewAllowance)
            .HasPrecision(18, 2);

        builder.Property(a => a.AdjustmentReason)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(a => a.CeoOverrideCode)
            .HasMaxLength(100);

        builder.Property(a => a.DigitalSignatureHash)
            .IsRequired()
            .HasMaxLength(256);

        builder.Property(a => a.SignerName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.SignerTitle)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(a => a.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.LaborContract)
            .WithMany()
            .HasForeignKey(a => a.LaborContractId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
