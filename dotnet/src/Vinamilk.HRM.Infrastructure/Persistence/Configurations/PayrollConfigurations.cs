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
        builder.Property(x => x.CompanySiRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.17m);
        builder.Property(x => x.CompanyHiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyUiRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyUnionFeeRate).HasColumnType("decimal(7,4)");
        builder.Property(x => x.CompanyOccupationalAccidentRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.005m);
        builder.Property(x => x.EmployeeUnionFeeRate).HasColumnType("decimal(7,4)").HasDefaultValue(0m);
        builder.Property(x => x.EmployeeUnionFeeCapMultiplier).HasColumnType("decimal(7,4)").HasDefaultValue(0.10m);
        builder.Property(x => x.SelfDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DependentDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PitNonResidentRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.20m);
        builder.Property(x => x.PitShortTermRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.10m);
        builder.Property(x => x.PitShortTermThreshold).HasColumnType("decimal(18,0)").HasDefaultValue(2_000_000m);
        builder.Property(x => x.OtMultiplier150).HasColumnType("decimal(6,2)").HasDefaultValue(1.50m);
        builder.Property(x => x.OtMultiplier200).HasColumnType("decimal(6,2)").HasDefaultValue(2.00m);
        builder.Property(x => x.OtMultiplier300).HasColumnType("decimal(6,2)").HasDefaultValue(3.00m);
        builder.Property(x => x.OtNightShiftBonusRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.30m);
        builder.Property(x => x.OtNightOvertimeExtraRate).HasColumnType("decimal(7,4)").HasDefaultValue(0.20m);
        builder.Property(x => x.OtTaxableBaseRate).HasColumnType("decimal(7,4)").HasDefaultValue(1.00m);
        builder.Property(x => x.IsOtExcessTaxExempt).HasDefaultValue(true);
        builder.Property(x => x.PayrollVarianceWarningThresholdPercent).HasColumnType("decimal(5,2)").HasDefaultValue(20.00m);
        builder.Property(x => x.MinProbationSalaryRatePercent).HasColumnType("decimal(5,2)").HasDefaultValue(85.00m);
        builder.Property(x => x.SeveranceAllowanceMonthsPerYear).HasColumnType("decimal(6,2)").HasDefaultValue(0.50m);
        builder.Property(x => x.SeveranceMinTenureMonths).HasDefaultValue(12);
        builder.Property(x => x.SeveranceRoundingHalfYearMonths).HasDefaultValue(6);
        builder.Property(x => x.IsSeveranceStatutoryTaxExempt).HasDefaultValue(true);
        builder.Property(x => x.NeedsHrVerification).HasDefaultValue(true);

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
        builder.Property(x => x.CalculationType).HasColumnType("varchar(30)").HasDefaultValue("Formula");
        builder.Property(x => x.DefaultAmount).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.TaxExemptLimit).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxExemptLimitParameterCode).HasColumnType("varchar(50)");
        builder.Property(x => x.ShowOnPayslip).HasDefaultValue(true);
        builder.Property(x => x.FormulaExpression).HasMaxLength(1000);
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.ComponentCode)
            .IsUnique()
            .HasDatabaseName("UQ_SalaryComponents_ComponentCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class SalaryTemplateConfiguration : IEntityTypeConfiguration<SalaryTemplate>
{
    public void Configure(EntityTypeBuilder<SalaryTemplate> builder)
    {
        builder.ConfigureBaseAuditable<SalaryTemplate, int>("SalaryTemplates", "payroll");

        builder.Property(x => x.TemplateCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StaffGroup).HasColumnType("varchar(30)").HasDefaultValue("Office");
        builder.Property(x => x.DefaultPayFrequency).HasColumnType("varchar(30)").HasDefaultValue("Monthly");
        builder.Property(x => x.DefaultProbationSalaryRatePercent).HasColumnType("decimal(5,2)").HasDefaultValue(85.00m);
        builder.Property(x => x.RoundingRule).HasColumnType("varchar(30)").HasDefaultValue("RoundToDong");
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.TemplateCode)
            .IsUnique()
            .HasDatabaseName("UQ_SalaryTemplates_TemplateCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class SalaryTemplateComponentConfiguration : IEntityTypeConfiguration<SalaryTemplateComponent>
{
    public void Configure(EntityTypeBuilder<SalaryTemplateComponent> builder)
    {
        builder.ConfigureBaseAuditable<SalaryTemplateComponent, int>("SalaryTemplateComponents", "payroll");

        builder.Property(x => x.OverrideFormulaExpression).HasMaxLength(1000);
        builder.Property(x => x.DefaultFixedAmount).HasColumnType("decimal(18,0)");

        builder.HasIndex(x => new { x.SalaryTemplateId, x.SalaryComponentId })
            .IsUnique()
            .HasDatabaseName("UQ_SalaryTemplateComponents_Template_Comp")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.SalaryTemplate)
            .WithMany(t => t.Components)
            .HasForeignKey(x => x.SalaryTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryComponent)
            .WithMany(c => c.TemplateComponents)
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeSalaryProfileConfiguration : IEntityTypeConfiguration<EmployeeSalaryProfile>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryProfile> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeSalaryProfile, int>("EmployeeSalaryProfiles", "payroll");

        builder.Property(x => x.BaseSalary).HasColumnType("decimal(18,0)");
        builder.Property(x => x.InsuranceSalary).HasColumnType("decimal(18,0)");
        builder.Property(x => x.PayMode).HasColumnType("varchar(30)").HasDefaultValue("Monthly");
        builder.Property(x => x.UnitRate).HasColumnType("decimal(18,4)").HasDefaultValue(0m);
        builder.Property(x => x.ProbationRatePercent).HasColumnType("decimal(5,2)").HasDefaultValue(100.00m);
        builder.Property(x => x.PositionAllowance).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.OtherAllowance).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.TaxResidencyMode).HasColumnType("varchar(30)").HasDefaultValue("ResidentStandard");
        builder.Property(x => x.Commitment08Number).HasMaxLength(80);
        builder.Property(x => x.TargetNetSalary).HasColumnType("decimal(18,0)");
        builder.Property(x => x.DecisionNumber).HasMaxLength(80);
        builder.Property(x => x.Reason).HasMaxLength(500);

        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom })
            .HasDatabaseName("IX_EmployeeSalaryProfiles_Emp_Effective");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryScale)
            .WithMany()
            .HasForeignKey(x => x.SalaryScaleId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryStep)
            .WithMany()
            .HasForeignKey(x => x.SalaryStepId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryTemplate)
            .WithMany(t => t.EmployeeProfiles)
            .HasForeignKey(x => x.SalaryTemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeSalaryProfileAllowanceConfiguration : IEntityTypeConfiguration<EmployeeSalaryProfileAllowance>
{
    public void Configure(EntityTypeBuilder<EmployeeSalaryProfileAllowance> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeSalaryProfileAllowance, int>("EmployeeSalaryProfileAllowances", "payroll");

        builder.Property(x => x.ComponentCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Notes).HasMaxLength(300);

        builder.HasIndex(x => new { x.EmployeeSalaryProfileId, x.SalaryComponentId })
            .IsUnique()
            .HasDatabaseName("UQ_EmpSalaryProfileAllowances_Profile_Comp")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.EmployeeSalaryProfile)
            .WithMany(p => p.FixedAllowances)
            .HasForeignKey(x => x.EmployeeSalaryProfileId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SalaryComponent)
            .WithMany()
            .HasForeignKey(x => x.SalaryComponentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollPeriodConfiguration : IEntityTypeConfiguration<PayrollPeriod>
{
    public void Configure(EntityTypeBuilder<PayrollPeriod> builder)
    {
        builder.ConfigureBaseAuditable<PayrollPeriod, int>("PayrollPeriods", "payroll");

        builder.Property(x => x.PeriodCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.PeriodType).HasColumnType("varchar(30)").HasDefaultValue("Monthly");
        builder.Property(x => x.DisbursementSequence).HasDefaultValue(1);
        builder.Property(x => x.PeriodTitle).HasMaxLength(200).IsRequired();
        builder.Property(x => x.StandardWorkdays).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Draft");
        builder.Property(x => x.AttendanceWarningMessage).HasMaxLength(1000);
        builder.Property(x => x.LastUnlockReason).HasMaxLength(500);

        builder.HasIndex(x => x.PeriodCode)
            .IsUnique()
            .HasDatabaseName("UQ_PayrollPeriods_PeriodCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.Year, x.Month, x.PeriodType, x.DisbursementSequence })
            .IsUnique()
            .HasDatabaseName("UQ_PayrollPeriods_Year_Month_Type_Seq")
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
        builder.Property(x => x.OtHours150).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OtHours200).HasColumnType("decimal(5,2)");
        builder.Property(x => x.OtHours300).HasColumnType("decimal(5,2)");
        builder.Property(x => x.TotalOtHours).HasColumnType("decimal(5,2)");
        builder.Property(x => x.ContractBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.ActualBaseSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PositionAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtherAllowance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OvertimePay).HasColumnType("decimal(18,2)");
        builder.Property(x => x.OtNightExtraPay).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.OtTaxExemptAmount).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.KpiBonus).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CommissionBonus).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.ThirteenthMonthBonus).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.RetroEarningAmount).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.UnusedLeavePayout).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.GrossIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.InsuranceSalaryBase).HasColumnType("decimal(18,2)");
        builder.Property(x => x.UnemploymentInsuranceBase).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeSocialInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeHealthInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeUnemploymentInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalEmployeeInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.EmployeeUnionFee).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.CompanySocialInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyHealthInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyUnemploymentInsurance).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CompanyOccupationalAccidentInsurance).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.CompanyUnionFee).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalCompanyCost).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.TaxResidencyMode).HasColumnType("varchar(30)").HasDefaultValue("ResidentStandard");
        builder.Property(x => x.TargetNetSalary).HasColumnType("decimal(18,0)");
        builder.Property(x => x.SelfDeductionAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.DependentDeductionAmount).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TaxableIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.AssessableIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PersonalIncomeTax).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PitBracketsJson).HasMaxLength(2000);
        builder.Property(x => x.DisciplineDeduction).HasColumnType("decimal(18,2)");
        builder.Property(x => x.SalaryAdvanceDeduction).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.LoanInstallmentDeduction).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.RetroDeductionAmount).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.NetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PreviousRevisionNetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.PreviousPeriodNetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.VariancePercentVsPreviousPeriod).HasColumnType("decimal(7,2)");
        builder.Property(x => x.PaymentStatus).HasColumnType("varchar(30)").HasDefaultValue("Paid");
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.CalculationTraceJson).HasColumnType("nvarchar(max)");

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

