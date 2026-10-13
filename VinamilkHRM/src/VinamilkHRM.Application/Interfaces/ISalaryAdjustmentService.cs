using VinamilkHRM.Application.DTOs.Salaries;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.Interfaces;

/// <summary>
/// Dịch vụ xử lý Quy trình Tăng lương Thường lệ & Giám đốc Đặc cách (Salary Adjustment Workflow).
/// Hỗ trợ cả 2 luồng:
/// 1. Quy trình thường lệ đa cấp (Trưởng bộ phận -> Giám đốc Nhân sự & C&B).
/// 2. Fast-Track Executive Override của Tổng Giám đốc (CEO) kèm sinh Phụ lục hợp đồng số và Ghi vết AuditLog bảo mật cao.
/// </summary>
public interface ISalaryAdjustmentService
{
    Task<IReadOnlyList<SalaryProposalDto>> GetAllProposalsAsync(SalaryProposalStatus? status = null, CancellationToken cancellationToken = default);
    Task<SalaryProposalDto?> GetProposalByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<SalaryProposalDto> SubmitDepartmentProposalAsync(CreateSalaryProposalRequest request, CancellationToken cancellationToken = default);
    Task ApproveByDepartmentHeadAsync(int proposalId, int managerId, string notes, CancellationToken cancellationToken = default);
    Task ApproveByHRDirectorAsync(int proposalId, int hrDirectorId, string notes, CancellationToken cancellationToken = default);
    Task RejectProposalAsync(int proposalId, int reviewerId, string rejectionReason, CancellationToken cancellationToken = default);
    Task<ElectronicContractAppendixDto> FastTrackExecutiveOverrideAsync(FastTrackCeoOverrideRequest request, CancellationToken cancellationToken = default);
    Task<ElectronicContractAppendixDto> CreateDirectCeoOverrideAsync(DirectCeoOverrideRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectronicContractAppendixDto>> GetAppendicesByEmployeeAsync(int employeeId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ElectronicContractAppendixDto>> GetAllAppendicesAsync(CancellationToken cancellationToken = default);
}
