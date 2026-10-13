using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class PaySlipConfiguration : IEntityTypeConfiguration<PaySlip>
{
    public void Configure(EntityTypeBuilder<PaySlip> builder)
    {
        builder.ToTable("PaySlips", b => b.IsTemporal());

        builder.HasKey(p => p.Id);

        builder.Property(p => p.PayrollPeriod)
            .IsRequired()
            .HasMaxLength(20);

        builder.HasIndex(p => p.PayrollPeriod);

        builder.HasIndex(p => new { p.EmployeeId, p.PayrollPeriod })
            .IsUnique();

        builder.Property(p => p.StandardWorkDays).HasPrecision(5, 2);
        builder.Property(p => p.ActualWorkDays).HasPrecision(5, 2);
        builder.Property(p => p.OvertimeHours).HasPrecision(5, 2);
        builder.Property(p => p.NightShiftHours).HasPrecision(5, 2);

        // Toàn bộ các cột tiền tệ kiểu decimal(18,0) theo quy chuẩn dự án Vinamilk
        builder.Property(p => p.BaseSalary).HasColumnType("decimal(18,0)");
        builder.Property(p => p.Allowance).HasColumnType("decimal(18,0)");
        builder.Property(p => p.LunchAllowance).HasColumnType("decimal(18,0)");
        builder.Property(p => p.OvertimePay).HasColumnType("decimal(18,0)");
        builder.Property(p => p.NightShiftPay).HasColumnType("decimal(18,0)");
        builder.Property(p => p.HazardousAllowance).HasColumnType("decimal(18,0)");
        builder.Property(p => p.SalesCommission).HasColumnType("decimal(18,0)");
        builder.Property(p => p.KpiBonus).HasColumnType("decimal(18,0)");
        builder.Property(p => p.OtherBonuses).HasColumnType("decimal(18,0)");
        builder.Property(p => p.GrossSalary).HasColumnType("decimal(18,0)");
        builder.Property(p => p.SocialInsuranceDeduction).HasColumnType("decimal(18,0)");
        builder.Property(p => p.HealthInsuranceDeduction).HasColumnType("decimal(18,0)");
        builder.Property(p => p.UnemploymentInsuranceDeduction).HasColumnType("decimal(18,0)");
        builder.Property(p => p.PersonalDeduction).HasColumnType("decimal(18,0)");
        builder.Property(p => p.DependentDeduction).HasColumnType("decimal(18,0)");
        builder.Property(p => p.TaxableIncome).HasColumnType("decimal(18,0)");
        builder.Property(p => p.PersonalIncomeTax).HasColumnType("decimal(18,0)");
        builder.Property(p => p.TotalDeductions).HasColumnType("decimal(18,0)");
        builder.Property(p => p.NetSalary).HasColumnType("decimal(18,0)");

        builder.Property(p => p.TransactionReference)
            .HasMaxLength(100);

        builder.Property(p => p.IsImported)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(p => p.LegacyId);
        builder.HasIndex(p => p.LegacyId);

        builder.Property(p => p.CreatedBy)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(p => p.EmployerCost).HasColumnType("decimal(18,0)");
        builder.Property(p => p.FormulaTraceJson).HasMaxLength(4000);

        builder.HasOne(p => p.PayrollRun)
            .WithMany(r => r.PaySlips)
            .HasForeignKey(p => p.PayrollRunId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.Employee)
            .WithMany(e => e.PaySlips)
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

