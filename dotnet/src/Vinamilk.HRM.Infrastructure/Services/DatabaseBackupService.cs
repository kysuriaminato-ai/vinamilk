using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Infrastructure.Services;

public record BackupExecutionResult(bool Success, string Mode, string FilePath, long SizeBytes, string Message);

public class DatabaseBackupService
{
    private readonly IApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseBackupService> _logger;

    public DatabaseBackupService(
        IApplicationDbContext context,
        IConfiguration configuration,
        ILogger<DatabaseBackupService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<BackupExecutionResult> RunScheduledBackupAsync(
        string? outputDirectory = null,
        CancellationToken cancellationToken = default)
    {
        var dir = outputDirectory
            ?? _configuration["Backup:Directory"]
            ?? Path.Combine(AppContext.BaseDirectory, "backups");

        Directory.CreateDirectory(dir);
        var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");

        // Export lightweight JSON metadata snapshot (safe across SQL Server, LocalDB, and InMemory)
        var snapshotPath = Path.Combine(dir, $"vinamilk_hrm_snapshot_{timestamp}.json");
        var summary = new
        {
            GeneratedAtUtc = DateTime.UtcNow,
            DatabaseProvider = "SQL Server / EF Core 8",
            Counts = new
            {
                Employees = await _context.Employees.CountAsync(cancellationToken),
                OrganizationUnits = await _context.OrganizationUnits.CountAsync(cancellationToken),
                LaborContracts = await _context.LaborContracts.CountAsync(cancellationToken),
                PayrollPeriods = await _context.PayrollPeriods.CountAsync(cancellationToken),
                JobPostings = await _context.JobPostings.CountAsync(cancellationToken)
            },
            SqlBackupCommandTemplate = $"BACKUP DATABASE [VinamilkHRM] TO DISK = N'{Path.Combine(dir, $"VinamilkHRM_{timestamp}.bak")}' WITH NOFORMAT, NOINIT, NAME = N'VinamilkHRM-Full Database Backup', SKIP, NOREWIND, NOUNLOAD, STATS = 10;"
        };

        var json = JsonSerializer.Serialize(summary, new JsonSerializerOptions { WriteIndented = true });
        await File.WriteAllTextAsync(snapshotPath, json, cancellationToken);
        var size = new FileInfo(snapshotPath).Length;

        _logger.LogInformation("[DB BACKUP] Created backup snapshot at {Path} ({Size} bytes)", snapshotPath, size);
        return new BackupExecutionResult(true, "Snapshot+SqlTemplate", snapshotPath, size, "Sao lưu metadata & script SQL định kỳ thành công.");
    }
}

