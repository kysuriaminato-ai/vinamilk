using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Statutory;

namespace VinamilkHRM.Infrastructure.Data.Configurations;

public class StatutoryParameterConfiguration : IEntityTypeConfiguration<StatutoryParameter>
{
    public void Configure(EntityTypeBuilder<StatutoryParameter> builder)
    {
        builder.ToTable("StatutoryParameters");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Code)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(p => p.Value)
            .HasColumnType("decimal(18,0)");

        builder.Property(p => p.LegalReference)
            .HasMaxLength(250);

        builder.Property(p => p.Description)
            .HasMaxLength(500);

        builder.HasIndex(p => new { p.Code, p.EffectiveFrom });
    }
}

public class MinimumWageConfiguration : IEntityTypeConfiguration<MinimumWage>
{
    public void Configure(EntityTypeBuilder<MinimumWage> builder)
    {
        builder.ToTable("MinimumWages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.MonthlyAmount)
            .HasColumnType("decimal(18,0)");

        builder.Property(m => m.HourlyAmount)
            .HasColumnType("decimal(18,0)");

        builder.Property(m => m.LegalReference)
            .HasMaxLength(250);

        builder.HasIndex(m => new { m.Region, m.EffectiveFrom });
    }
}

public class TaxBracketConfiguration : IEntityTypeConfiguration<TaxBracket>
{
    public void Configure(EntityTypeBuilder<TaxBracket> builder)
    {
        builder.ToTable("TaxBrackets");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.ScheduleCode)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(t => t.UpperBoundMonthly)
            .HasColumnType("decimal(18,0)");

        builder.Property(t => t.Rate)
            .HasColumnType("decimal(5,4)");

        builder.Property(t => t.Description)
            .HasMaxLength(250);

        builder.HasIndex(t => new { t.ScheduleCode, t.Order, t.EffectiveFrom });
    }
}

public class InsuranceRateConfiguration : IEntityTypeConfiguration<InsuranceRate>
{
    public void Configure(EntityTypeBuilder<InsuranceRate> builder)
    {
        builder.ToTable("InsuranceRates");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.EmployeeRate)
            .HasColumnType("decimal(5,4)");

        builder.Property(i => i.EmployerRate)
            .HasColumnType("decimal(5,4)");

        builder.Property(i => i.LegalReference)
            .HasMaxLength(250);

        builder.HasIndex(i => new { i.Kind, i.EffectiveFrom });
    }
}

public class OvertimeRateConfiguration : IEntityTypeConfiguration<OvertimeRate>
{
    public void Configure(EntityTypeBuilder<OvertimeRate> builder)
    {
        builder.ToTable("OvertimeRates");
        builder.HasKey(o => o.Id);

        builder.Property(o => o.Multiplier)
            .HasColumnType("decimal(4,2)");

        builder.HasIndex(o => new { o.DayKind, o.Night, o.EffectiveFrom });
    }
}

public class PublicHolidayConfiguration : IEntityTypeConfiguration<PublicHoliday>
{
    public void Configure(EntityTypeBuilder<PublicHoliday> builder)
    {
        builder.ToTable("PublicHolidays");
        builder.HasKey(h => h.Id);

        builder.Property(h => h.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(h => h.Date);
        builder.HasIndex(h => h.Year);
    }
}
