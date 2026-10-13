using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VinamilkHRM.Domain.Entities.Privacy;

namespace VinamilkHRM.Infrastructure.Data.Configurations.Privacy;

public class PersonalDataConsentConfiguration : IEntityTypeConfiguration<PersonalDataConsent>
{
    public void Configure(EntityTypeBuilder<PersonalDataConsent> builder)
    {
        builder.ToTable("PersonalDataConsents");
        builder.HasKey(c => c.Id);

        builder.Property(c => c.SubjectType)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.SubjectEmail)
            .HasMaxLength(256);

        builder.Property(c => c.ConsentPurpose)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(c => c.TermsVersion)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(c => c.IpAddress)
            .HasMaxLength(50);

        builder.HasIndex(c => new { c.SubjectId, c.SubjectType, c.ConsentPurpose });
    }
}

public class DataRetentionPolicyConfiguration : IEntityTypeConfiguration<DataRetentionPolicy>
{
    public void Configure(EntityTypeBuilder<DataRetentionPolicy> builder)
    {
        builder.ToTable("DataRetentionPolicies");
        builder.HasKey(p => p.Id);

        builder.Property(p => p.DataType)
            .IsRequired()
            .HasMaxLength(100);
        builder.HasIndex(p => p.DataType).IsUnique();

        builder.Property(p => p.ActionOnExpiry)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(p => p.Description)
            .HasMaxLength(500);
    }
}

public class PersonalDataAccessLogConfiguration : IEntityTypeConfiguration<PersonalDataAccessLog>
{
    public void Configure(EntityTypeBuilder<PersonalDataAccessLog> builder)
    {
        builder.ToTable("PersonalDataAccessLogs");
        builder.HasKey(l => l.Id);

        builder.Property(l => l.AccessedField)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.AccessPurpose)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.IpAddress)
            .HasMaxLength(50);

        builder.HasOne(l => l.TargetEmployee)
            .WithMany()
            .HasForeignKey(l => l.TargetEmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(l => new { l.ViewerUserId, l.TargetEmployeeId, l.AccessedField, l.AccessTimestamp });
    }
}