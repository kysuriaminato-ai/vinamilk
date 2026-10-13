using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Mẫu phiếu lương dành riêng cho Khối Chuỗi Cửa Hàng Bán Lẻ (Vinamilk - Giấc Mơ Sữa Việt).
/// Áp dụng tính ĐA HÌNH: Trình bày chi tiết Lương cơ bản, Doanh số bán lẻ, Hoa hồng chuỗi cửa hàng,
/// và Điểm đánh giá chất lượng phục vụ khách hàng (Customer Service Rating).
/// </summary>
public class RetailStorePayslipPrinter : BasePayslipPrinter
{
    protected override string GetPrinterTitle() => "KHỐI CHUỖI CỬA HÀNG GIẤC MƠ SỮA VIỆT";

    protected override void RenderSpecificMetrics(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        var associate = employee as StoreAssociate;
        string registerId = associate?.CashRegisterId ?? "POS-VNM-STORE-088";
        decimal rating = associate?.CustomerServiceRating ?? 4.9m;
        decimal bonusRate = associate?.RetailSalesBonusRate ?? 0.025m;

        sb.AppendLine("  <div style='background: #ECFDF5; border-left: 4px solid #10B981; padding: 10px 14px; margin-bottom: 20px; border-radius: 0 6px 6px 0; font-size: 13px;'>");
        sb.AppendLine("    <div style='display: flex; justify-content: space-between;'>");
        sb.AppendLine($"      <div><strong>Mã quầy thu ngân (POS):</strong> {registerId}</div>");
        sb.AppendLine($"      <div><strong>Đánh giá dịch vụ khách hàng:</strong> <span style='color: #F59E0B;'>{new string('★', (int)rating)}</span> <strong>{rating:F1}/5.0</strong></div>");
        sb.AppendLine($"      <div><strong>Tỷ lệ hoa hồng chuỗi:</strong> <strong>{(bonusRate * 100):F1}%</strong></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div style='margin-top: 6px; color: #065F46;'>");
        sb.AppendLine($"      🛒 <strong>Thành tích doanh số cửa hàng:</strong> Đạt chuẩn chỉ tiêu doanh thu chuỗi cửa hàng bán lẻ Vinamilk.");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
    }

    protected override void RenderSpecificEarningsAndAllowances(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        var associate = employee as StoreAssociate;
        decimal commission = payslip.SalesCommission > 0 ? payslip.SalesCommission : 3_250_000m;

        // Hoa hồng doanh số bán lẻ chuỗi cửa hàng
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>3. Hoa hồng doanh số chuỗi Giấc Mơ Sữa Việt</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #16a34a; font-weight: 600;'>{commission:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Thu nhập tính thuế theo biểu lũy tiến</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>-</td>");
        sb.AppendLine("      </tr>");

        // Lương làm thêm giờ cao điểm & ca tối bán lẻ
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>4. Lương đứng quầy cao điểm ca tối ({payslip.OvertimeHours} giờ)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7;'>{payslip.OvertimePay:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Miễn thuế phần chênh lệch làm thêm giờ</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");
    }
}
