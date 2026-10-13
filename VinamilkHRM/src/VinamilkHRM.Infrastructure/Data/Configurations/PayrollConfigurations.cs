using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> builder)
    {
        builder.ToTable("PayrollRuns", b => b.IsTemporal());

        builder.HasKey(r => r.Id);

        builder.Property(r => r.Period)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(r => r.Period);

        builder.Property(r => r.TotalGross).HasColumnType("decimal(18,0)");
        builder.Property(r => r.TotalNet).HasColumnType("decimal(18,0)");
        builder.Property(r => r.TotalEmployerCost).HasColumnType("decimal(18,0)");
        builder.Property(r => r.TotalPersonalIncomeTax).HasColumnType("decimal(18,0)");
        builder.Property(r => r.TotalEmployeeInsurance).HasColumnType("decimal(18,0)");
        builder.Property(r => r.TotalEmployerInsurance).HasColumnType("decimal(18,0)");

        builder.Property(r => r.PaymentReference).HasMaxLength(100);

        builder.HasOne(r => r.OrganizationalUnit)
            .WithMany()
            .HasForeignKey(r => r.OrganizationalUnitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(r => r.SubmittedByEmployee)
            .WithMany()
            .HasForeignKey(r => r.SubmittedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ApprovedByChiefAccountantEmployee)
            .WithMany()
            .HasForeignKey(r => r.ApprovedByChiefAccountant)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ApprovedByHrDirectorEmployee)
            .WithMany()
            .HasForeignKey(r => r.ApprovedByHrDirector)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.LockedByEmployee)
            .WithMany()
            .HasForeignKey(r => r.LockedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.PaidByEmployee)
            .WithMany()
            .HasForeignKey(r => r.PaidBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(r => r.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(r => r.UpdatedBy).HasMaxLength(100);
    }
}

public class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> builder)
    {
        builder.ToTable("PayrollAdjustments", b => b.IsTemporal());

        builder.HasKey(a => a.Id);

        builder.Property(a => a.SourcePeriod)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(a => a.TargetPeriod)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(a => a.TargetPeriod);

        builder.Property(a => a.AdjustmentType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(a => a.Amount).HasColumnType("decimal(18,0)");

        builder.Property(a => a.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(a => a.Employee)
            .WithMany()
            .HasForeignKey(a => a.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ApprovedByEmployee)
            .WithMany()
            .HasForeignKey(a => a.ApprovedBy)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(a => a.CreatedBy).IsRequired().HasMaxLength(100);
        builder.Property(a => a.UpdatedBy).HasMaxLength(100);
    }
}
