namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Phân loại quy trình phê duyệt trong hệ thống Vinamilk HRM.
/// </summary>
public enum WorkflowType
{
    LeaveRequest = 1,
    OvertimeRequest = 2,
    ShiftSwapRequest = 3,
    KpiAppeal = 4,
    SalaryAdjustment = 5,
    OnboardingChecklist = 6,
    OffboardingChecklist = 7,
    Generic = 99
}