using VinamilkHRM.Application.DTOs.Kpis;

namespace VinamilkHRM.Application.Interfaces;

public interface IKpiService
{
    /// <summary>
    /// Lãnh đạo khởi tạo chỉ tiêu KPI gắn hạn định DueDate và Điểm chuẩn ban đầu BaseScore = 100.
    /// </summary>
    Task<KpiAssignmentDto> CreateKpiAsync(CreateKpiCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nhân viên bắt đầu tiếp nhận và triển khai chỉ tiêu KPI.
    /// </summary>
    Task StartKpiAsync(int kpiId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Nhân viên nộp báo cáo kết quả hoàn thành:
    /// Tự động kích hoạt Động cơ SLA: Sớm (+2đ/ngày, max +10đ), Trễ (-5đ/ngày, sàn 0đ).
    /// Chuyển trạng thái thành PendingApproval chờ Lãnh đạo thẩm định.
    /// </summary>
    Task<KpiAssignmentDto> SubmitKpiResultAsync(SubmitKpiCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Quy trình Duyệt Lãnh đạo (Leader Approval):
    /// Thẩm định minh chứng, áp dụng ScoreAdjustment và chốt FinalScore chính thức để chuyển sang Tính lương.
    /// </summary>
    Task<KpiAssignmentDto> LeaderApproveKpiAsync(LeaderApproveKpiCommand command, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lãnh đạo trả về yêu cầu chỉnh sửa/bổ sung số liệu.
    /// </summary>
    Task RejectKpiAsync(int kpiId, int leaderId, string rejectionReason, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy bảng theo dõi Kanban 4 cột (Assigned, InProgress, PendingApproval, Approved).
    /// </summary>
    Task<KpiKanbanBoardDto> GetKanbanBoardAsync(string period, int? unitId = null, int? employeeId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy chi tiết một chỉ tiêu KPI.
    /// </summary>
    Task<KpiAssignmentDto?> GetKpiByIdAsync(int id, CancellationToken cancellationToken = default);
}