public class PayrollCalculationRevisionConfiguration : IEntityTypeConfiguration<PayrollCalculationRevision>
{
    public void Configure(EntityTypeBuilder<PayrollCalculationRevision> builder)
    {
        builder.ConfigureBaseAuditable<PayrollCalculationRevision, long>("PayrollCalculationRevisions", "payroll");

        builder.Property(x => x.Scope).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.TotalGrossIncome).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalPersonalIncomeTax).HasColumnType("decimal(18,2)");
        builder.Property(x => x.TotalNetSalary).HasColumnType("decimal(18,2)");
        builder.Property(x => x.NetDeltaFromPreviousRevision).HasColumnType("decimal(18,2)");
        builder.Property(x => x.CalculatedByUsername).HasMaxLength(100);
        builder.Property(x => x.Notes).HasMaxLength(500);

        builder.HasIndex(x => new { x.PayrollPeriodId, x.RevisionNumber })
            .HasDatabaseName("IX_PayrollCalculationRevisions_Period_Rev");

        builder.HasOne(x => x.PayrollPeriod)
            .WithMany(p => p.CalculationRevisions)
            .HasForeignKey(x => x.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayslipDispatchLogConfiguration : IEntityTypeConfiguration<PayslipDispatchLog>
{
    public void Configure(EntityTypeBuilder<PayslipDispatchLog> builder)
    {
        builder.ConfigureBaseAuditable<PayslipDispatchLog, long>("PayslipDispatchLogs", "payroll");

        builder.Property(x => x.EmployeeCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.EmployeeName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RecipientEmail).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Queued");
        builder.Property(x => x.ErrorMessage).HasMaxLength(1000);
        builder.Property(x => x.HangfireJobId).HasColumnType("varchar(100)");

        builder.HasIndex(x => new { x.PayrollPeriodId, x.BatchId })
            .HasDatabaseName("IX_PayslipDispatchLogs_Period_Batch");

        builder.HasOne(x => x.PayrollPeriod)
            .WithMany(p => p.PayslipDispatchLogs)
            .HasForeignKey(x => x.PayrollPeriodId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SalaryAdvanceConfiguration : IEntityTypeConfiguration<SalaryAdvance>
{
    public void Configure(EntityTypeBuilder<SalaryAdvance> builder)
    {
        builder.ConfigureBaseAuditable<SalaryAdvance, int>("SalaryAdvances", "payroll");

        builder.Property(x => x.AdvanceNumber).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.MaxAllowedAmountSnapshot).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.ApproverComment).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => new { x.EmployeeId, x.TargetYear, x.TargetMonth })
            .HasDatabaseName("IX_SalaryAdvances_Emp_Period");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeLoanConfiguration : IEntityTypeConfiguration<EmployeeLoan>
{
    public void Configure(EntityTypeBuilder<EmployeeLoan> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeLoan, int>("EmployeeLoans", "payroll");

        builder.Property(x => x.LoanCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.LoanType).HasColumnType("varchar(30)").HasDefaultValue("Loan");
        builder.Property(x => x.PrincipalAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.MonthlyInstallmentAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Active");

        builder.HasIndex(x => x.LoanCode)
            .IsUnique()
            .HasDatabaseName("UQ_EmployeeLoans_LoanCode")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeLoanInstallmentConfiguration : IEntityTypeConfiguration<EmployeeLoanInstallment>
{
    public void Configure(EntityTypeBuilder<EmployeeLoanInstallment> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeLoanInstallment, int>("EmployeeLoanInstallments", "payroll");

        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Scheduled");

        builder.HasIndex(x => new { x.EmployeeId, x.TargetYear, x.TargetMonth })
            .HasDatabaseName("IX_EmployeeLoanInstallments_Emp_Period");

        builder.HasOne(x => x.EmployeeLoan)
            .WithMany(l => l.Installments)
            .HasForeignKey(x => x.EmployeeLoanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollAdjustmentConfiguration : IEntityTypeConfiguration<PayrollAdjustment>
{
    public void Configure(EntityTypeBuilder<PayrollAdjustment> builder)
    {
        builder.ConfigureBaseAuditable<PayrollAdjustment, int>("PayrollAdjustments", "payroll");

        builder.Property(x => x.AdjustmentCode).HasColumnType("varchar(50)").HasDefaultValue("");
        builder.Property(x => x.AdjustmentType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.Amount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.SourcePeriodTaxDelta).HasColumnType("decimal(18,0)").HasDefaultValue(0m);
        builder.Property(x => x.TaxCalculationNote).HasMaxLength(500);
        builder.Property(x => x.Reason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Approved");

        builder.HasIndex(x => new { x.EmployeeId, x.TargetYear, x.TargetMonth })
            .HasDatabaseName("IX_PayrollAdjustments_Emp_Period");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollPolicyConfiguration : IEntityTypeConfiguration<PayrollPolicy>
{
    public void Configure(EntityTypeBuilder<PayrollPolicy> builder)
    {
        builder.ConfigureBaseAuditable<PayrollPolicy, int>("PayrollPolicies", "payroll");

        builder.Property(x => x.MaxAdvanceLimitPercent).HasColumnType("decimal(5,2)").HasDefaultValue(50.00m);
        builder.Property(x => x.AdvanceMinTenureDays).HasDefaultValue(30);
        builder.Property(x => x.AdvanceCutoffDayOfMonth).HasDefaultValue(25);
        builder.Property(x => x.SettleLoansOnTermination).HasDefaultValue(true);
        builder.Property(x => x.RetroTaxMode).HasColumnType("varchar(40)").HasDefaultValue("RecalculateSourcePeriod");
        builder.Property(x => x.ExtraSeveranceMonthsPerYear).HasColumnType("decimal(6,2)").HasDefaultValue(0m);
        builder.Property(x => x.PayMidMonthAdvanceWithoutPit).HasDefaultValue(true);
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.Property(x => x.NeedsHrVerification).HasDefaultValue(true);

        builder.HasIndex(x => x.PolicyGroupId)
            .IsUnique()
            .HasDatabaseName("UQ_PayrollPolicies_PolicyGroupId")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.PolicyGroup)
            .WithMany(g => g.PayrollPolicies)
            .HasForeignKey(x => x.PolicyGroupId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class EmployeeTerminationSettlementConfiguration : IEntityTypeConfiguration<EmployeeTerminationSettlement>
{
    public void Configure(EntityTypeBuilder<EmployeeTerminationSettlement> builder)
    {
        builder.ConfigureBaseAuditable<EmployeeTerminationSettlement, int>("EmployeeTerminationSettlements", "payroll");

        builder.Property(x => x.SettlementCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.TerminationType).HasColumnType("varchar(40)").HasDefaultValue("Resignation");
        builder.Property(x => x.StandardWorkdaysUsed).HasColumnType("decimal(5,2)").HasDefaultValue(26m);
        builder.Property(x => x.WorkedDaysInFinalMonth).HasColumnType("decimal(5,2)");
        builder.Property(x => x.BaseSalaryUsed).HasColumnType("decimal(18,0)");
        builder.Property(x => x.PositionAllowanceUsed).HasColumnType("decimal(18,0)");
        builder.Property(x => x.FinalMonthSalaryAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.AverageSalaryLast6Months).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TotalTenureMonths).HasColumnType("decimal(6,2)");
        builder.Property(x => x.UnemploymentInsuranceCoveredMonths).HasColumnType("decimal(6,2)");
        builder.Property(x => x.EligibleSeveranceYears).HasColumnType("decimal(6,2)");
        builder.Property(x => x.StatutorySeveranceRatePerYear).HasColumnType("decimal(6,2)");
        builder.Property(x => x.CompanyExtraSeveranceRatePerYear).HasColumnType("decimal(6,2)");
        builder.Property(x => x.StatutorySeveranceAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.CompanyExtraSeveranceAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.UnusedLeaveDays).HasColumnType("decimal(6,2)");
        builder.Property(x => x.UnusedLeavePayoutAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.OtherBonusOrRetroAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TotalGrossSettlement).HasColumnType("decimal(18,0)");
        builder.Property(x => x.EmployeeInsuranceDeduction).HasColumnType("decimal(18,0)");
        builder.Property(x => x.PersonalIncomeTaxDeduction).HasColumnType("decimal(18,0)");
        builder.Property(x => x.SalaryAdvanceDeduction).HasColumnType("decimal(18,0)");
        builder.Property(x => x.LoanPayoffDeduction).HasColumnType("decimal(18,0)");
        builder.Property(x => x.OtherDeduction).HasColumnType("decimal(18,0)");
        builder.Property(x => x.NetSettlementAmount).HasColumnType("decimal(18,0)");
        builder.Property(x => x.RemainingDebtToRecover).HasColumnType("decimal(18,0)");
        builder.Property(x => x.CalculationBreakdownJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Draft");
        builder.Property(x => x.Notes).HasMaxLength(1000);

        builder.HasIndex(x => x.SettlementCode)
            .IsUnique()
            .HasDatabaseName("UQ_EmployeeTerminationSettlements_Code")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.EmployeeId, x.TargetPayrollYear, x.TargetPayrollMonth })
            .HasDatabaseName("IX_EmployeeTerminationSettlements_Emp_Period");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PayrollAccountingAccountConfigConfiguration : IEntityTypeConfiguration<PayrollAccountingAccountConfig>
{
    public void Configure(EntityTypeBuilder<PayrollAccountingAccountConfig> builder)
    {
        builder.ConfigureBaseAuditable<PayrollAccountingAccountConfig, int>("PayrollAccountingAccountConfigs", "payroll");

        builder.Property(x => x.AccountKey).HasColumnType("varchar(60)").IsRequired();
        builder.Property(x => x.AccountCode).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.AccountName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.NormalSide).HasColumnType("varchar(10)").HasDefaultValue("Credit");
        builder.Property(x => x.CostCenterPattern).HasColumnType("varchar(50)");
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.NeedsHrVerification).HasDefaultValue(true);

        builder.HasIndex(x => new { x.AccountKey, x.EffectiveFrom })
            .HasDatabaseName("IX_PayrollAccountingConfigs_Key_Eff");
    }
}

public class SocialInsuranceReportTemplateConfiguration : IEntityTypeConfiguration<SocialInsuranceReportTemplate>
{
    public void Configure(EntityTypeBuilder<SocialInsuranceReportTemplate> builder)
    {
        builder.ConfigureBaseAuditable<SocialInsuranceReportTemplate, int>("SocialInsuranceReportTemplates", "payroll");

        builder.Property(x => x.TemplateCode).HasColumnType("varchar(40)").IsRequired();
        builder.Property(x => x.TemplateName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.SoftwareVendor).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ColumnMappingsJson).HasColumnType("nvarchar(max)").IsRequired();
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.NeedsHrVerification).HasDefaultValue(true);

        builder.HasIndex(x => x.TemplateCode)
            .IsUnique()
            .HasDatabaseName("UQ_SocialInsuranceTemplates_Code")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class PitWithholdingCertificateConfiguration : IEntityTypeConfiguration<PitWithholdingCertificate>
{
    public void Configure(EntityTypeBuilder<PitWithholdingCertificate> builder)
    {
        builder.ConfigureBaseAuditable<PitWithholdingCertificate, int>("PitWithholdingCertificates", "payroll");

        builder.Property(x => x.CertificateCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.GrossIncome).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TotalInsuranceDeducted).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TotalFamilyDeductions).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TaxableIncome).HasColumnType("decimal(18,0)");
        builder.Property(x => x.AssessableIncome).HasColumnType("decimal(18,0)");
        builder.Property(x => x.TotalPitWithheld).HasColumnType("decimal(18,0)");
        builder.Property(x => x.AnnualFinalTaxLiability).HasColumnType("decimal(18,0)");
        builder.Property(x => x.PitOverpaidOrUnderpaid).HasColumnType("decimal(18,0)");
        builder.Property(x => x.IssuedByUsername).HasMaxLength(100).IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("Issued");
        builder.Property(x => x.LegalReference).HasMaxLength(300);
        builder.Property(x => x.NeedsHrVerification).HasDefaultValue(true);

        builder.HasIndex(x => x.CertificateCode)
            .IsUnique()
            .HasDatabaseName("UQ_PitCertificates_Code")
            .HasFilter("[IsDeleted] = 0");

        builder.HasIndex(x => new { x.TaxYear, x.EmployeeId })
            .HasDatabaseName("IX_PitCertificates_Year_Emp");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

