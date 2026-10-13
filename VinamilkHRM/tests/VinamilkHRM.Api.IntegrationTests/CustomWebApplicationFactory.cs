using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Infrastructure.Data;
using VinamilkHRM.Infrastructure.Interceptors;

namespace VinamilkHRM.Api.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string TestConnectionString =
        "Server=(localdb)\\mssqllocaldb;Database=VinamilkHRM_IntegrationTestDb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var descriptors = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<VinamilkHRMDbContext>) ||
                            d.ServiceType == typeof(DbContextOptions))
                .ToList();

            foreach (var d in descriptors)
            {
                services.Remove(d);
            }

            services.AddDbContext<VinamilkHRMDbContext>((sp, options) =>
            {
                var interceptor = sp.GetRequiredService<AuditableEntityInterceptor>();
                options.UseSqlServer(TestConnectionString, sqlOptions =>
                {
                    sqlOptions.UseHierarchyId();
                })
                .AddInterceptors(interceptor);
            });

            // Khởi tạo schema mới nhất trên LocalDB
            var spBuild = services.BuildServiceProvider();
            using var scope = spBuild.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<VinamilkHRMDbContext>();
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();
        });

        builder.UseEnvironment("Development");
    }
}
