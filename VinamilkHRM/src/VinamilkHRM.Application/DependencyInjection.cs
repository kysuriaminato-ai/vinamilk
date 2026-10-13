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
        services.AddScoped<IKpiCalibrationService, KpiCalibrationService>();
        services.AddScoped<IKpiMetricService, KpiMetricService>();
        services.AddScoped<IKpiBackgroundJobService, KpiBackgroundJobService>();
        services.AddScoped<ISalaryAdjustmentService, SalaryAdjustmentService>();
        services.AddScoped<ISalaryReviewService, SalaryReviewService>();
        services.AddScoped<IPeopleAnalyticsService, PeopleAnalyticsService>();
        services.AddScoped<VinamilkHRM.Application.Jobs.WorkforceSnapshotBackgroundJobs>();

        services.AddScoped<VinamilkHRM.Application.Jobs.SalaryReviewBackgroundJobs>();

        services.AddScoped<ICareersService, CareersService>();
        services.AddScoped<IStrategicDashboardService, StrategicDashboardService>();
        services.AddScoped<IOrganizationalUnitService, OrganizationalUnitService>();
        services.AddScoped<IJobService, JobService>();
        services.AddScoped<IEmployeeExcelService, EmployeeExcelService>();

        // Phase 3 Time & Attendance Services
        services.AddScoped<IRosterConstraintValidator, RosterConstraintValidator>();
        services.AddScoped<IRosterService, RosterService>();
        services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
        services.AddScoped<ITimeEventService, TimeEventService>();
        services.AddScoped<IAttendanceExplanationService, AttendanceExplanationService>();
        services.AddScoped<ITimesheetPeriodService, TimesheetPeriodService>();
        services.AddScoped<IOvertimeService, OvertimeService>();
        services.AddScoped<VinamilkHRM.Application.Jobs.AttendanceBackgroundJobs>();

                // Session 11 P1-1 Workflow Engine Service
        services.AddScoped<VinamilkHRM.Domain.Interfaces.Workflows.IWorkflowEngine, VinamilkHRM.Application.Workflows.WorkflowEngineService>();

                // Session 11 P1-2 Notification Service
        services.AddScoped<VinamilkHRM.Domain.Interfaces.Notifications.INotificationService, VinamilkHRM.Application.Notifications.NotificationService>();

                // Session 11 P1-3 Contract Management & Document Generation Services
        services.AddScoped<VinamilkHRM.Application.Contracts.IContractDocumentGenerator, VinamilkHRM.Application.Contracts.DocxContractDocumentGenerator>();
        services.AddScoped<VinamilkHRM.Application.Contracts.IContractManagementService, VinamilkHRM.Application.Contracts.ContractManagementService>();

                // Session 11 P1-4 Onboarding / Offboarding Service
        services.AddScoped<VinamilkHRM.Application.OnboardingOffboarding.IOnboardingOffboardingService, VinamilkHRM.Application.OnboardingOffboarding.OnboardingOffboardingService>();

        return services;
    }
}
