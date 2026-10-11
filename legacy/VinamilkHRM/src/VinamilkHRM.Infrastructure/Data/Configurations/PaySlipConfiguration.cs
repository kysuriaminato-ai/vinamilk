using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class PaySlipConfiguration : IEntityTypeConfiguration<PaySlip>
{
    public void Configure(EntityTypeBuilder<PaySlip> builder)
    {
        builder.ToTable("PaySlips");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PayrollPeriod)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.PayrollPeriod);

        builder.HasIndex(p => new { p.EmployeeId, p.PayrollPeriod })
            .IsUnique();

        builder.Property(p => p.StandardWorkDays).HasPrecision(5, 2);
        builder.Property(p => p.ActualWorkDays).HasPrecision(5, 2);
        builder.Property(p => p.BaseSalary).HasPrecision(18, 2);
        builder.Property(p => p.Allowance).HasPrecision(18, 2);
        builder.Property(p => p.LunchAllowance).HasPrecision(18, 2);
        builder.Property(p => p.OvertimeHours).HasPrecision(5, 2);
        builder.Property(p => p.OvertimePay).HasPrecision(18, 2);
        builder.Property(p => p.NightShiftHours).HasPrecision(5, 2);
        builder.Property(p => p.NightShiftPay).HasPrecision(18, 2);
        builder.Property(p => p.HazardousAllowance).HasPrecision(18, 2);
        builder.Property(p => p.SalesCommission).HasPrecision(18, 2);
        builder.Property(p => p.KpiBonus).HasPrecision(18, 2);
        builder.Property(p => p.OtherBonuses).HasPrecision(18, 2);
        builder.Property(p => p.GrossSalary).HasPrecision(18, 2);
        builder.Property(p => p.SocialInsuranceDeduction).HasPrecision(18, 2);
        builder.Property(p => p.HealthInsuranceDeduction).HasPrecision(18, 2);
        builder.Property(p => p.UnemploymentInsuranceDeduction).HasPrecision(18, 2);
        builder.Property(p => p.PersonalDeduction).HasPrecision(18, 2);
        builder.Property(p => p.DependentDeduction).HasPrecision(18, 2);
        builder.Property(p => p.TaxableIncome).HasPrecision(18, 2);
        builder.Property(p => p.PersonalIncomeTax).HasPrecision(18, 2);
        builder.Property(p => p.TotalDeductions).HasPrecision(18, 2);
        builder.Property(p => p.NetSalary).HasPrecision(18, 2);

        builder.Property(p => p.TransactionReference)
            .HasMaxLength(100);

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);

        builder.HasOne(p => p.Employee)
            .WithMany(e => e.PaySlips)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
