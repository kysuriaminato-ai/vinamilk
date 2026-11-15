using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Domain.Common;
using Vinamilk.HRM.Domain.Entities.Identity;
using Vinamilk.HRM.Domain.Entities.SystemEntities;

namespace Vinamilk.HRM.Infrastructure.Persistence.Interceptors;

public sealed class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditableEntityInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateEntitiesAndWriteAuditLogs(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateEntitiesAndWriteAuditLogs(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntitiesAndWriteAuditLogs(DbContext? context)
    {
        if (context is null)
            return;

        var utcNow = DateTime.UtcNow;
        var username = string.IsNullOrWhiteSpace(_currentUserService.Username)
            ? "SYSTEM"
            : _currentUserService.Username;

        var auditEntries = new List<AuditLog>();

        foreach (var entry in context.ChangeTracker.Entries<IAuditableEntity>().ToList())
        {
            if (entry.Entity is AuditLog or UserSession)
            {
                if (entry.State == EntityState.Added)
                {
                    entry.Entity.CreatedAt = utcNow;
                    entry.Entity.CreatedBy = username;
                }
                continue;
            }

            if (entry.State == EntityState.Added)
            {
                entry.Entity.CreatedAt = utcNow;
                if (string.IsNullOrWhiteSpace(entry.Entity.CreatedBy) || entry.Entity.CreatedBy == "SYSTEM")
                    entry.Entity.CreatedBy = username;

                auditEntries.Add(CreateAuditLog(entry, "Create", utcNow, username));
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.UpdatedAt = utcNow;
                entry.Entity.UpdatedBy = username;

                var isSoftDeleteTransition = entry.Property(nameof(IAuditableEntity.IsDeleted)).IsModified
                                             && entry.Entity.IsDeleted;
                auditEntries.Add(CreateAuditLog(
                    entry,
                    isSoftDeleteTransition ? "Delete" : "Update",
                    utcNow,
                    username));
            }
            else if (entry.State == EntityState.Deleted)
            {
                // Chuyển Hard Delete thành Soft Delete
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.UpdatedAt = utcNow;
                entry.Entity.UpdatedBy = username;

                auditEntries.Add(CreateAuditLog(entry, "Delete", utcNow, username));
            }
        }

        if (auditEntries.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditEntries);
        }
    }

    private AuditLog CreateAuditLog(
        EntityEntry<IAuditableEntity> entry,
        string actionType,
        DateTime utcNow,
        string username)
    {
        var entityName = entry.Entity.GetType().Name;
        var oldValues = new Dictionary<string, object?>();
        var newValues = new Dictionary<string, object?>();
        var affectedColumns = new List<string>();

        foreach (var property in entry.Properties)
        {
            var propertyName = property.Metadata.Name;
            if (propertyName is nameof(IAuditableEntity.RowVersion))
                continue;

            switch (actionType)
            {
                case "Create":
                    newValues[propertyName] = property.CurrentValue;
                    break;
                case "Delete":
                    oldValues[propertyName] = property.OriginalValue;
                    break;
                case "Update":
                    if (property.IsModified)
                    {
                        affectedColumns.Add(propertyName);
                        oldValues[propertyName] = property.OriginalValue;
                        newValues[propertyName] = property.CurrentValue;
                    }
                    break;
            }
        }

        var idProp = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
        var entityId = idProp?.CurrentValue?.ToString() ?? "0";

        return new AuditLog
        {
            UserId = _currentUserService.UserId,
            Username = username,
            ModuleName = ResolveModuleName(entry.Entity.GetType().Namespace),
            ActionType = actionType,
            EntityName = entityName,
            EntityId = entityId,
            OldValuesJson = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null,
            NewValuesJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null,
            AffectedColumns = affectedColumns.Count > 0 ? string.Join(",", affectedColumns) : null,
            IpAddress = _currentUserService.IpAddress ?? "127.0.0.1",
            UserAgent = _currentUserService.UserAgent ?? "EF-Core-Interceptor",
            CreatedAt = utcNow,
            CreatedBy = username
        };
    }

    private static string ResolveModuleName(string? entityNamespace)
    {
        if (string.IsNullOrEmpty(entityNamespace))
            return "System";
        if (entityNamespace.EndsWith(".Identity", StringComparison.OrdinalIgnoreCase))
            return "Identity";
        if (entityNamespace.EndsWith(".Core", StringComparison.OrdinalIgnoreCase))
            return "CoreHR";
        if (entityNamespace.EndsWith(".Time", StringComparison.OrdinalIgnoreCase))
            return "AttendanceLeave";
        if (entityNamespace.EndsWith(".Payroll", StringComparison.OrdinalIgnoreCase))
            return "Payroll";
        if (entityNamespace.EndsWith(".Talent", StringComparison.OrdinalIgnoreCase))
            return "Talent";
        return "System";
    }
}

