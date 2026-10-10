using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Domain.Interfaces.Printing;

/// <summary>
/// Giao diện đa hình (Polymorphism) cho việc kết xuất và in phiếu lương nhân viên Vinamilk.
/// Cho phép thay thế linh hoạt các engine sinh tài liệu (QuestPDF, iTextSharp, HTML-to-PDF).
/// </summary>
public interface IPayslipPrinter
{
    /// <summary>
    /// Xuất phiếu lương cá nhân thành tài liệu chuẩn định dạng PDF.
    /// </summary>
    byte[] PrintPayslipToPdf(PaySlip payslip, Employee employee, OrganizationalUnit unit);

    /// <summary>
    /// Xuất bảng lương tập trung của toàn bộ phòng ban/đơn vị theo lô (batch).
    /// </summary>
    byte[] PrintBatchPayslipsToPdf(IEnumerable<(PaySlip Payslip, Employee Employee, OrganizationalUnit Unit)> payslipBatch);

    /// <summary>
    /// Sinh mã HTML tương tác phục vụ việc hiển thị trước (preview) trên cổng thông tin nội bộ.
    /// </summary>
    string GenerateHtmlPreview(PaySlip payslip, Employee employee, OrganizationalUnit unit);
}
