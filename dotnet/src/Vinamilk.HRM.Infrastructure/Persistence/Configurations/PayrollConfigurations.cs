using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.Payroll;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class PayrollStatutoryParameterConfiguration : IEntityTypeConfiguration<PayrollStatutoryParameter>
{
    public void Configure(EntityTypeBuilder<PayrollStatutoryParameter> builder)
    {
        builder.ConfigureBaseAuditable<PayrollStatutoryParameter, int>("PayrollStatutoryParameters", "payroll");

        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.StatutoryBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MinWageRegion1).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MinWageRegion2).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MinWageRegion3).HasColumnType("decimal(18,2)");
        builder.Property(x => x.MinWageRegion4).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SocialInsuranceCapMultiplier).HasColumnType("decimal(6,2)");
        builder.Property(x => x.UnemploymentInsuranceCapMultiplier).HasColumnType("decimal(6,2)");
        builder.Property(x => x.EmployeeSiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.EmployeeHiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.EmployeeUiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanySiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyHiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyUiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyUnionFeeRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.SelfDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DependentDeduction).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => new { x.EffectiveFrom, x.EffectiveTo })
            .HasDatabaseName("IX_PayrollStatutoryParameters_EffectivePeriod");
    }
}

public class PitBracketConfiguration : IEntityTypeConfiguration<PitBracket>
{
    public void Configure(EntityTypeBuilder<PitBracket> builder)
    {
        builder.ConfigureBaseAuditable<PitBracket, int>("PitBrackets", "payroll");

        builder.Property(x => x.LowerBound).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UpperBound).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.LegalReference).HasMaxLength(300);

        builder.HasIndex(x => new { x.EffectiveFrom, x.BracketLevel })
            .IsUnique()
            .HasDatabaseName("UQ_PitBrackets_EffectiveFrom_Level")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PayrollParameterConfiguration : IEntityTypeConfiguration<PayrollParameter>
{
    public void Configure(EntityTypeBuilder<PayrollParameter> builder)
    {
        builder.ConfigureBaseAuditable<PayrollParameter, int>("PayrollParameters", "payroll");

        builder.Property(x => x.ParamCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ParamName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.ParamValue).HasColumnType("decimal(18,4)");
        builder.Property(x => x.ValueUnit).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.LegalReference).HasMaxLength(300);

        builder.HasIndex(x => x.ParamCode)
            .IsUnique()
            .HasDatabaseName("UQ_PayrollParameters_ParamCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class TaxBracketConfiguration : IEntityTypeConfiguration<TaxBracket>
{
    public void Configure(EntityTypeBuilder<TaxBracket> builder)
    {
        builder.ConfigureBaseAuditable<TaxBracket, int>("TaxBrackets", "payroll");

        builder.Property(x => x.FromIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ToIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxRatePercent).HasColumnType("decimal(5,2)");
        builder.Property(x => x.QuickSubtractionAmount).HasColumnType("decimal(18,2)");

        builder.HasIndex(x => x.BracketLevel)
            .IsUnique()
            .HasDatabaseName("UQ_TaxBrackets_BracketLevel")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class SalaryComponentConfiguration : IEntityTypeConfiguration<SalaryComponent>
{
    public void Configure(EntityTypeBuilder<SalaryComponent> builder)
    {
        builder.ConfigureBaseAuditable<SalaryComponent, int>("SalaryComponents", "payroll");

        builder.Property(x => x.ComponentCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ComponentName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ComponentType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.TaxExemptLimit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.FormulaExpression).HasMaxLength(500);

        builder.HasIndex(x => x.ComponentCode)
            .IsUnique()
            .HasDatabaseName("UQ_SalaryComponents_ComponentCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ConfigureBaseAuditable<PayrollPeriod, int>("PayrollPeriods", "payroll");

        builder.Property(x => x.PeriodCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.PeriodTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StandardWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Draft");

        builder.HasIndex(x => x.PeriodCode)
            .IsUnique()
            .HasDatabaseName("UQ_PayrollPeriods_PeriodCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.Year, x.Month })
            .IsUnique()
            .HasDatabaseName("UQ_PayrollPeriods_Year_Month")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PayrollRecordConfiguration : IEntityTypeConfiguration<PayrollRecord>
{
    public void Configure(EntityTypeBuilder<PayrollRecord> builder)
    {
        builder.ConfigureBaseAuditable<PayrollRecord, long>("PayrollRecords", "payroll");

        builder.Property(x => x.StandardWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.PayableWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ContractBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PositionAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OvertimePay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.KpiBonus).HasColumnType("decimal(18,2)");
        builder.Property(x => x.GrossIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.InsuranceSalaryBase).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeSocialInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeHealthInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeUnemploymentInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalEmployeeInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanySocialInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyHealthInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyUnemploymentInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyUnionFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SelfDeductionAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DependentDeductionAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxableIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AssessableIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PersonalIncomeTax).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DisciplineDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PaymentStatus).HasColumnType("varchar(30)").HasDefaultValue("Paid");
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.PayrollPeriodId, x.EmployeeId })
            .IsUnique()
            .HasDatabaseName("UQ_PayrollRecords_Period_Employee")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.OrganizationUnitId, x.PayrollPeriodId })
            .HasDatabaseName("IX_PayrollRecords_Unit_Period");

        builder.HasOne(x => x.PayrollPeriod)
            .WithMany(p => p.PayrollRecords)
            .HasForeignKey(x => x.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany(e => e.PayrollRecords)
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Position)
            .WithMany()
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.MonthlyAttendanceSummary)
            .WithMany()
            .HasForeignKey(x => x.MonthlyAttendanceSummaryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollRecordDetailConfiguration : IEntityTypeConfiguration<PayrollRecordDetail>
{
    public void Configure(EntityTypeBuilder<PayrollRecordDetail> builder)
    {
        builder.ConfigureBaseAuditable<PayrollRecordDetail, long>("PayrollRecordDetails", "payroll");

        builder.Property(x => x.ComponentCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ComponentName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CalculationNote).HasMaxLength(300);

        builder.HasIndex(x => x.PayrollRecordId).HasDatabaseName("IX_PayrollRecordDetails_PayrollRecordId");

        builder.HasOne(x => x.PayrollRecord)
            .WithMany(r => r.Details)
            .HasForeignKey(x => x.PayrollRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryComponent)
            .WithMany(c => c.RecordDetails)
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

