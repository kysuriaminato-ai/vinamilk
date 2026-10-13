using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Mẫu phiếu lương dành riêng cho Khối Hành chính Văn phòng (Vinamilk Tower Quận 7).
/// Áp dụng tính ĐA HÌNH: Trình bày chi tiết KPI dự án SLA, phụ cấp cơm trưa văn phòng, và vị trí làm việc.
/// </summary>
public class OfficePayslipPrinter : BasePayslipPrinter
{
    protected override string GetPrinterTitle() => "KHỐI VĂN PHÒNG & QUẢN TRỊ TRỤ SỞ";

    protected override void RenderSpecificMetrics(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        var officeEmp = employee as OfficeEmployee;
        string jobTitle = officeEmp?.JobTitle ?? "Chuyên viên khối văn phòng";
        string deskLocation = officeEmp?.DeskLocation ?? "Tầng 8, Vinamilk Tower";
        string ext = officeEmp?.ExtensionNumber ?? "8801";

        sb.AppendLine("  <div style='background: #EFF6FF; border-left: 4px solid #3B82F6; padding: 10px 14px; margin-bottom: 20px; border-radius: 0 6px 6px 0; font-size: 13px;'>");
        sb.AppendLine("    <div style='display: flex; justify-content: space-between;'>");
        sb.AppendLine($"      <div><strong>Chức danh vị trí:</strong> {jobTitle}</div>");
        sb.AppendLine($"      <div><strong>Vị trí bàn làm việc:</strong> {deskLocation}</div>");
        sb.AppendLine($"      <div><strong>Máy nhánh (Ext):</strong> {ext}</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div style='margin-top: 6px; color: #1E40AF;'>");
        sb.AppendLine($"      <i class='bi bi-award'></i> <strong>Đánh giá KPI Dự án SLA:</strong> Thưởng hiệu suất đạt <strong>{payslip.KpiBonus:N0} đ</strong> (Đạt tiêu chuẩn chất lượng công việc văn phòng)");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
    }

    protected override void RenderSpecificEarningsAndAllowances(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        decimal lunch = payslip.LunchAllowance > 0 ? payslip.LunchAllowance : 730_000m;

        // Phụ cấp cơm trưa văn phòng
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>3. Phụ cấp ăn trưa văn phòng (chuẩn 22 ngày)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #16a34a; font-weight: 500;'>{lunch:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Miễn thuế ăn trưa theo quy định</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");

        // Lương ngoài giờ OT văn phòng (có phụ cấp bữa tối nếu làm >= 2h)
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>4. Lương làm thêm giờ dự án ({payslip.OvertimeHours} giờ)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7;'>{payslip.OvertimePay:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Thu nhập OT được miễn thuế phần vượt</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");
    }
}
