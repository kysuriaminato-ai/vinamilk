using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.OnboardingOffboarding;

namespace VinamilkHRM.Infrastructure.Data.Configurations.OnboardingOffboarding;

public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.ToTable("ChecklistTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasMany(t => t.Items)
            .WithOne(i => i.ChecklistTemplate)
            .HasForeignKey(i => i.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateItem> builder)
    {
        builder.ToTable("ChecklistTemplateItems");
        builder.HasKey(i => i.Id);

        builder.Property(i => i.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(i => i.AssignedRole)
            .IsRequired()
            .HasMaxLength(50);
    }
}

public class EmployeeChecklistTaskConfiguration : IEntityTypeConfiguration<EmployeeChecklistTask>
{
    public void Configure(EntityTypeBuilder<EmployeeChecklistTask> builder)
    {
        builder.ToTable("EmployeeChecklistTasks");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.AssignedRole)
            .IsRequired()
            .HasMaxLength(50);

        builder.HasOne(t => t.Employee)
            .WithMany()
            .HasForeignKey(t => t.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(t => new { t.EmployeeId, t.ChecklistType, t.IsCompleted });
    }
}

public class SocialInsuranceDeclarationConfiguration : IEntityTypeConfiguration<SocialInsuranceDeclaration>
{
    public void Configure(EntityTypeBuilder<SocialInsuranceDeclaration> builder)
    {
        builder.ToTable("SocialInsuranceDeclarations");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.DeclarationNumber)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(d => d.DeclarationNumber).IsUnique();

        builder.Property(d => d.EffectiveMonth)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(d => d.InsuranceSalary)
            .HasPrecision(18, 0);

        builder.HasOne(d => d.Employee)
            .WithMany()
            .HasForeignKey(d => d.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class FinalSettlementConfiguration : IEntityTypeConfiguration<FinalSettlement>
{
    public void Configure(EntityTypeBuilder<FinalSettlement> builder)
    {
        builder.ToTable("FinalSettlements");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.WorkingDaysWorkedInMonth)
            .HasPrecision(5, 2);

        builder.Property(s => s.DailySalaryRate)
            .HasPrecision(18, 0);

        builder.Property(s => s.EarnedSalary)
            .HasPrecision(18, 0);

        builder.Property(s => s.RemainingAnnualLeaveDays)
            .HasPrecision(5, 2);

        builder.Property(s => s.UnusedLeavePay)
            .HasPrecision(18, 0);

        builder.Property(s => s.SeveranceAllowance)
            .HasPrecision(18, 0);

        builder.Property(s => s.OtherAllowances)
            .HasPrecision(18, 0);

        builder.Property(s => s.Deductions)
            .HasPrecision(18, 0);

        builder.Property(s => s.NetSettlementAmount)
            .HasPrecision(18, 0);

        builder.HasOne(s => s.Employee)
            .WithMany()
            .HasForeignKey(s => s.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.EmployeeId).IsUnique();
    }
}