using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Notifications;

namespace VinamilkHRM.Infrastructure.Data.Configurations.Notifications;

public class NotificationTemplateConfiguration : IEntityTypeConfiguration<NotificationTemplate>
{
    public void Configure(EntityTypeBuilder<NotificationTemplate> builder)
    {
        builder.ToTable("NotificationTemplates");
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(50);
        builder.HasIndex(t => t.Code).IsUnique();

        builder.Property(t => t.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(t => t.SubjectTemplate)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(t => t.BodyTemplate)
            .IsRequired();
    }
}

public class NotificationPreferenceConfiguration : IEntityTypeConfiguration<NotificationPreference>
{
    public void Configure(EntityTypeBuilder<NotificationPreference> builder)
    {
        builder.ToTable("NotificationPreferences");
        builder.HasKey(p => p.Id);

        builder.HasOne(p => p.Employee)
            .WithMany()
            .HasForeignKey(p => p.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(p => new { p.EmployeeId, p.Category }).IsUnique();
    }
}

public class InAppNotificationConfiguration : IEntityTypeConfiguration<InAppNotification>
{
    public void Configure(EntityTypeBuilder<InAppNotification> builder)
    {
        builder.ToTable("InAppNotifications");
        builder.HasKey(n => n.Id);

        builder.Property(n => n.Title)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(n => n.Message)
            .IsRequired();

        builder.Property(n => n.TargetUrl)
            .HasMaxLength(500);

        builder.HasOne(n => n.RecipientEmployee)
            .WithMany()
            .HasForeignKey(n => n.RecipientEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(n => new { n.RecipientEmployeeId, n.IsRead, n.CreatedAt });
    }
}

public class NotificationOutboxMessageConfiguration : IEntityTypeConfiguration<NotificationOutboxMessage>
{
    public void Configure(EntityTypeBuilder<NotificationOutboxMessage> builder)
    {
        builder.ToTable("NotificationOutboxMessages");
        builder.HasKey(m => m.Id);

        builder.Property(m => m.RecipientEmail)
            .HasMaxLength(256);

        builder.Property(m => m.Subject)
            .IsRequired()
            .HasMaxLength(300);

        builder.Property(m => m.Body)
            .IsRequired();

        builder.Property(m => m.TargetUrl)
            .HasMaxLength(500);

        builder.Property(m => m.LastError)
            .HasMaxLength(2000);

        builder.HasIndex(m => new { m.Status, m.RetryCount, m.CreatedAt });
    }
}