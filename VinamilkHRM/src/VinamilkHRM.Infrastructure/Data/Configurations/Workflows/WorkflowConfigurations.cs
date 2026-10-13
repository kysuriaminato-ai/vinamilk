using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Workflows;

namespace VinamilkHRM.Infrastructure.Data.Configurations.Workflows;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(w => w.Code).IsUnique();

        builder.Property(w => w.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(w => w.Description)
            .HasMaxLength(1000);

        builder.HasMany(w => w.Steps)
            .WithOne(s => s.WorkflowDefinition)
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class WorkflowStepDefinitionConfiguration : IEntityTypeConfiguration<WorkflowStepDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowStepDefinition> builder)
    {
        builder.ToTable("WorkflowStepDefinitions");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.MinDays)
            .HasPrecision(5, 2);

        builder.Property(s => s.MaxDays)
            .HasPrecision(5, 2);

        builder.Property(s => s.MinPercentage)
            .HasPrecision(5, 2);

        builder.Property(s => s.MinAmount)
            .HasPrecision(18, 0);

        builder.HasIndex(s => new { s.WorkflowDefinitionId, s.StepOrder }).IsUnique();
    }
}

public class ApprovalDelegationConfiguration : IEntityTypeConfiguration<ApprovalDelegation>
{
    public void Configure(EntityTypeBuilder<ApprovalDelegation> builder)
    {
        builder.ToTable("ApprovalDelegations");
        builder.HasKey(d => d.Id);

        builder.Property(d => d.Reason)
            .IsRequired()
            .HasMaxLength(500);

        builder.HasOne(d => d.Delegator)
            .WithMany()
            .HasForeignKey(d => d.DelegatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Delegatee)
            .WithMany()
            .HasForeignKey(d => d.DelegateeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(d => new { d.DelegatorId, d.FromDate, d.ToDate, d.IsActive });
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ToTable("WorkflowInstances");
        builder.HasKey(w => w.Id);

        builder.Property(w => w.EntityType)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasOne(w => w.WorkflowDefinition)
            .WithMany()
            .HasForeignKey(w => w.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Requester)
            .WithMany()
            .HasForeignKey(w => w.RequesterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(w => w.Steps)
            .WithOne(s => s.WorkflowInstance)
            .HasForeignKey(s => s.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(w => new { w.EntityType, w.EntityId });
    }
}

public class WorkflowStepInstanceConfiguration : IEntityTypeConfiguration<WorkflowStepInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowStepInstance> builder)
    {
        builder.ToTable("WorkflowStepInstances");
        builder.HasKey(s => s.Id);

        builder.Property(s => s.StepName)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(s => s.Comment)
            .HasMaxLength(1000);

        builder.HasOne(s => s.AssignedApprover)
            .WithMany()
            .HasForeignKey(s => s.AssignedApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ActualApprover)
            .WithMany()
            .HasForeignKey(s => s.ActualApproverId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.DelegatedFrom)
            .WithMany()
            .HasForeignKey(s => s.DelegatedFromId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.WorkflowInstanceId, s.StepOrder });
        builder.HasIndex(s => new { s.Status, s.SlaDeadline, s.IsAutoEscalate });
    }
}