using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Interfaces.Printing;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Lớp cơ sở trừu tượng cho hệ thống in phiếu lương đa hình Vinamilk.
/// Đảm bảo tính nhất quán về chuẩn nhận diện thương hiệu, luật thuế TNCN & bảo hiểm xã hội Việt Nam.
/// </summary>
public abstract class BasePayslipPrinter : IPayslipPrinter
{
    public virtual byte[] PrintPayslipToPdf(PaySlip payslip, Employee employee, OrganizationalUnit unit)
    {
        string html = GenerateHtmlPreview(payslip, employee, unit);
        return Encoding.UTF8.GetBytes(html);
    }

    public virtual byte[] PrintBatchPayslipsToPdf(IEnumerable<(PaySlip Payslip, Employee Employee, OrganizationalUnit Unit)> payslipBatch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
        sb.AppendLine("@media print { .page-break { page-break-after: always; } }");
        sb.AppendLine("</style></head><body>");

        foreach (var item in payslipBatch)
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

        sb.AppendLine("<div class='vnm-payslip-wrapper' style='font-family: Arial, Helvetica, sans-serif; max-width: 850px; margin: 20px auto; padding: 30px; background: #ffffff; border: 1px solid #dcdfe6; border-radius: 12px; box-shadow: 0 4px 20px rgba(0,0,0,0.06); color: #2c3e50;'>");

        // Header Công ty
        sb.AppendLine("  <div style='display: flex; justify-content: space-between; align-items: center; border-bottom: 3px solid #0047BA; padding-bottom: 16px; margin-bottom: 24px;'>");
        sb.AppendLine("    <div>");
        sb.AppendLine("      <div style='color: #0047BA; font-size: 20px; font-weight: 800; letter-spacing: 0.5px;'>CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)</div>");
        sb.AppendLine($"      <div style='font-size: 13px; color: #606266; margin-top: 4px;'>Đơn vị: <strong>{unit?.UnitName ?? "Tập đoàn Vinamilk"}</strong> | Mã ĐV: <strong>{unit?.UnitCode ?? "VNM-HQ"}</strong></div>");
        sb.AppendLine($"      <div style='font-size: 12px; color: #909399;'>Địa chỉ: Vinamilk Tower, 10 Tân Trào, P. Tân Phú, Quận 7, TP. HCM</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("    <div style='text-align: right;'>");
        sb.AppendLine($"      <span style='background: #E8F0FE; color: #0047BA; font-size: 11px; font-weight: 700; padding: 4px 10px; border-radius: 20px;'>{GetPrinterTitle()}</span>");
        sb.AppendLine($"      <div style='font-size: 13px; font-weight: 600; color: #303133; margin-top: 6px;'>KỲ LƯƠNG: {payslip.PayrollPeriod}</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");

        // Tiêu đề Phiếu lương
        sb.AppendLine("  <div style='text-align: center; margin-bottom: 24px;'>");
        sb.AppendLine($"    <h2 style='color: #0047BA; margin: 0; font-size: 22px; text-transform: uppercase;'>PHIẾU THANH TOÁN TIỀN LƯƠNG & THU NHẬP</h2>");
        sb.AppendLine($"    <div style='font-size: 13px; color: #606266; font-style: italic; margin-top: 4px;'>Tuân thủ Luật Lao động & Luật Thuế TNCN Việt Nam (Kỳ tháng {payslip.PayrollPeriod})</div>");
        sb.AppendLine("  </div>");

        // Thông tin người lao động
        sb.AppendLine("  <table style='width: 100%; border-collapse: collapse; margin-bottom: 20px; background: #f8fafc; border-radius: 8px; padding: 12px; font-size: 13.5px;'>");
        sb.AppendLine("    <tr>");
        sb.AppendLine($"      <td style='padding: 8px 12px; width: 25%;'><strong>Mã nhân viên:</strong></td><td style='padding: 8px 12px; width: 25%; color: #0047BA; font-weight: bold;'>{employee.EmployeeCode}</td>");
        sb.AppendLine($"      <td style='padding: 8px 12px; width: 25%;'><strong>Họ và tên:</strong></td><td style='padding: 8px 12px; width: 25%; font-weight: bold;'>{employee.FullName}</td>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("    <tr>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Số CCCD:</strong></td><td style='padding: 8px 12px;'>{employee.CitizenId}</td>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Khối nhân sự:</strong></td><td style='padding: 8px 12px;'><span style='background: #e2e8f0; padding: 2px 8px; border-radius: 4px; font-size: 12px;'>{employee.EmployeeType}</span></td>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("    <tr>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Ngày công chuẩn:</strong></td><td style='padding: 8px 12px;'>{payslip.StandardWorkDays} ngày</td>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Ngày công thực tế:</strong></td><td style='padding: 8px 12px; color: #16a34a; font-weight: bold;'>{payslip.ActualWorkDays} ngày</td>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("    <tr>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Người phụ thuộc:</strong></td><td style='padding: 8px 12px;'>{payslip.DependentCount} người (Giảm trừ: {payslip.DependentDeduction:N0} đ)</td>");
        sb.AppendLine($"      <td style='padding: 8px 12px;'><strong>Quỹ phép năm:</strong></td><td style='padding: 8px 12px;'>{employee.AnnualLeaveBalance} ngày</td>");
        sb.AppendLine("    </tr>");
        sb.AppendLine("  </table>");

        // Chi tiết nghiệp vụ đặc thù phân hệ (ĐA HÌNH)
        RenderSpecificMetrics(payslip, employee, sb);

        // Bảng thu nhập & khấu trừ 2 cột chuẩn
        sb.AppendLine("  <table style='width: 100%; border-collapse: collapse; margin-bottom: 24px; font-size: 13.5px;'>");
        sb.AppendLine("    <thead>");
        sb.AppendLine("      <tr style='background: #0047BA; color: #ffffff;'>");
        sb.AppendLine("        <th style='padding: 10px 14px; text-align: left; width: 30%; border-top-left-radius: 6px;'>I. CÁC KHOẢN THU NHẬP</th>");
        sb.AppendLine("        <th style='padding: 10px 14px; text-align: right; width: 20%;'>SỐ TIỀN (VNĐ)</th>");
        sb.AppendLine("        <th style='padding: 10px 14px; text-align: left; width: 30%;'>II. CÁC KHOẢN KHẤU TRỪ</th>");
        sb.AppendLine("        <th style='padding: 10px 14px; text-align: right; width: 20%; border-top-right-radius: 6px;'>SỐ TIỀN (VNĐ)</th>");
        sb.AppendLine("      </tr>");
        sb.AppendLine("    </thead>");
        sb.AppendLine("    <tbody>");

        // Dòng 1: Lương cơ bản & BHXH
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>1. Lương cơ bản theo hợp đồng</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; font-weight: 600;'>{payslip.BaseSalary:N0}</td>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>1. Bảo hiểm Xã hội (BHXH 8%)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #dc2626;'>{payslip.SocialInsuranceDeduction:N0}</td>");
        sb.AppendLine("      </tr>");

        // Dòng 2: Phụ cấp chức danh & BHYT
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>2. Phụ cấp chức vụ / chức danh</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7;'>{payslip.Allowance:N0}</td>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>2. Bảo hiểm Y tế (BHYT 1.5%)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #dc2626;'>{payslip.HealthInsuranceDeduction:N0}</td>");
        sb.AppendLine("      </tr>");

        // Phần đặc thù ĐA HÌNH các dòng thu nhập phụ cấp & OT
        RenderSpecificEarningsAndAllowances(payslip, employee, sb);

        // Dòng BHTN & Giảm trừ gia cảnh info
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>Thưởng hiệu suất KPI (SLA)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #16a34a; font-weight: 600;'>{payslip.KpiBonus:N0}</td>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>3. Bảo hiểm Thất nghiệp (BHTN 1%)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #dc2626;'>{payslip.UnemploymentInsuranceDeduction:N0}</td>");
        sb.AppendLine("      </tr>");

        // Dòng Thuế TNCN
        sb.AppendLine("      <tr>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>Phúc lợi hiện vật Vinamilk</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7;'>{payslip.OtherBonuses:N0}</td>");
        sb.AppendLine($"        <td style='padding: 9px 14px; border-bottom: 1px solid #edf2f7;'>4. Thuế TNCN lũy tiến (7 bậc)</td><td style='padding: 9px 14px; text-align: right; border-bottom: 1px solid #edf2f7; color: #dc2626; font-weight: 600;'>{payslip.PersonalIncomeTax:N0}</td>");
        sb.AppendLine("      </tr>");

        // Dòng Giảm trừ gia cảnh tổng quan (để minh bạch thuế)
        sb.AppendLine("      <tr style='background: #fdf2f2; font-size: 12px; color: #666;'>");
        sb.AppendLine($"        <td colspan='2' style='padding: 6px 14px;'><i>(Thu nhập chịu thuế trước giảm trừ: {payslip.GrossSalary:N0} đ)</i></td>");
        sb.AppendLine($"        <td colspan='2' style='padding: 6px 14px; text-align: right;'><i>(Giảm trừ bản thân 11M + {payslip.DependentCount} người PT: {(payslip.PersonalDeduction + payslip.DependentDeduction):N0} đ)</i></td>");
        sb.AppendLine("      </tr>");

        // Tổng kết Gross & Deductions
        sb.AppendLine("      <tr style='background: #f1f5f9; font-weight: bold;'>");
        sb.AppendLine($"        <td style='padding: 10px 14px; color: #0047BA;'>TỔNG THU NHẬP (GROSS)</td><td style='padding: 10px 14px; text-align: right; color: #0047BA; font-size: 15px;'>{payslip.GrossSalary:N0}</td>");
        sb.AppendLine($"        <td style='padding: 10px 14px; color: #dc2626;'>TỔNG CÁC KHOẢN TRÍCH NỘP</td><td style='padding: 10px 14px; text-align: right; color: #dc2626; font-size: 15px;'>{payslip.TotalDeductions:N0}</td>");
        sb.AppendLine("      </tr>");

        // Dòng THỰC LĨNH NET
        sb.AppendLine("      <tr style='background: linear-gradient(135deg, #E8F5E9 0%, #C8E6C9 100%);'>");
        sb.AppendLine("        <td colspan='2' style='padding: 14px; font-size: 16px; font-weight: 800; color: #166534;'>THỰC LĨNH CHUYỂN KHOẢN (NET SALARY):</td>");
        sb.AppendLine($"        <td colspan='2' style='padding: 14px; text-align: right; font-size: 20px; font-weight: 800; color: #15803d;'>{payslip.NetSalary:N0} VNĐ</td>");
        sb.AppendLine("      </tr>");

        sb.AppendLine("    </tbody>");
        sb.AppendLine("  </table>");

        // Footer & Digital Signature
        RenderFooterAndSignature(payslip, sb);

        sb.AppendLine("</div>");

        return sb.ToString();
    }

    protected abstract string GetPrinterTitle();
    protected abstract void RenderSpecificEarningsAndAllowances(PaySlip payslip, Employee employee, StringBuilder sb);
    protected abstract void RenderSpecificMetrics(PaySlip payslip, Employee employee, StringBuilder sb);

    private static void RenderFooterAndSignature(PaySlip payslip, StringBuilder sb)
    {
        sb.AppendLine("  <div style='display: flex; justify-content: space-between; align-items: flex-end; margin-top: 25px; padding-top: 15px; border-top: 1px dashed #cbd5e1;'>");
        sb.AppendLine("    <div style='font-size: 12px; color: #64748b; line-height: 1.6;'>");
        sb.AppendLine($"      <div>Mã giao dịch chi trả: <strong>{(string.IsNullOrEmpty(payslip.TransactionReference) ? "VNM-PAYROLL-PENDING" : payslip.TransactionReference)}</strong></div>");
        sb.AppendLine($"      <div>Trạng thái: <span style='color: #16a34a; font-weight: bold;'>{payslip.Status}</span> | Ngày thanh toán: {(payslip.PaymentDate.HasValue ? payslip.PaymentDate.Value.ToString("dd/MM/yyyy") : "Đang chờ giải ngân")}</div>");
        sb.AppendLine("      <div>Ngân hàng thụ hưởng: <strong>Vietcombank / BIDV Vinamilk Corporate Payroll</strong></div>");
        sb.AppendLine("    </div>");

        // Mộc điện tử SVG
        sb.AppendLine("    <div style='text-align: center;'>");
        sb.AppendLine("      <div style='border: 2px solid #dc2626; border-radius: 50%; width: 100px; height: 100px; display: flex; flex-direction: column; align-items: center; justify-content: center; color: #dc2626; margin: 0 auto; transform: rotate(-5deg); box-shadow: 0 0 5px rgba(220,38,38,0.2);'>");
        sb.AppendLine("        <div style='font-size: 8px; font-weight: bold; text-transform: uppercase;'>VINAMILK CORP</div>");
        sb.AppendLine("        <div style='font-size: 14px;'>★ ★ ★</div>");
        sb.AppendLine("        <div style='font-size: 8px; font-weight: bold; text-transform: uppercase;'>ĐÃ KÝ ĐIỆN TỬ</div>");
        sb.AppendLine("        <div style='font-size: 7px;'>C&B PAYROLL</div>");
        sb.AppendLine("      </div>");
        sb.AppendLine("      <div style='font-size: 11px; color: #475569; margin-top: 4px; font-weight: 600;'>Xác nhận Ban Tài chính & C&B</div>");
        sb.AppendLine("    </div>");
        sb.AppendLine("  </div>");
    }
}
