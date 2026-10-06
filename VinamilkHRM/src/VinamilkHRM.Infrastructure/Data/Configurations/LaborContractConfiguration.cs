using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class LaborContractConfiguration : IEntityTypeConfiguration<LaborContract>
{
    public void Configure(EntityTypeBuilder<LaborContract> builder)
    {
        builder.ToTable("LaborContracts");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.ContractNumber)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasIndex(c => c.ContractNumber)
            .IsUnique();

        builder.Property(c => c.BasicSalary)
            .HasPrecision(18, 2);

        builder.Property(c => c.InsuranceSalary)
            .HasPrecision(18, 2);

        builder.Property(c => c.TerminationReason)
            .HasMaxLength(500);

        builder.Property(c => c.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(c => c.Employee)
            .WithMany(e => e.LaborContracts)
            .HasForeignKey(c => c.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        // TPH Discriminator Mapping
        builder.HasDiscriminator<ContractType>("ContractType")
            .HasValue<ProbationContract>(ContractType.Probation)
            .HasValue<DefiniteContract>(ContractType.Definite)
            .HasValue<IndefiniteContract>(ContractType.Indefinite);
    }
}
