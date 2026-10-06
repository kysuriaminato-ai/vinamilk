using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Interfaces.Printing;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Dịch vụ kết xuất và in phiếu lương nhân viên Vinamilk (Polymorphic implementation của IPayslipPrinter).
/// </summary>
public class PayslipPrinter : IPayslipPrinter
{
    public byte[] PrintPayslipToPdf(PaySlip payslip, Employee employee, OrganizationalUnit unit)
    {
        string html = GenerateHtmlPreview(payslip, employee, unit);
        // Chuyển đổi tài liệu sang dạng binary bytes (có thể cắm QuestPDF / DinkToPdf / Puppeteer)
        return Encoding.UTF8.GetBytes(html);
    }

    public byte[] PrintBatchPayslipsToPdf(IEnumerable<(PaySlip Payslip, Employee Employee, OrganizationalUnit Unit)> batch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>.page-break{page-break-after:always;}</style></head><body>");

        foreach (var item in batch)
        {
            sb.AppendLine("<div class='page-break'>");
            sb.AppendLine(GenerateHtmlPreview(item.Payslip, item.Employee, item.Unit));
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public string GenerateHtmlPreview(PaySlip payslip, Employee employee, OrganizationalUnit unit)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<div style='font-family: Arial, sans-serif; max-width: 800px; margin: auto; padding: 24px; border: 1px solid #ddd; border-radius: 8px;'>");
        sb.AppendLine("  <div style='text-align: center; border-bottom: 2px solid #0047BA; padding-bottom: 12px; margin-bottom: 20px;'>");
        sb.AppendLine("    <h2 style='color: #0047BA; margin: 0; text-transform: uppercase;'>CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</h2>");
        sb.AppendLine($"    <p style='margin: 4px 0; color: #555;'>Đơn vị: <strong>{unit?.UnitName ?? "Tập đoàn Vinamilk"}</strong> | Mã ĐV: {unit?.UnitCode}</p>");
        sb.AppendLine($"    <h3 style='margin: 10px 0 0 0; color: #222;'>PHIẾU THANH TOÁN LƯƠNG - KỲ THÁNG {payslip.PayrollPeriod}</h3>");
        sb.AppendLine("  </div>");

        sb.AppendLine("  <table style='width: 100%; margin-bottom: 16px; font-size: 14px;'>");
        sb.AppendLine($"    <tr><td><strong>Mã NV:</strong> {employee.EmployeeCode}</td><td><strong>Họ và tên:</strong> {employee.FullName}</td></tr>");
        sb.AppendLine($"    <tr><td><strong>Số CCCD:</strong> {employee.CitizenId}</td><td><strong>Khối nhân sự:</strong> {employee.EmployeeType}</td></tr>");
        sb.AppendLine($"    <tr><td><strong>Số ngày công chuẩn:</strong> {payslip.StandardWorkDays}</td><td><strong>Số ngày công thực tế:</strong> {payslip.ActualWorkDays}</td></tr>");
        sb.AppendLine("  </table>");

        sb.AppendLine("  <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px; font-size: 14px;'>");
        sb.AppendLine("    <thead><tr style='background-color: #0047BA; color: white;'>");
        sb.AppendLine("      <th style='padding: 8px; text-align: left;'>MỤC THU NHẬP</th><th style='padding: 8px; text-align: right;'>SỐ TIỀN (VNĐ)</th>");
        sb.AppendLine("      <th style='padding: 8px; text-align: left;'>MỤC KHẤU TRỪ</th><th style='padding: 8px; text-align: right;'>SỐ TIỀN (VNĐ)</th>");
        sb.AppendLine("    </tr></thead><tbody>");

        sb.AppendLine($"    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>Lương cơ bản</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.BaseSalary:N0}</td>");
        sb.AppendLine($"        <td style='padding: 6px; border-bottom: 1px solid #eee;'>Bảo hiểm Xã hội (8%)</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.SocialInsuranceDeduction:N0}</td></tr>");

        sb.AppendLine($"    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>Phụ cấp chức vụ / chức danh</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.Allowance:N0}</td>");
        sb.AppendLine($"        <td style='padding: 6px; border-bottom: 1px solid #eee;'>Bảo hiểm Y tế (1.5%)</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.HealthInsuranceDeduction:N0}</td></tr>");

        sb.AppendLine($"    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>Lương ngoài giờ (OT: {payslip.OvertimeHours}h)</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.OvertimePay:N0}</td>");
        sb.AppendLine($"        <td style='padding: 6px; border-bottom: 1px solid #eee;'>Bảo hiểm Thất nghiệp (1%)</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.UnemploymentInsuranceDeduction:N0}</td></tr>");

        sb.AppendLine($"    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>Thưởng hiệu suất KPI</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.KpiBonus:N0}</td>");
        sb.AppendLine($"        <td style='padding: 6px; border-bottom: 1px solid #eee;'>Thuế TNCN tạm khấu trừ</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.PersonalIncomeTax:N0}</td></tr>");

        sb.AppendLine($"    <tr><td style='padding: 6px; border-bottom: 1px solid #eee;'>Thưởng khác</td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee;'>{payslip.OtherBonuses:N0}</td>");
        sb.AppendLine($"        <td style='padding: 6px; border-bottom: 1px solid #eee;'><strong>TỔNG CÁC KHOẢN TRÍCH NỘP</strong></td><td style='padding: 6px; text-align: right; border-bottom: 1px solid #eee; color: #D32F2F;'><strong>{payslip.TotalDeductions:N0}</strong></td></tr>");

        sb.AppendLine("    <tr style='background-color: #f5f5f5;'>");
        sb.AppendLine($"      <td style='padding: 8px; font-weight: bold;'>TỔNG THU NHẬP (GROSS)</td><td style='padding: 8px; text-align: right; font-weight: bold; color: #0047BA;'>{payslip.GrossSalary:N0}</td>");
        sb.AppendLine($"      <td style='padding: 8px; font-weight: bold; color: #2E7D32;'>THỰC LĨNH CHUYỂN KHOẢN (NET)</td><td style='padding: 8px; text-align: right; font-weight: bold; color: #2E7D32; font-size: 16px;'>{payslip.NetSalary:N0} VNĐ</td>");
        sb.AppendLine("    </tr></tbody></table>");

        sb.AppendLine($"  <div style='font-size: 12px; color: #777; text-align: right;'>Mã tham chiếu: {payslip.TransactionReference ?? "CHUA_THANH_TOAN"} | Trạng thái: {payslip.Status}</div>");
        sb.AppendLine("</div>");

        return sb.ToString();
    }
}
