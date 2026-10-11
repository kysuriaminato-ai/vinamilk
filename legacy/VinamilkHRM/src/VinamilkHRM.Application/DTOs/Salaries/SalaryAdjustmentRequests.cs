namespace VinamilkHRM.Application.DTOs.Salaries;

public record CreateSalaryProposalRequest(
    int EmployeeId,
    int ProposedById,
    decimal ProposedSalary,
    decimal ProposedAllowance,
    string Reason,
    DateTime EffectiveDate);

public record ApproveProposalRequest(
    int ProposalId,
    int ReviewerId,
    string Notes);

public record RejectProposalRequest(
    int ProposalId,
    int ReviewerId,
    string RejectionReason);

public record FastTrackCeoOverrideRequest(
    int ProposalId,
    int CeoEmployeeId,
    string Justification,
    string? OverrideCode = null,
    string? IpAddress = null);

public record DirectCeoOverrideRequest(
    int EmployeeId,
    decimal NewSalary,
    decimal NewAllowance,
    int CeoEmployeeId,
    string Justification,
    string? OverrideCode = null,
    DateTime? EffectiveDate = null,
    string? IpAddress = null);
