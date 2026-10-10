using Microsoft.Extensions.DependencyInjection;
using VinamilkHRM.Application.Interfaces;
using VinamilkHRM.Application.Services;

namespace VinamilkHRM.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IEmployeeService, EmployeeService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IShiftAssignmentService, ShiftAssignmentService>();
        services.AddScoped<IKpiService, KpiService>();

        return services;
    }
}
