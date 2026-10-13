namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Trạng thái từng bước duyệt trong Route phê duyệt lương.
/// </summary>
public enum ApprovalStepStatus
{
    Pending = 1,
    Approved = 2,
    Rejected = 3,
    Skipped = 4
}