using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class OrganizationalUnitConfiguration : IEntityTypeConfiguration<OrganizationalUnit>
{
    public void Configure(EntityTypeBuilder<OrganizationalUnit> builder)
    {
        builder.ToTable("OrganizationalUnits");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.UnitCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(u => u.UnitCode)
            .IsUnique();

        builder.Property(u => u.UnitName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.UnitType)
            .IsRequired();

        builder.Property(u => u.Address)
            .HasMaxLength(500);

        builder.Property(u => u.PhoneNumber)
            .HasMaxLength(50);

        builder.Property(u => u.Email)
            .HasMaxLength(150);

        builder.Property(u => u.TaxCode)
            .HasMaxLength(50);

        builder.Property(u => u.RegionId)
            .IsRequired()
            .HasDefaultValue(3);

        builder.Property(u => u.RegionName)
            .IsRequired()
            .HasMaxLength(100)
            .HasDefaultValue("Miền Nam");

        builder.Property(u => u.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.UpdatedBy)
            .HasMaxLength(100);

        // Self-referencing Parent Unit
        builder.HasOne(u => u.ParentUnit)
            .WithMany(u => u.ChildUnits)
            .HasForeignKey(u => u.ParentUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        // Manager Employee
        builder.HasOne(u => u.ManagerEmployee)
            .WithMany()
            .HasForeignKey(u => u.ManagerEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // TPH Discriminator Mapping
        builder.HasDiscriminator<UnitType>("UnitType")
            .HasValue<HeadquartersUnit>(UnitType.Headquarters)
            .HasValue<FactoryUnit>(UnitType.Factory)
            .HasValue<DairyFarmUnit>(UnitType.DairyFarm)
            .HasValue<RetailStoreUnit>(UnitType.RetailStore);
    }
}
