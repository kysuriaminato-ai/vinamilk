using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Common;

namespace Vinamilk.HRM.Infrastructure.Persistence.Configurations;

public static class BaseAuditableEntityConfigurationExtensions
{
    public static void ConfigureBaseAuditable<TEntity, TKey>(
        this EntityTypeBuilder<TEntity> builder,
        string tableName,
        string schemaName)
        where TEntity : BaseAuditableEntity<TKey>
    {
        builder.ToTable(tableName, schemaName);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.CreatedAt)
            .HasColumnType("datetime2(7)")
            .HasDefaultValueSql("SYSUTCDATETIME()")
            .IsRequired();

        builder.Property(x => x.CreatedBy)
            .HasMaxLength(100)
            .HasDefaultValue(SystemJobAccount.Username)
            .IsRequired();

        builder.Property(x => x.UpdatedAt)
            .HasColumnType("datetime2(7)");

        builder.Property(x => x.UpdatedBy)
            .HasMaxLength(100);

        builder.Property(x => x.IsDeleted)
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(x => x.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Ignore(x => x.DomainEvents);

        builder.HasQueryFilter(x => !x.IsDeleted);
    }
}

