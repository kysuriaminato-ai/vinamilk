using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ConfigureBaseAuditable<WorkflowDefinition, int>("WorkflowDefinitions", "system");

        builder.Property(x => x.WorkflowCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.WorkflowName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.EntityType).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Description).HasMaxLength(500);

        builder.HasIndex(x => x.WorkflowCode)
            .IsUnique()
            .HasDatabaseName("UQ_WorkflowDefinitions_WorkflowCode")
            .HasFilter("[IsDeleted] = 0");
    }
}

public class WorkflowStepConfiguration : IEntityTypeConfiguration<WorkflowStep>
{
    public void Configure(EntityTypeBuilder<WorkflowStep> builder)
    {
        builder.ConfigureBaseAuditable<WorkflowStep, int>("WorkflowSteps", "system");

        builder.Property(x => x.StepName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.ApproverType).HasColumnType("varchar(30)").IsRequired();
        builder.Property(x => x.ConditionExpression).HasMaxLength(300);

        builder.HasIndex(x => new { x.WorkflowDefinitionId, x.StepOrder })
            .IsUnique()
            .HasDatabaseName("UQ_WorkflowSteps_Def_Order")
            .HasFilter("[IsDeleted] = 0");

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(d => d.Steps)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowInstanceConfiguration : IEntityTypeConfiguration<WorkflowInstance>
{
    public void Configure(EntityTypeBuilder<WorkflowInstance> builder)
    {
        builder.ConfigureBaseAuditable<WorkflowInstance, int>("WorkflowInstances", "system");

        builder.Property(x => x.EntityType).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("InProgress");

        builder.HasIndex(x => new { x.EntityType, x.EntityId })
            .HasDatabaseName("IX_WorkflowInstances_Entity");

        builder.HasOne(x => x.WorkflowDefinition)
            .WithMany(d => d.Instances)
            .HasForeignKey(x => x.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.InitiatorEmployee)
            .WithMany()
            .HasForeignKey(x => x.InitiatorEmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkflowApprovalLogConfiguration : IEntityTypeConfiguration<WorkflowApprovalLog>
{
    public void Configure(EntityTypeBuilder<WorkflowApprovalLog> builder)
    {
        builder.ConfigureBaseAuditable<WorkflowApprovalLog, long>("WorkflowApprovalLogs", "system");

        builder.Property(x => x.StepName).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Action).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.Comment).HasMaxLength(500);

        builder.HasIndex(x => x.WorkflowInstanceId)
            .HasDatabaseName("IX_WorkflowApprovalLogs_InstanceId");

        builder.HasOne(x => x.WorkflowInstance)
            .WithMany(i => i.ApprovalLogs)
            .HasForeignKey(x => x.WorkflowInstanceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.ApproverUser)
            .WithMany()
            .HasForeignKey(x => x.ApproverUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ConfigureBaseAuditable<Notification, long>("Notifications", "system");

        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.NotificationType).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.TargetUrl).HasMaxLength(300);

        builder.HasIndex(x => new { x.RecipientUserId, x.IsRead, x.CreatedAt })
            .HasDatabaseName("IX_Notifications_Recipient_Read");

        builder.HasOne(x => x.RecipientUser)
            .WithMany()
            .HasForeignKey(x => x.RecipientUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AiRecommendationConfiguration : IEntityTypeConfiguration<AiRecommendation>
{
    public void Configure(EntityTypeBuilder<AiRecommendation> builder)
    {
        builder.ConfigureBaseAuditable<AiRecommendation, int>("AiRecommendations", "system");

        builder.Property(x => x.RecommendationType).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.PriorityLevel).HasColumnType("varchar(20)").IsRequired();
        builder.Property(x => x.ConfidenceScore).HasColumnType("decimal(5,2)");
        builder.Property(x => x.Title).HasMaxLength(250).IsRequired();
        builder.Property(x => x.AnalysisContent).IsRequired();
        builder.Property(x => x.SuggestedAction).HasMaxLength(1000);
        builder.Property(x => x.Status).HasColumnType("varchar(30)").HasDefaultValue("New");

        builder.HasIndex(x => new { x.RecommendationType, x.PriorityLevel, x.Status })
            .HasDatabaseName("IX_AiRecommendations_Type_Priority");

        builder.HasOne(x => x.Employee)
            .WithMany()
            .HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.OrganizationUnit)
            .WithMany()
            .HasForeignKey(x => x.OrganizationUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ConfigureBaseAuditable<AuditLog, long>("AuditLogs", "system");

        builder.Property(x => x.Username).HasMaxLength(100).IsRequired();
        builder.Property(x => x.ModuleName).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.ActionType).HasColumnType("varchar(60)").IsRequired();
        builder.Property(x => x.EntityName).HasColumnType("varchar(100)").IsRequired();
        builder.Property(x => x.EntityId).HasColumnType("varchar(80)").IsRequired();
        builder.Property(x => x.AffectedColumns).HasMaxLength(500);
        builder.Property(x => x.IpAddress).HasColumnType("varchar(64)");
        builder.Property(x => x.UserAgent).HasMaxLength(500);

        builder.HasIndex(x => new { x.EntityName, x.EntityId, x.CreatedAt })
            .HasDatabaseName("IX_AuditLogs_Entity_CreatedAt");
    }
}

public class MenuItemConfiguration : IEntityTypeConfiguration<MenuItem>
{
    public void Configure(EntityTypeBuilder<MenuItem> builder)
    {
        builder.ConfigureBaseAuditable<MenuItem, int>("MenuItems", "system");

        builder.Property(x => x.MenuCode).HasColumnType("varchar(50)").IsRequired();
        builder.Property(x => x.Title).HasMaxLength(150).IsRequired();
        builder.Property(x => x.Icon).HasColumnType("varchar(50)");
        builder.Property(x => x.RouteUrl).HasMaxLength(200);
        builder.Property(x => x.RequiredPermissionCode).HasColumnType("varchar(100)");

        builder.HasIndex(x => x.MenuCode)
            .IsUnique()
            .HasDatabaseName("UQ_MenuItems_MenuCode")
            .HasFilter("[IsDeleted] = 0");
    }
}
