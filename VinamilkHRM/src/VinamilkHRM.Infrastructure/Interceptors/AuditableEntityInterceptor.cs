using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using VinamilkHRM.Application.Common.Interfaces;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Assets;
using VinamilkHRM.Domain.Entities.Attendances;
using VinamilkHRM.Domain.Entities.Auditing;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Kpis;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Interceptors;

/// <summary>
/// Interceptor tự động ghi nhận thời gian, UserId thật của người thực hiện (Non-repudiation)
/// và tự động ghi log kiểm toán JSON trước/sau cho các thực thể nhạy cảm.
/// </summary>
public class AuditableEntityInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUser _currentUser;

    public AuditableEntityInterceptor(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context == null) return;

        string currentUserId = _currentUser.UserId ?? "SYSTEM";
        DateTime utcNow = DateTime.UtcNow;

        // 1. Cập nhật thông tin BaseAuditableEntity với UserId thật
        foreach (var entry in context.ChangeTracker.Entries<BaseAuditableEntity>())
        {
            if (entry.State == EntityState.Added)
            {
                entry.Entity.SetAuditCreated(currentUserId, utcNow);
            }
            else if (entry.State == EntityState.Modified)
            {
                entry.Entity.SetAuditUpdated(currentUserId, utcNow);
            }
        }

        // 2. Tự động ghi nhận AuditLog JSON trước/sau cho các thực thể nhạy cảm
        var sensitiveEntries = context.ChangeTracker.Entries()
            .Where(e => e.Entity is Employee or PaySlip or KpiAssignment or Attendance or LaborContract or ElectronicContractAppendix or SalaryProposal)
            .Where(e => e.State is EntityState.Added or EntityState.Modified or EntityState.Deleted)
            .ToList();

        var auditLogs = new List<AuditLog>();

        foreach (var entry in sensitiveEntries)
        {
            string tableName = entry.Metadata.GetTableName() ?? entry.Entity.GetType().Name;
            string action = entry.State switch
            {
                EntityState.Added => "INSERT",
                EntityState.Modified => "UPDATE",
                EntityState.Deleted => "DELETE",
                _ => entry.State.ToString()
            };

            var oldValues = new Dictionary<string, object?>();
            var newValues = new Dictionary<string, object?>();

            foreach (var property in entry.Properties)
            {
                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey()) continue;

                switch (entry.State)
                {
                    case EntityState.Added:
                        newValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        oldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            oldValues[propertyName] = property.OriginalValue;
                            newValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }

            var idProperty = entry.Properties.FirstOrDefault(p => p.Metadata.IsPrimaryKey());
            string entityId = idProperty?.CurrentValue?.ToString() ?? "0";

            string? oldJson = oldValues.Count > 0 ? JsonSerializer.Serialize(oldValues) : null;
            string? newJson = newValues.Count > 0 ? JsonSerializer.Serialize(newValues) : null;

            auditLogs.Add(new AuditLog(
                tableName: tableName,
                action: action,
                entityId: entityId,
                oldValues: oldJson,
                newValues: newJson,
                performedBy: currentUserId,
                ipAddress: null
            ));
        }

        if (auditLogs.Count > 0)
        {
            context.Set<AuditLog>().AddRange(auditLogs);
        }
    }
}
