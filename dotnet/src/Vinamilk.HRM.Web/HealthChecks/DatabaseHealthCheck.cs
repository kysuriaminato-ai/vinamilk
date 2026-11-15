using Microsoft.Extensions.Diagnostics.HealthChecks;
using Vinamilk.HRM.Infrastructure.Persistence;

namespace Vinamilk.HRM.Web.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly ApplicationDbContext _dbContext;

    public DatabaseHealthCheck(ApplicationDbContext dbContext) => _dbContext = dbContext;

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await _dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("SQL Server kết nối bình thường.")
                : HealthCheckResult.Unhealthy("Không thể kết nối tới SQL Server.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Lỗi kiểm tra SQL Server.", ex);
        }
    }
}

