using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Assets;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class CompanyAssetConfiguration : IEntityTypeConfiguration<CompanyAsset>
{
    public void Configure(EntityTypeBuilder<CompanyAsset> builder)
    {
        builder.ToTable("CompanyAssets");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.AssetCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(a => a.AssetCode)
            .IsUnique();

        builder.Property(a => a.AssetName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(a => a.Category)
            .HasMaxLength(100);

        builder.Property(a => a.SerialNumber)
            .HasMaxLength(100);

        builder.Property(a => a.OriginalValue)
            .HasPrecision(18, 2);

        builder.Property(a => a.Notes)
            .HasMaxLength(500);

        builder.Property(a => a.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(a => a.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(a => a.AssignedToEmployee)
            .WithMany(e => e.AssignedAssets)
            .HasForeignKey(a => a.AssignedToEmployeeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
