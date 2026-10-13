using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Services.Payroll;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Trình tạo phiếu lương PDF chuẩn doanh nghiệp (A4) sử dụng QuestPDF.
/// Đảm bảo giao diện hiện đại, hiển thị rõ ràng FormulaTrace rút gọn,
/// thông tin bảo hiểm, thuế TNCN và thực lĩnh của cán bộ nhân viên Vinamilk.
/// </summary>
public class QuestPdfPayslipGenerator
{
    private static readonly CultureInfo ViCulture = CultureInfo.GetCultureInfo("vi-VN");
    private const string NavyColor = "#002D72";
    private const string LightBlueBg = "#F0F4F8";
    private const string BorderColor = "#D0D7DE";

    static QuestPdfPayslipGenerator()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] GeneratePayslipPdf(PaySlip payslip, Employee employee, OrganizationalUnit unit, string? password = null)
    {
        ArgumentNullException.ThrowIfNull(payslip);
        ArgumentNullException.ThrowIfNull(employee);

        var lines = DeserializeFormulaLines(payslip.FormulaTraceJson);

        var document = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(24, Unit.Point);
                page.DefaultTextStyle(x => x.FontSize(8.5f).FontFamily("Arial"));

                page.Header().Element(header => ComposeHeader(header, payslip, employee, unit));
                page.Content().Element(content => ComposeContent(content, payslip, employee, lines));
                page.Footer().Element(ComposeFooter);
            });
        });

        // Xuất PDF dạng mảng bytes
        return document.GeneratePdf();
    }

    private static void ComposeHeader(IContainer container, PaySlip payslip, Employee employee, OrganizationalUnit? unit)
    {
        container.Column(col =>
        {
            col.Item().Row(row =>
            {
                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Text("CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)").Bold().FontSize(10.5f).FontColor(NavyColor);
                    c.Item().Text("Số 10 Tân Trào, P. Tân Phú, Q. 7, TP. Hồ Chí Minh | www.vinamilk.com.vn").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                });
                row.RelativeItem(1).AlignRight().Column(c =>
                {
                    c.Item().Text("MẪU SỐ: VNM-HRM-PL01").FontSize(7.5f).FontColor(Colors.Grey.Medium);
                    c.Item().Text($"Kỳ lương: {payslip.PayrollPeriod}").Bold().FontSize(9.5f).FontColor(NavyColor);
                });
            });

            col.Item().PaddingTop(8).LineHorizontal(1).LineColor(NavyColor);

            col.Item().PaddingTop(6).PaddingBottom(6).AlignCenter().Text("PHIẾU LƯƠNG NHÂN VIÊN (PAYSLIP)").Bold().FontSize(13).FontColor(NavyColor);

            // Bảng thông tin nhân sự
            col.Item().Border(1).BorderColor(BorderColor).Background(LightBlueBg).Padding(6).Row(row =>
            {
                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Mã NV: ").Bold(); t.Span(employee.EmployeeCode); });
                    c.Item().Text(t => { t.Span("Họ và tên: ").Bold(); t.Span(employee.FullName); });
                    c.Item().Text(t => { t.Span("Chức danh: ").Bold(); t.Span(employee.StandardJobTitle?.JobTitleName ?? "Cán bộ nhân viên"); });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Đơn vị: ").Bold(); t.Span(unit?.UnitName ?? employee.OrganizationalUnit?.UnitName ?? "Tập đoàn Vinamilk"); });
                    c.Item().Text(t => { t.Span("Công chuẩn: ").Bold(); t.Span($"{payslip.StandardWorkDays:N1} ngày"); });
                    c.Item().Text(t => { t.Span("Công thực tế: ").Bold(); t.Span($"{payslip.ActualWorkDays:N1} ngày"); });
                });

                row.RelativeItem().Column(c =>
                {
                    c.Item().Text(t => { t.Span("Số người phụ thuộc: ").Bold(); t.Span($"{payslip.DependentCount} người"); });
                    c.Item().Text(t => { t.Span("Mã số thuế: ").Bold(); t.Span(employee.TaxCode ?? "Chưa đăng ký"); });
                    c.Item().Text(t => { t.Span("Tài khoản: ").Bold(); t.Span(employee.BankAccountNumber ?? "ATM"); });
                });
            });
        });
    }

    private static void ComposeContent(IContainer container, PaySlip payslip, Employee employee, List<PayrollLine> lines)
    {
        container.PaddingTop(8).Column(col =>
        {
            // Bảng chi tiết lương
            col.Item().Table(table =>
            {
                table.ColumnsDefinition(columns =>
                {
                    columns.ConstantColumn(30);  // STT
                    columns.RelativeColumn(3);   // Khoản mục
                    columns.RelativeColumn(4);   // Công thức / Diễn giải
                    columns.RelativeColumn(2);   // Số tiền
                });

                // Tiêu đề bảng
                table.Header(header =>
                {
                    header.Cell().Background(NavyColor).Padding(4).AlignCenter().Text("STT").Bold().FontColor(Colors.White);
                    header.Cell().Background(NavyColor).Padding(4).Text("Khoản mục thu nhập & khấu trừ").Bold().FontColor(Colors.White);
                    header.Cell().Background(NavyColor).Padding(4).Text("Công thức tính (Formula Trace)").Bold().FontColor(Colors.White);
                    header.Cell().Background(NavyColor).Padding(4).AlignRight().Text("Số tiền (VND)").Bold().FontColor(Colors.White);
                });

                int stt = 1;

                // MỤC I: CÁC KHOẢN THU NHẬP (GROSS)
                table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3).Padding(3).Text("I. CÁC KHOẢN THU NHẬP (EARNINGS)").Bold().FontColor(NavyColor);

                if (lines.Count > 0)
                {
                    foreach (var line in lines)
                    {
                        if (line.Amount > 0 && (line.IsTaxable || line.IsInsurable || line.Code.StartsWith("P") || line.Code.StartsWith("OT") || line.Code.StartsWith("LUONG") || line.Code.StartsWith("PHU_CAP")))
                        {
                            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(line.Name);
                            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text(line.FormulaTrace).FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                            table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(line.Amount));
                        }
                    }
                }
                else
                {
                    // Fallback nếu không có lines
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Lương cơ bản & Phụ cấp");
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text($"{FormatMoney(payslip.BaseSalary + payslip.Allowance)}");
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(payslip.GrossSalary));
                }

                // Dòng Tổng Gross
                table.Cell().ColumnSpan(3).Background(LightBlueBg).Padding(4).Text("TỔNG THU NHẬP (GROSS SALARY)").Bold();
                table.Cell().Background(LightBlueBg).Padding(4).AlignRight().Text(FormatMoney(payslip.GrossSalary)).Bold().FontColor(NavyColor);

                // MỤC II: CÁC KHOẢN TRÍCH NỘP & KHẤU TRỪ
                table.Cell().ColumnSpan(4).Background(Colors.Grey.Lighten3).Padding(3).Text("II. CÁC KHOẢN TRÍCH NỘP & KHẤU TRỪ (DEDUCTIONS)").Bold().FontColor(NavyColor);

                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Bảo hiểm xã hội người lao động (8%)");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Lương đóng BH × 8%").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(payslip.SocialInsuranceDeduction));

                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Bảo hiểm y tế người lao động (1.5%)");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Lương đóng BH × 1.5%").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(payslip.HealthInsuranceDeduction));

                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Bảo hiểm thất nghiệp người lao động (1%)");
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Lương đóng BHTN × 1%").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(payslip.UnemploymentInsuranceDeduction));

                if (payslip.PersonalIncomeTax > 0)
                {
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignCenter().Text($"{stt++}");
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text("Thuế thu nhập cá nhân (PIT)");
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).Text($"TNTT: {FormatMoney(payslip.TaxableIncome)} (Biểu lũy tiến)").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                    table.Cell().BorderBottom(1).BorderColor(BorderColor).Padding(3).AlignRight().Text(FormatMoney(payslip.PersonalIncomeTax));
                }

                // Dòng Tổng Giảm trừ
                table.Cell().ColumnSpan(3).Background(LightBlueBg).Padding(4).Text("TỔNG CÁC KHOẢN KHẤU TRỪ (TOTAL DEDUCTIONS)").Bold();
                table.Cell().Background(LightBlueBg).Padding(4).AlignRight().Text(FormatMoney(payslip.TotalDeductions)).Bold();
            });

            // KHỐI THỰC LĨNH NỔI BẬT (NET SALARY BOX)
            col.Item().PaddingTop(8).Border(1.5f).BorderColor(NavyColor).Background(LightBlueBg).Padding(8).Row(row =>
            {
                row.RelativeItem(2).Column(c =>
                {
                    c.Item().Text("THỰC LĨNH CHUYỂN KHOẢN (NET SALARY)").Bold().FontSize(11).FontColor(NavyColor);
                    c.Item().Text($"Giảm trừ bản thân: {FormatMoney(payslip.PersonalDeduction)} đ | Người phụ thuộc: {FormatMoney(payslip.DependentDeduction)} đ").FontSize(7.5f).FontColor(Colors.Grey.Darken2);
                });
                row.RelativeItem(1).AlignRight().Text($"{FormatMoney(payslip.NetSalary)} VND").Bold().FontSize(13).FontColor(NavyColor);
            });

            // Chữ ký xác nhận
            col.Item().PaddingTop(12).Row(row =>
            {
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Người lập biểu").Bold();
                    c.Item().Text("(Kế toán tiền lương)").FontSize(7.5f).Italic();
                });
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Kế toán trưởng").Bold();
                    c.Item().Text("(Ký, họ tên)").FontSize(7.5f).Italic();
                });
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Giám đốc Nhân sự").Bold();
                    c.Item().Text("(Ký, họ tên)").FontSize(7.5f).Italic();
                });
                row.RelativeItem().AlignCenter().Column(c =>
                {
                    c.Item().Text("Người nhận lương").Bold();
                    c.Item().Text("(Ký, ghi rõ họ tên)").FontSize(7.5f).Italic();
                });
            });
        });
    }

    private static void ComposeFooter(IContainer container)
    {
        container.BorderTop(0.5f).BorderColor(BorderColor).PaddingTop(4).Row(row =>
        {
            row.RelativeItem().Text("Tài liệu bảo mật nội bộ Vinamilk. Mọi thắc mắc liên hệ Phòng Nhân sự trong 03 ngày làm việc.").FontSize(7).FontColor(Colors.Grey.Medium);
            row.RelativeItem().AlignRight().Text(t =>
            {
                t.Span("Trang ");
                t.CurrentPageNumber();
                t.Span(" / ");
                t.TotalPages();
            });
        });
    }

    private static List<PayrollLine> DeserializeFormulaLines(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return new();
        try
        {
            return JsonSerializer.Deserialize<List<PayrollLine>>(json) ?? new();
        }
        catch
        {
            return new();
        }
    }

    private static string FormatMoney(decimal amount) => amount.ToString("#,##0", ViCulture);
}
