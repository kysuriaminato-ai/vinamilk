using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Vinamilk.HRM.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Logs slow EF Core SQL queries (> 300ms) for Core Web Vitals and SQL tuning.
/// </summary>
public class SlowQueryLoggingInterceptor : DbCommandInterceptor
{
    private const int SlowThresholdMs = 300;
    private readonly ILogger<SlowQueryLoggingInterceptor> _logger;

    public SlowQueryLoggingInterceptor(ILogger<SlowQueryLoggingInterceptor> logger)
        => _logger = logger;

    public override DbDataReader ReaderExecuted(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecuted(command, eventData, result);
    }

    public override ValueTask<DbDataReader> ReaderExecutedAsync(
        DbCommand command,
        CommandExecutedEventData eventData,
        DbDataReader result,
        CancellationToken cancellationToken = default)
    {
        LogIfSlow(command, eventData);
        return base.ReaderExecutedAsync(command, eventData, result, cancellationToken);
    }

    private void LogIfSlow(DbCommand command, CommandExecutedEventData eventData)
    {
        if (eventData.Duration.TotalMilliseconds >= SlowThresholdMs)
        {
            _logger.LogWarning(
                "[SLOW SQL QUERY] {DurationMs:0}ms | Command: {CommandText}",
                eventData.Duration.TotalMilliseconds,
                command.CommandText);
        }
    }
}

