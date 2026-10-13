using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.Salaries;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Mẫu phiếu lương dành riêng cho Khối Nhà máy Sản xuất Sữa (Mega Factory, Nhà máy Sài Gòn, Thống Nhất,...).
/// Áp dụng tính ĐA HÌNH: Trình bày chi tiết giờ làm việc ca đêm (+30%), phụ cấp độc hại/tiếng ồn,
/// và phụ cấp bồi dưỡng trực tiếp tại chuyền sản xuất tiệt trùng.
/// </summary>
public class FactoryPayslipPrinter : BasePayslipPrinter
{
    protected override string GetPrinterTitle() => "KHỐI NHÀ MÁY SẢN XUẤT SỮA";

    protected override void RenderSpecificMetrics(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        var worker = employee as FactoryWorker;
        string shiftGroup = worker?.ProductionShiftGroup ?? "Tổ Dây chuyền Tiệt trùng UHT - Ca 3";
        string safetyLevel = worker?.SafetyLevel ?? "Level 3 - Chuẩn An toàn Mega Factory";
        string machineCert = worker?.MachineCertificationCode ?? "CERT-TETRAPAK-A3-FLEX";

        sb.AppendLine("  <div style='background: #FEF3C7; border-left: 4px solid #F59E0B; padding: 10px 14px; margin-bottom: 20px; border-radius: 0 6px 6px 0; font-size: 13px;'>");
        sb.AppendLine("    <div style='display: flex; justify-content: space-between;'>");
        sb.AppendLine($"      <div><strong>Tổ đội sản xuất:</strong> {shiftGroup}</div>");
        sb.AppendLine($"      <div><strong>Chứng chỉ vận hành máy:</strong> {machineCert}</div>");
        sb.AppendLine($"      <div><strong>Tiêu chuẩn an toàn:</strong> {safetyLevel}</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div style='margin-top: 6px; color: #92400E; display: flex; gap: 20px;'>");
        sb.AppendLine($"      <div>🌙 <strong>Giờ làm ca đêm (22h-06h):</strong> <strong>{payslip.NightShiftHours} giờ</strong> (Hưởng +30% theo Luật Lao động)</div>");
        sb.AppendLine($"      <div>⚡ <strong>Tổng giờ OT sản xuất:</strong> <strong>{payslip.OvertimeHours} giờ</strong></div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
    }

    protected override void RenderSpecificEarningsAndAllowances(PaySlip payslip, Employee employee, StringBuilder sb)
    {
        var worker = employee as FactoryWorker;
        decimal hazardous = payslip.HazardousAllowance > 0 ? payslip.HazardousAllowance : (worker?.HazardousAllowanceRate ?? 1_200_000m);
        decimal nightPay = payslip.NightShiftPay > 0 ? payslip.NightShiftPay : Math.Round(payslip.NightShiftHours * (payslip.BaseSalary / 176m) * 0.30m, 0);

        // Phụ cấp làm việc ca đêm +30%
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>3. Phụ cấp làm thêm ca đêm (+30% Luật LĐ)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #16a34a; font-weight: 600;'>{nightPay:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Miễn thuế phần phụ cấp ca đêm vượt</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");

        // Phụ cấp độc hại & môi trường nặng nhọc
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>4. Phụ cấp độc hại & tiếng ồn phân xưởng</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #16a34a; font-weight: 500;'>{hazardous:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Phụ cấp độc hại theo danh mục Bộ LĐ-TBXH</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");

        // Lương làm thêm ngoài giờ OT (kèm bồi dưỡng chuyền 50,000 đ/h)
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>5. Lương OT phân xưởng & bồi dưỡng chuyền</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7;'>{payslip.OvertimePay:N0}</td>");
        sb.AppendLine("        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7; color: #64748b;'><i>Miễn thuế phần chênh lệch làm thêm giờ</i></td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #64748b;'>0</td>");
        sb.AppendLine("      </tr>");
    }
}
