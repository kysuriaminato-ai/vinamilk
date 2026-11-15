using System.Security.Cryptography;
using System.Text.Json;
using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using PdfSharp.Pdf.IO;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Vinamilk.HRM.Application.Common.Helpers;
using Vinamilk.HRM.Application.Common.Interfaces;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Core;
using Vinamilk.HRM.Domain.Entities.Payroll;

namespace Vinamilk.HRM.Infrastructure.Services;

public sealed class PayrollDocumentService : IPayrollDocumentService
{
    private readonly IPiiEncryptionService _encryptionService;

    public PayrollDocumentService(
        IPiiEncryptionService encryptionService,
        IConfiguration? configuration = null)
    {
        _encryptionService = encryptionService;
        if (configuration is not null)
        {
            QuestPDF.Settings.License = DependencyInjection.ResolveQuestPdfLicense(configuration);
        }
    }

    public static string GenerateRandomOwnerPassword() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    public PayslipPdfResult GeneratePayslipPdf(
        PayrollRecord record,
        Employee employee,
        PayrollPeriod period,
        IReadOnlyList<PitBracket> pitBrackets,
        bool applyPassword = true)
    {
        var plainCccd = _encryptionService.Decrypt(employee.CitizenIdEncrypted);
        var plainBankAcc = _encryptionService.Decrypt(employee.BankAccountEncrypted);
        var maskedAcc = string.IsNullOrWhiteSpace(plainBankAcc)
            ? "Chưa cập nhật"
            : (plainBankAcc.Length > 4 ? $"****{plainBankAcc[^4..]}" : plainBankAcc);

        var pitSlices = ResolvePitSlices(record, pitBrackets);
        var wageRegion = employee.WageRegion ?? employee.OrganizationUnit?.WageRegion ?? 1;

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)").Bold().FontSize(11).FontColor("#003DA5");
                            left.Item().Text("Hệ thống Quản trị Nguồn nhân lực Enterprise HRM").FontSize(8.5f).FontColor("#64748b");
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text($"PHIẾU LƯƠNG KỲ {period.PeriodCode}").Bold().FontSize(11);
                            right.Item().Text($"Từ {period.StartDate:dd/MM/yyyy} đến {period.EndDate:dd/MM/yyyy} | Trạng thái: {period.Status}").FontSize(8.5f);
                        });
                    });
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#003DA5");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Border(0.75f).BorderColor("#cbd5e1").Padding(8).Column(info =>
                    {
                        info.Spacing(3);
                        info.Item().Text($"Nhân viên: {employee.FullName.ToUpperInvariant()}   |   Mã NV: {employee.EmployeeCode}").Bold();
                        info.Item().Text($"Đơn vị: {employee.OrganizationUnit?.UnitName ?? "-"}   |   Chức danh: {employee.Position?.PositionName ?? "-"}   |   Vùng BHTN: Vùng {wageRegion}");
                        info.Item().Text($"Số người phụ thuộc giảm trừ: {record.DependentCount}   |   Ngân hàng: {employee.BankName ?? "-"} ({maskedAcc})");
                    });

                    // Bảng I: Chi tiết thu nhập & Công
                    col.Item().Text("I. CHI TIẾT NGÀY CÔNG & THU NHẬP (EARNINGS)").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });

                        AddTableHeader(table, "Khoản mục", "Diễn giải / Số lượng", "Số tiền (VNĐ)");
                        AddTableRow(table, "1. Lương hợp đồng cơ bản", $"Công chuẩn: {record.StandardWorkdays:0.#} ngày", $"{record.ContractBaseSalary:N0}");
                        AddTableRow(table, "2. Lương thực tế theo công", $"Công hưởng lương: {record.PayableWorkdays:0.#} ngày", $"{record.ActualBaseSalary:N0}");
                        AddTableRow(table, "3. Phụ cấp chức vụ", "Theo quyết định bổ nhiệm", $"{record.PositionAllowance:N0}");
                        AddTableRow(table, "4. Phụ cấp khác", "Ăn trưa, xăng xe, điện thoại...", $"{record.OtherAllowance:N0}");
                        AddTableRow(table, "5. Tiền làm thêm giờ (OT)", $"OT150: {record.OtHours150:0.#}h | OT200: {record.OtHours200:0.#}h | OT300: {record.OtHours300:0.#}h", $"{record.OvertimePay:N0}");
                        AddTableRow(table, "6. Thưởng KPI / Hiệu suất", "Thưởng định kỳ / KPI", $"{record.KpiBonus:N0}");
                        AddTableTotalRow(table, "TỔNG THU NHẬP (GROSS)", $"{record.GrossIncome:N0} đ");
                    });

                    // Bảng II: Bảo hiểm & Giảm trừ
                    col.Item().Text("II. BẢO HIỂM BẮT BUỘC (NLĐ ĐÓNG) & GIẢM TRỪ GIA CẢNH").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(3);
                            c.RelativeColumn(3);
                        });

                        AddTableHeader(table, "Khoản khấu trừ / Giảm trừ", "Cơ sở tính", "Số tiền (VNĐ)");
                        AddTableRow(table, "1. BHXH NLĐ đóng (8%)", $"Quỹ lương đóng BHXH: {record.InsuranceSalaryBase:N0} đ", $"{record.EmployeeSocialInsurance:N0}");
                        AddTableRow(table, "2. BHYT NLĐ đóng (1.5%)", $"Quỹ lương đóng BHYT: {record.InsuranceSalaryBase:N0} đ", $"{record.EmployeeHealthInsurance:N0}");
                        AddTableRow(table, "3. BHTN NLĐ đóng (1%)", $"Quỹ lương đóng BHTN (Vùng {wageRegion}): {record.UnemploymentInsuranceBase:N0} đ", $"{record.EmployeeUnemploymentInsurance:N0}");
                        AddTableTotalRow(table, "TỔNG BẢO HIỂM BẮT BUỘC NLĐ (10.5%)", $"{record.TotalEmployeeInsurance:N0} đ");
                        AddTableRow(table, "4. Thu nhập chịu thuế (Gross - BH NLĐ)", "-", $"{record.TaxableIncome:N0}");
                        AddTableRow(table, "5. Giảm trừ gia cảnh bản thân", "Theo hiệu lực kỳ lương", $"{record.SelfDeductionAmount:N0}");
                        AddTableRow(table, "6. Giảm trừ người phụ thuộc", $"{record.DependentCount} người phụ thuộc", $"{record.DependentDeductionAmount:N0}");
                        AddTableTotalRow(table, "THU NHẬP TÍNH THUẾ (ASSESSABLE INCOME)", $"{record.AssessableIncome:N0} đ");
                    });

                    // Bảng III: Chi tiết Thuế TNCN từng bậc
                    col.Item().Text($"III. CHI TIẾT THUẾ TNCN LŨY TIẾN ({pitSlices.Count} BẬC HIỆU LỰC)").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(4);
                            c.RelativeColumn(1.5f);
                            c.RelativeColumn(2.5f);
                            c.RelativeColumn(2.5f);
                        });

                        AddTableHeader(table, "Bậc", "Ngưỡng thu nhập tính thuế", "Thuế suất", "TN tính vào bậc", "Tiền thuế (VNĐ)");
                        foreach (var s in pitSlices)
                        {
                            var rangeText = s.UpperBound.HasValue
                                ? $"Trên {s.LowerBound:N0} đến {s.UpperBound.Value:N0}"
                                : $"Trên {s.LowerBound:N0}";
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text($"Bậc {s.BracketLevel}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(rangeText);
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{s.TaxRate * 100m:0.#}%");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{s.IncomeInBracket:N0}");
                            table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text($"{s.TaxAmount:N0}");
                        }
                        AddTableTotalRowSpan(table, 5, "TỔNG THUẾ TNCN KHẤU TRỪ", $"{record.PersonalIncomeTax:N0} đ");
                    });

                    // Bảng IV: Thực lĩnh
                    col.Item().Background("#eef4ff").Border(1).BorderColor("#003DA5").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("THỰC LĨNH CHUYỂN KHOẢN (NET SALARY)").Bold().FontSize(11).FontColor("#003DA5");
                            c.Item().Text($"Khấu trừ kỷ luật / khác: {record.DisciplineDeduction:N0} đ | Trạng thái chi trả: {record.PaymentStatus}").FontSize(8.5f);
                        });
                        r.AutoItem().AlignMiddle().Text($"{record.NetSalary:N0} VNĐ").Bold().FontSize(15).FontColor("#0f766e");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Tài liệu bảo mật nội bộ Vinamilk HRM • Phát hành cho {employee.EmployeeCode} ({period.PeriodCode}) • Trang ");
                    x.CurrentPageNumber();
                });
            });
        });

        var rawBytes = doc.GeneratePdf();
        var fileName = $"PhieuLuong_{employee.EmployeeCode}_{period.PeriodCode}.pdf";

        if (!applyPassword)
            return new PayslipPdfResult(rawBytes, fileName, "Không đặt mật khẩu", false);

        var password = VietnameseTextHelper.ExtractPayslipPassword(plainCccd, employee.EmployeeCode, employee.DateOfBirth);
        var protectedBytes = ProtectPdfWithPassword(rawBytes, password);

        return new PayslipPdfResult(
            protectedBytes,
            fileName,
            "Mật khẩu mở file là 6 số cuối CCCD (hoặc ngày sinh ddMMyy nếu chưa cập nhật CCCD)",
            true);
    }

    public byte[] ExportDepartmentSummaryExcel(PayrollDepartmentSummaryReportDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("TongHopLuong_PhongBan");

        ws.Cell(1, 1).Value = $"BÁO CÁO TỔNG HỢP QUỸ LƯƠNG, BHXH & THUẾ TNCN — {report.PeriodTitle.ToUpperInvariant()} ({report.PeriodCode})";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;
        ws.Cell(2, 1).Value = $"Ngày chốt hiệu lực: {report.EndDate:dd/MM/yyyy} | Trạng thái: {report.Status} | Tổng nhân sự: {report.TotalEmployees}";

        string[] headers =
        [
            "STT", "Mã ĐV", "Tên đơn vị / Phòng ban", "Số NS",
            "Lương & Phụ cấp", "OT & Thưởng KPI", "Tổng quỹ lương (Gross)",
            "Tổng BH NLĐ đóng (10.5%)", "Tổng BH DN đóng (21.5%)", "KPCĐ DN (2%)",
            "Tổng Thuế TNCN khấu trừ", "Tổng Thực lĩnh (Net)"
        ];

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(4, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#00338D");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int row = 5;
        for (int i = 0; i < report.Rows.Count; i++)
        {
            var r = report.Rows[i];
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.UnitCode;
            ws.Cell(row, 3).Value = r.UnitName;
            ws.Cell(row, 4).Value = r.Headcount;
            ws.Cell(row, 5).Value = r.TotalActualAndAllowances;
            ws.Cell(row, 6).Value = r.TotalOtAndBonus;
            ws.Cell(row, 7).Value = r.TotalGross;
            ws.Cell(row, 8).Value = r.TotalEmployeeInsurance;
            ws.Cell(row, 9).Value = r.TotalCompanyInsurance;
            ws.Cell(row, 10).Value = r.TotalCompanyUnionFee;
            ws.Cell(row, 11).Value = r.TotalPit;
            ws.Cell(row, 12).Value = r.TotalNet;

            for (int c = 5; c <= 12; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";

            row++;
        }

        // Dòng TỔNG CỘNG
        ws.Cell(row, 3).Value = "TỔNG CỘNG TOÀN KỲ";
        ws.Cell(row, 4).Value = report.TotalEmployees;
        ws.Cell(row, 5).Value = report.Rows.Sum(x => x.TotalActualAndAllowances);
        ws.Cell(row, 6).Value = report.Rows.Sum(x => x.TotalOtAndBonus);
        ws.Cell(row, 7).Value = report.GrandTotalGross;
        ws.Cell(row, 8).Value = report.GrandTotalEmployeeInsurance;
        ws.Cell(row, 9).Value = report.GrandTotalCompanyInsurance;
        ws.Cell(row, 10).Value = report.GrandTotalUnionFee;
        ws.Cell(row, 11).Value = report.GrandTotalPit;
        ws.Cell(row, 12).Value = report.GrandTotalNet;

        for (int c = 1; c <= 12; c++)
        {
            ws.Cell(row, c).Style.Font.Bold = true;
            ws.Cell(row, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#eef4ff");
            if (c >= 5)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static byte[] ProtectPdfWithPassword(byte[] rawPdfBytes, string userPassword)
    {
        using var inStream = new MemoryStream(rawPdfBytes);
        using var pdfDocument = PdfReader.Open(inStream, PdfDocumentOpenMode.Modify);
        pdfDocument.SecuritySettings.UserPassword = userPassword;
        pdfDocument.SecuritySettings.OwnerPassword = GenerateRandomOwnerPassword();
        pdfDocument.SecuritySettings.PermitPrint = true;
        pdfDocument.SecuritySettings.PermitExtractContent = false;
        pdfDocument.SecuritySettings.PermitModifyDocument = false;

        using var outStream = new MemoryStream();
        pdfDocument.Save(outStream, false);
        return outStream.ToArray();
    }

    private static IReadOnlyList<PitBracketTaxSliceDto> ResolvePitSlices(
        PayrollRecord record,
        IReadOnlyList<PitBracket> pitBrackets)
    {
        if (!string.IsNullOrWhiteSpace(record.PitBracketsJson))
        {
            var deserialized = JsonSerializer.Deserialize<List<PitBracketTaxSliceDto>>(record.PitBracketsJson);
            if (deserialized is { Count: > 0 })
                return deserialized;
        }

        return VietnamPayrollEngine.CalculateProgressivePitBreakdown(record.AssessableIncome, pitBrackets);
    }

    public PayslipPdfResult GenerateTerminationSettlementPdf(EmployeeTerminationSettlement settlement, Employee employee)
    {
        ArgumentNullException.ThrowIfNull(settlement);
        ArgumentNullException.ThrowIfNull(employee);

        var plainBankAcc = _encryptionService.Decrypt(employee.BankAccountEncrypted);
        var maskedAcc = string.IsNullOrWhiteSpace(plainBankAcc)
            ? "Chưa cập nhật"
            : (plainBankAcc.Length > 4 ? $"****{plainBankAcc[^4..]}" : plainBankAcc);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.4f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)").Bold().FontSize(11).FontColor("#003DA5");
                            left.Item().Text("Hệ thống Quản trị Nguồn nhân lực Enterprise HRM").FontSize(8.5f).FontColor("#64748b");
                        });
                        row.RelativeItem().AlignRight().Column(right =>
                        {
                            right.Item().Text($"BIÊN BẢN QUYẾT TOÁN NGHỈ VIỆC — {settlement.SettlementCode}").Bold().FontSize(11);
                            right.Item().Text($"Ngày làm việc cuối: {settlement.LastWorkingDate:dd/MM/yyyy} | Trạng thái: {settlement.Status}").FontSize(8.5f);
                        });
                    });
                    col.Item().PaddingVertical(6).LineHorizontal(1).LineColor("#003DA5");
                });

                page.Content().Column(col =>
                {
                    col.Spacing(8);

                    col.Item().Border(0.75f).BorderColor("#cbd5e1").Padding(8).Column(info =>
                    {
                        info.Spacing(3);
                        info.Item().Text($"Nhân viên: {employee.FullName.ToUpperInvariant()}   |   Mã NV: {employee.EmployeeCode}").Bold();
                        info.Item().Text($"Đơn vị: {employee.OrganizationUnit?.UnitName ?? "-"}   |   Chức danh: {employee.Position?.PositionName ?? "-"}");
                        info.Item().Text($"Ngày vào làm: {settlement.HireDate:dd/MM/yyyy}   |   Hình thức: {settlement.TerminationType}   |   Tài khoản nhận: {employee.BankName ?? "-"} ({maskedAcc})");
                        info.Item().Text($"Thâm niên: {settlement.TotalTenureMonths:0.#} tháng | Đóng BHTN: {settlement.UnemploymentInsuranceCoveredMonths:0.#} tháng | Số năm hưởng TCTV: {settlement.EligibleSeveranceYears:0.#} năm");
                    });

                    col.Item().Text("I. CÁC KHOẢN CHI TRẢ QUYẾT TOÁN (GROSS SETTLEMENT)").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(4);
                            c.RelativeColumn(3);
                        });

                        AddTableHeader(table, "Khoản mục", "Cơ sở tính", "Số tiền (VNĐ)");
                        AddTableRow(table, "1. Lương kỳ làm việc cuối cùng", $"{settlement.WorkedDaysInFinalMonth:0.##}/{settlement.StandardWorkdaysUsed:0.##} công chuẩn", $"{settlement.FinalMonthSalaryAmount:N0}");
                        AddTableRow(table, "2. Chi trả phép năm chưa sử dụng", $"{settlement.UnusedLeaveDays:0.##} ngày phép tồn", $"{settlement.UnusedLeavePayoutAmount:N0}");
                        AddTableRow(table, "3. Trợ cấp thôi việc theo luật định", $"{settlement.EligibleSeveranceYears:0.##} năm x {settlement.StatutorySeveranceRatePerYear:0.##} x {settlement.AverageSalaryLast6Months:N0}", $"{settlement.StatutorySeveranceAmount:N0}");
                        AddTableRow(table, "4. Trợ cấp bổ sung chính sách nội bộ", $"{settlement.EligibleSeveranceYears:0.##} năm x {settlement.CompanyExtraSeveranceRatePerYear:0.##} tháng lương", $"{settlement.CompanyExtraSeveranceAmount:N0}");
                        AddTableRow(table, "5. Thưởng / Truy lĩnh tồn đọng khác", $"Áp dụng kỳ {settlement.TargetPayrollMonth:D2}/{settlement.TargetPayrollYear}", $"{settlement.OtherBonusOrRetroAmount:N0}");
                        AddTableTotalRow(table, "TỔNG THU NHẬP QUYẾT TOÁN (GROSS)", $"{settlement.TotalGrossSettlement:N0} đ");
                    });

                    col.Item().Text("II. CÁC KHOẢN KHẤU TRỪ, TẠM ỨNG & TẤT TOÁN KHOẢN VAY").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(5);
                            c.RelativeColumn(4);
                            c.RelativeColumn(3);
                        });

                        AddTableHeader(table, "Khoản khấu trừ / Thu hồi", "Diễn giải", "Số tiền (VNĐ)");
                        AddTableRow(table, "1. BHXH / BHYT / BHTN NLĐ đóng", "Theo lương đóng bảo hiểm kỳ cuối", $"{settlement.EmployeeInsuranceDeduction:N0}");
                        AddTableRow(table, "2. Thuế TNCN quyết toán", "Đã loại trừ TCTV miễn thuế theo TT 111/2013", $"{settlement.PersonalIncomeTaxDeduction:N0}");
                        AddTableRow(table, "3. Thu hồi tạm ứng lương chưa khấu trừ", "Đơn tạm ứng đã duyệt chờ cấn trừ", $"{settlement.SalaryAdvanceDeduction:N0}");
                        AddTableRow(table, "4. Tất toán dư nợ vay / trả góp nội bộ", "Toàn bộ các kỳ còn lại của hợp đồng vay", $"{settlement.LoanPayoffDeduction:N0}");
                        AddTableRow(table, "5. Khấu trừ / bồi hoàn khác", settlement.Notes ?? "-", $"{settlement.OtherDeduction:N0}");
                        var totalDeductions = settlement.EmployeeInsuranceDeduction
                            + settlement.PersonalIncomeTaxDeduction
                            + settlement.SalaryAdvanceDeduction
                            + settlement.LoanPayoffDeduction
                            + settlement.OtherDeduction;
                        AddTableTotalRow(table, "TỔNG CÁC KHOẢN KHẤU TRỪ & THU HỒI", $"{totalDeductions:N0} đ");
                    });

                    col.Item().Background("#eef4ff").Border(1).BorderColor("#003DA5").Padding(10).Row(r =>
                    {
                        r.RelativeItem().Column(c =>
                        {
                            c.Item().Text("SỐ TIỀN QUYẾT TOÁN THỰC NHẬN (NET SETTLEMENT)").Bold().FontSize(11).FontColor("#003DA5");
                            if (settlement.RemainingDebtToRecover > 0m)
                            {
                                c.Item().Text($"Cảnh báo: Công nợ còn phải thu hồi thêm từ NLĐ: {settlement.RemainingDebtToRecover:N0} đ")
                                    .FontSize(8.5f).Bold().FontColor("#b91c1c");
                            }
                            else
                            {
                                c.Item().Text("Đã đối chiếu công, phép tồn, tạm ứng lương và tất toán khoản vay nội bộ.")
                                    .FontSize(8.5f);
                            }
                        });
                        r.AutoItem().AlignMiddle().Text($"{settlement.NetSettlementAmount:N0} VNĐ").Bold().FontSize(15).FontColor("#0f766e");
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Biên bản quyết toán nghỉ việc Vinamilk HRM • {employee.EmployeeCode} ({settlement.SettlementCode}) • Trang ");
                    x.CurrentPageNumber();
                });
            });
        });

        var rawBytes = doc.GeneratePdf();
        var fileName = $"QuyetToanNghiViec_{employee.EmployeeCode}_{settlement.SettlementCode}.pdf";
        return new PayslipPdfResult(rawBytes, fileName, "Biên bản quyết toán nghỉ việc nội bộ", false);
    }

    private static void AddTableHeader(TableDescriptor table, params string[] headers)
    {
        table.Header(h =>
        {
            foreach (var title in headers)
            {
                h.Cell().Background("#003DA5").Padding(5).Text(title).Bold().FontColor(Colors.White).FontSize(9);
            }
        });
    }

    private static void AddTableRow(TableDescriptor table, string col1, string col2, string col3)
    {
        table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(col1);
        table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).Text(col2);
        table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(4).AlignRight().Text(col3);
    }

    private static void AddTableTotalRow(TableDescriptor table, string label, string totalText)
    {
        table.Cell().ColumnSpan(2).Background("#f1f5f9").Padding(5).Text(label).Bold();
        table.Cell().Background("#f1f5f9").Padding(5).AlignRight().Text(totalText).Bold().FontColor("#003DA5");
    }

    private static void AddTableTotalRowSpan(TableDescriptor table, uint totalColumns, string label, string totalText)
    {
        table.Cell().ColumnSpan(totalColumns - 1).Background("#f1f5f9").Padding(5).Text(label).Bold();
        table.Cell().Background("#f1f5f9").Padding(5).AlignRight().Text(totalText).Bold().FontColor("#003DA5");
    }

    public byte[] GenerateDetailedUnitPayrollA3LandscapePdf(DetailedUnitPayrollReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A3.Landscape());
                page.Margin(1.0f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(8f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(left =>
                        {
                            left.Item().Text("CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)").Bold().FontSize(11).FontColor("#003DA5");
                            left.Item().Text("Số 10 Tân Trào, Phường Tân Phú, Quận 7, TP.HCM • Hệ thống Quản trị Lương & Phúc lợi C&B").FontSize(8).FontColor("#475569");
                        });
                        row.ConstantItem(280).AlignRight().Column(right =>
                        {
                            right.Item().AlignRight().Text($"BẢNG LƯƠNG CHI TIẾT ĐƠN VỊ ({report.PeriodCode})").Bold().FontSize(11).FontColor("#003DA5");
                            right.Item().AlignRight().Text($"Kỳ: {report.StartDate:dd/MM/yyyy} – {report.EndDate:dd/MM/yyyy} • Trạng thái: {report.Status}").FontSize(8).FontColor("#475569");
                        });
                    });
                    col.Item().PaddingTop(4).LineHorizontal(1).LineColor("#003DA5");
                });

                page.Content().PaddingVertical(8).Column(col =>
                {
                    col.Spacing(8);

                    col.Item().AlignCenter().Text(report.PeriodTitle.ToUpperInvariant()).Bold().FontSize(12);

                    foreach (var group in report.UnitGroups)
                    {
                        col.Item().Text($"Đơn vị: [{group.UnitCode}] {group.UnitName} • Cost Center: {group.CostCenterCode} • Quân số: {group.Headcount}")
                            .Bold().FontSize(9).FontColor("#003DA5");

                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(cols =>
                            {
                                cols.ConstantColumn(24);   // STT
                                cols.ConstantColumn(58);   // Ma NV
                                cols.RelativeColumn(1.4f); // Ho ten
                                cols.RelativeColumn(1.1f); // Chuc danh
                                cols.ConstantColumn(38);   // Cong
                                cols.ConstantColumn(70);   // Luong CB
                                cols.ConstantColumn(65);   // Phu cap
                                cols.ConstantColumn(58);   // OT
                                cols.ConstantColumn(65);   // Thuong/Truy linh
                                cols.ConstantColumn(75);   // Gross
                                cols.ConstantColumn(65);   // BH NLD
                                cols.ConstantColumn(58);   // Thue TNCN
                                cols.ConstantColumn(60);   // Tam ung/Khac
                                cols.ConstantColumn(78);   // Thuc linh Net
                                cols.ConstantColumn(75);   // Chi phi DN
                            });

                            string[] headers =
                            [
                                "STT", "Mã NV", "Họ và tên", "Chức danh", "Công",
                                "Lương thực tế", "Phụ cấp", "OT", "Thưởng/TL", "Tổng Gross",
                                "BH NLĐ", "Thuế TNCN", "Khấu trừ khác", "Thực lĩnh Net", "Tổng CP DN"
                            ];

                            table.Header(h =>
                            {
                                foreach (var hdr in headers)
                                {
                                    h.Cell().Background("#003DA5").Padding(3).Text(hdr).Bold().FontColor(Colors.White).FontSize(7.5f);
                                }
                            });

                            foreach (var r in group.Employees)
                            {
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignCenter().Text(r.Stt.ToString());
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).Text(r.EmployeeCode);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).Text(r.FullName);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).Text(r.PositionName);
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.PayableWorkdays:N1}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.ActualBaseSalary:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.PositionAndOtherAllowance:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.OvertimePay:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.BonusAndRetroTotal:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.GrossIncome:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.TotalEmployeeInsurance:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.PersonalIncomeTax:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.OtherDeductionsAndAdvances:N0}");
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.NetSalary:N0}").Bold();
                                table.Cell().BorderBottom(0.5f).BorderColor("#e2e8f0").Padding(2.5f).AlignRight().Text($"{r.TotalCompanyCost:N0}");
                            }

                            table.Cell().ColumnSpan(9).Background("#f1f5f9").Padding(3).Text($"Cộng đơn vị [{group.UnitCode}] ({group.Headcount} NV)").Bold();
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalGross:N0}").Bold();
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalEmployeeInsurance:N0}").Bold();
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalPit:N0}").Bold();
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalOtherDeductions:N0}").Bold();
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalNet:N0}").Bold().FontColor("#003DA5");
                            table.Cell().Background("#f1f5f9").Padding(3).AlignRight().Text($"{group.SubtotalCompanyCost:N0}").Bold();
                        });
                    }

                    col.Item().Background("#eef4ff").Border(1).BorderColor("#003DA5").Padding(6).Row(r =>
                    {
                        r.RelativeItem().Text($"TỔNG TOÀN KỲ ({report.TotalEmployees} nhân sự): Gross = {report.GrandTotalGross:N0} đ • BH NLĐ = {report.GrandTotalEmployeeInsurance:N0} đ • Thuế TNCN = {report.GrandTotalPit:N0} đ • Khấu trừ khác = {report.GrandTotalOtherDeductions:N0} đ")
                            .Bold().FontSize(9);
                        r.AutoItem().Text($"THỰC LĨNH NET: {report.GrandTotalNet:N0} đ • TỔNG CP DN: {report.GrandTotalCompanyCost:N0} đ")
                            .Bold().FontSize(9.5f).FontColor("#003DA5");
                    });

                    col.Item().PaddingTop(14).Row(sign =>
                    {
                        sign.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("NGƯỜI LẬP BIỂU").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(7.5f);
                            c.Item().PaddingTop(28).AlignCenter().Text("Chuyên viên C&B");
                        });
                        sign.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("KẾ TOÁN TRƯỞNG").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(7.5f);
                            c.Item().PaddingTop(28).AlignCenter().Text("Phòng Tài chính – Kế toán");
                        });
                        sign.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("GIÁM ĐỐC NHÂN SỰ / TỔNG GIÁM ĐỐC").Bold();
                            c.Item().AlignCenter().Text("(Ký, đóng dấu)").Italic().FontSize(7.5f);
                            c.Item().PaddingTop(28).AlignCenter().Text($"Ngày {DateTime.Today:dd/MM/yyyy}");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Vinamilk HRM • Bảng lương chi tiết A3 Ngang ({report.PeriodCode}) • Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] GeneratePitWithholdingCertificatePdf(PitWithholdingCertificateDto certificate, Employee employee)
    {
        ArgumentNullException.ThrowIfNull(certificate);
        ArgumentNullException.ThrowIfNull(employee);

        var doc = Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(1.5f, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(9.5f));

                page.Header().Column(col =>
                {
                    col.Item().Row(row =>
                    {
                        row.RelativeItem().Column(c =>
                        {
                            c.Item().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(10);
                            c.Item().Text("Độc lập - Tự do - Hạnh phúc").FontSize(9);
                            c.Item().PaddingTop(6).Text("TỔ CHỨC TRẢ THU NHẬP: CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)").Bold().FontColor("#003DA5");
                            c.Item().Text("Mã số thuế đơn vị: 0300588569 • Trụ sở: Số 10 Tân Trào, Q.7, TP.HCM").FontSize(8.5f);
                        });
                        row.ConstantItem(180).AlignRight().Column(r =>
                        {
                            r.Item().AlignRight().Text("Mẫu số CTT56 / TT80").Bold().FontSize(9);
                            r.Item().AlignRight().Text($"Số chứng từ: {certificate.CertificateCode}").Bold().FontColor("#b91c1c");
                            r.Item().AlignRight().Text($"Năm tính thuế: {certificate.TaxYear}").Bold();
                        });
                    });
                    col.Item().PaddingTop(6).LineHorizontal(1).LineColor("#003DA5");
                });

                page.Content().PaddingVertical(12).Column(col =>
                {
                    col.Spacing(10);

                    col.Item().AlignCenter().Text("CHỨNG TỪ KHẤU TRỪ THUẾ THU NHẬP CÁ NHÂN").Bold().FontSize(14).FontColor("#003DA5");
                    col.Item().AlignCenter().Text($"(Căn cứ: {certificate.LegalReference})").Italic().FontSize(8.5f);

                    col.Item().Background("#f8fafc").Border(0.5f).BorderColor("#cbd5e1").Padding(10).Column(info =>
                    {
                        info.Spacing(3);
                        info.Item().Text("I. THÔNG TIN NGƯỜI NỘP THUẾ").Bold().FontColor("#003DA5");
                        info.Item().Text($"Họ và tên người nộp thuế: {certificate.EmployeeName} (Mã NV: {certificate.EmployeeCode})");
                        info.Item().Text($"Mã số thuế cá nhân: {certificate.TaxCode}");
                        info.Item().Text($"Số CCCD / CMND: {certificate.CitizenId}");
                        info.Item().Text($"Loại hợp đồng / Tình trạng làm việc: {employee.CurrentContractType} ({employee.WorkStatus})");
                    });

                    col.Item().Text("II. THÔNG TIN THUẾ THU NHẬP CÁ NHÂN KHẤU TRỪ TRONG NĂM").Bold().FontColor("#003DA5");
                    col.Item().Table(table =>
                    {
                        table.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn(3);
                            c.RelativeColumn(1.4f);
                            c.RelativeColumn(1.3f);
                        });
                        AddTableHeader(table, "Chỉ tiêu tổng hợp", "Kỳ / Căn cứ", "Số tiền (VNĐ)");
                        AddTableRow(table, "[1] Tổng thu nhập chịu thuế đã trả (Gross)", $"Năm {certificate.TaxYear}", $"{certificate.GrossIncome:N0}");
                        AddTableRow(table, "[2] Tổng các khoản bảo hiểm bắt buộc được trừ", "BHXH, BHYT, BHTN", $"{certificate.TotalInsuranceDeducted:N0}");
                        AddTableRow(table, "[3] Tổng giảm trừ gia cảnh (bản thân + người phụ thuộc)", "Theo đăng ký NPT", $"{certificate.TotalFamilyDeductions:N0}");
                        AddTableRow(table, "[4] Thu nhập chịu thuế quy đổi", "[1] - [2]", $"{certificate.TaxableIncome:N0}");
                        AddTableRow(table, "[5] Tổng thu nhập tính thuế", "Max(0, [4] - [3])", $"{certificate.AssessableIncome:N0}");
                        AddTableRow(table, "[6] Số thuế TNCN quyết toán cả năm", "Biểu lũy tiến / 20%", $"{certificate.AnnualFinalTaxLiability:N0}");
                        AddTableTotalRow(table, "[7] TỔNG SỐ THUẾ TNCN ĐÃ KHẤU TRỪ TRONG NĂM", $"{certificate.TotalPitWithheld:N0} đ");
                    });

                    var diffLabel = certificate.PitOverpaidOrUnderpaid switch
                    {
                        > 0m => $"Số thuế nộp thừa / hoàn lại ước tính: {certificate.PitOverpaidOrUnderpaid:N0} VNĐ",
                        < 0m => $"Số thuế còn phải nộp thêm ước tính: {Math.Abs(certificate.PitOverpaidOrUnderpaid):N0} VNĐ",
                        _ => "Đã khấu trừ khớp 100% nghĩa vụ thuế quyết toán cả năm (Chênh lệch: 0 VNĐ)."
                    };

                    col.Item().Background("#eef4ff").Border(1).BorderColor("#003DA5").Padding(8)
                        .Text(diffLabel).Bold().FontColor("#003DA5");

                    col.Item().PaddingTop(14).Row(sign =>
                    {
                        sign.RelativeItem().Text($"Ngày cấp chứng từ: {certificate.IssuedDate:dd/MM/yyyy}");
                        sign.ConstantItem(220).AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("ĐẠI DIỆN TỔ CHỨC TRẢ THU NHẬP").Bold();
                            c.Item().AlignCenter().Text("(Ký, ghi rõ họ tên, đóng dấu)").Italic().FontSize(8);
                            c.Item().PaddingTop(32).AlignCenter().Text("Phòng Tài chính & Nhân sự Vinamilk");
                        });
                    });
                });

                page.Footer().AlignCenter().Text(x =>
                {
                    x.Span($"Chứng từ điện tử {certificate.CertificateCode} • Vinamilk HRM • Trang ");
                    x.CurrentPageNumber();
                });
            });
        });

        return doc.GeneratePdf();
    }

    public byte[] ExportSocialInsuranceExcel(SocialInsuranceMonthlyReportDto report, SocialInsuranceReportTemplate? template)
    {
        ArgumentNullException.ThrowIfNull(report);

        List<SiColumnMappingItem>? configuredCols = null;
        if (template is not null && !string.IsNullOrWhiteSpace(template.ColumnMappingsJson))
        {
            configuredCols = JsonSerializer.Deserialize<List<SiColumnMappingItem>>(template.ColumnMappingsJson);
        }

        if (configuredCols is null || configuredCols.Count == 0)
        {
            configuredCols =
            [
                new(1, "STT", "Stt", 6),
                new(2, "Mã NV", "EmployeeCode", 12),
                new(3, "Họ và tên", "FullName", 24),
                new(4, "Mã số BHXH", "SocialInsuranceNumber", 16),
                new(5, "Đơn vị", "UnitName", 22),
                new(6, "Chức vụ", "PositionName", 20),
                new(7, "Mã biến động", "ChangeCode", 18),
                new(8, "Diễn giải", "ChangeLabel", 26),
                new(9, "Mức đóng trước", "PreviousSalaryBase", 18),
                new(10, "Mức đóng này", "CurrentSalaryBase", 18),
                new(11, "Chênh lệch", "SalaryBaseDelta", 18),
                new(12, "Ghi chú", "Notes", 28)
            ];
        }

        configuredCols = configuredCols.OrderBy(c => c.ColumnIndex).ToList();

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BienDong_D02LT");
        ws.Cell(1, 1).Value = $"DANH SÁCH LAO ĐỘNG THAM GIA BHXH, BHYT, BHTN ({template?.TemplateCode ?? "D02-LT"}) - THÁNG {report.Month:D2}/{report.Year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;
        ws.Range(1, 1, 1, configuredCols.Count).Merge();

        ws.Cell(2, 1).Value = $"Kỳ đối chiếu: {report.CurrentPeriodCode} | Kỳ trước: {report.PreviousPeriodCode ?? "Không có"} | Căn cứ: {template?.LegalReference ?? "QĐ 490/QĐ-BHXH"}";
        ws.Range(2, 1, 2, configuredCols.Count).Merge();

        int headerRow = 4;
        for (int i = 0; i < configuredCols.Count; i++)
        {
            var colDef = configuredCols[i];
            int excelCol = i + 1;
            ws.Cell(headerRow, excelCol).Value = colDef.HeaderText;
            ws.Cell(headerRow, excelCol).Style.Font.Bold = true;
            ws.Cell(headerRow, excelCol).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws.Cell(headerRow, excelCol).Style.Font.FontColor = XLColor.White;
            if (colDef.Width > 0)
                ws.Column(excelCol).Width = colDef.Width;
        }

        int row = headerRow + 1;
        foreach (var item in report.Movements)
        {
            for (int i = 0; i < configuredCols.Count; i++)
            {
                int excelCol = i + 1;
                var cell = ws.Cell(row, excelCol);
                switch (configuredCols[i].FieldKey)
                {
                    case "Stt": cell.Value = item.Stt; break;
                    case "EmployeeCode": cell.Value = item.EmployeeCode; break;
                    case "FullName": cell.Value = item.FullName; break;
                    case "SocialInsuranceNumber": cell.Value = item.SocialInsuranceNumber; break;
                    case "UnitName": cell.Value = item.UnitName; break;
                    case "PositionName": cell.Value = item.PositionName; break;
                    case "ChangeCode": cell.Value = item.ChangeCode; break;
                    case "ChangeLabel": cell.Value = item.ChangeLabel; break;
                    case "PreviousSalaryBase":
                        cell.Value = item.PreviousSalaryBase;
                        cell.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "CurrentSalaryBase":
                        cell.Value = item.CurrentSalaryBase;
                        cell.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "SalaryBaseDelta":
                        cell.Value = item.SalaryBaseDelta;
                        cell.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "EmployeeSiHiUiTotal":
                        cell.Value = item.EmployeeSiHiUiTotal;
                        cell.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "CompanySiHiUiOaTotal":
                        cell.Value = item.CompanySiHiUiOaTotal;
                        cell.Style.NumberFormat.Format = "#,##0";
                        break;
                    case "Notes": cell.Value = item.Notes; break;
                    default: cell.Value = string.Empty; break;
                }
            }
            row++;
        }

        var reconWs = wb.Worksheets.Add("DoiChieu_PhaiNop");
        reconWs.Cell(1, 1).Value = $"ĐỐI CHIẾU SỐ PHẢI NỘP BHXH THÁNG {report.Month:D2}/{report.Year}";
        reconWs.Cell(1, 1).Style.Font.Bold = true;
        reconWs.Cell(3, 1).Value = "Chỉ tiêu đối chiếu";
        reconWs.Cell(3, 2).Value = "Số liệu (VNĐ / Người)";
        reconWs.Range(3, 1, 3, 2).Style.Font.Bold = true;

        (string Label, decimal Val)[] metrics =
        [
            ("Số lao động đóng BHXH", report.Reconciliation.InsuredHeadcount),
            ("Tổng quỹ lương đóng BHXH-BHYT", report.Reconciliation.TotalInsuranceSalaryBase),
            ("Tổng quỹ lương đóng BHTN", report.Reconciliation.TotalUnemploymentSalaryBase),
            ("NLĐ phải đóng theo tỷ lệ pháp định", report.Reconciliation.ExpectedEmployeeContribution),
            ("NLĐ đã khấu trừ trên bảng lương", report.Reconciliation.ActualEmployeeContributionFromRecords),
            ("DN phải đóng theo tỷ lệ pháp định", report.Reconciliation.ExpectedCompanyContribution),
            ("DN đã trích trên bảng lương", report.Reconciliation.ActualCompanyContributionFromRecords),
            ("Tổng phải nộp cơ quan BHXH (Dự kiến)", report.Reconciliation.TotalExpectedPayableToAgency),
            ("Tổng thực tế theo bảng lương", report.Reconciliation.TotalActualFromPayrollRecords),
            ("Chênh lệch đối chiếu", report.Reconciliation.ReconciliationDifference)
        ];

        for (int i = 0; i < metrics.Length; i++)
        {
            reconWs.Cell(4 + i, 1).Value = metrics[i].Label;
            reconWs.Cell(4 + i, 2).Value = metrics[i].Val;
            reconWs.Cell(4 + i, 2).Style.NumberFormat.Format = "#,##0";
        }
        reconWs.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportPitSettlementExcel(PitAnnualFinalizationReportDto report)
    {
        ArgumentNullException.ThrowIfNull(report);

        using var wb = new XLWorkbook();
        var wsResident = wb.Worksheets.Add("05_1_BK_QTT");
        wsResident.Cell(1, 1).Value = $"BẢNG KÊ CHI TIẾT CÁ NHÂN CƯ TRÚ TÍNH THUẾ THEO BIỂU LŨY TIẾN (MẪU 05-1/BK-QTT-TNCN) - NĂM {report.TaxYear}";
        wsResident.Cell(1, 1).Style.Font.Bold = true;

        var wsNonResident = wb.Worksheets.Add("05_2_BK_VangLai");
        wsNonResident.Cell(1, 1).Value = $"BẢNG KÊ CÁ NHÂN KHÔNG CƯ TRÚ / KHẤU TRỪ TOÀN PHẦN (MẪU 05-2/BK-QTT-TNCN) - NĂM {report.TaxYear}";
        wsNonResident.Cell(1, 1).Style.Font.Bold = true;

        string[] headers =
        [
            "STT", "Mã NV", "Họ và tên", "Mã số thuế", "Số CCCD", "Số tháng tính thuế",
            "Tổng TNCT (Gross)", "BH bắt buộc đã trừ", "Giảm trừ gia cảnh",
            "Thu nhập chịu thuế", "Thu nhập tính thuế", "Thuế phải nộp cả năm",
            "Thuế đã khấu trừ", "Nộp thêm", "Nộp thừa / Hoàn"
        ];

        static void WriteSheetHeaders(IXLWorksheet sheet, string[] hdrs)
        {
            for (int c = 0; c < hdrs.Length; c++)
            {
                sheet.Cell(3, c + 1).Value = hdrs[c];
                sheet.Cell(3, c + 1).Style.Font.Bold = true;
                sheet.Cell(3, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
                sheet.Cell(3, c + 1).Style.Font.FontColor = XLColor.White;
            }
        }

        WriteSheetHeaders(wsResident, headers);
        WriteSheetHeaders(wsNonResident, headers);

        int rRes = 4;
        int rNonRes = 4;
        foreach (var emp in report.Employees)
        {
            var isNonRes = emp.TaxResidencyMode.Contains("20%", StringComparison.OrdinalIgnoreCase);
            var targetWs = isNonRes ? wsNonResident : wsResident;
            ref int rowIdx = ref (isNonRes ? ref rNonRes : ref rRes);

            targetWs.Cell(rowIdx, 1).Value = rowIdx - 3;
            targetWs.Cell(rowIdx, 2).Value = emp.EmployeeCode;
            targetWs.Cell(rowIdx, 3).Value = emp.FullName;
            targetWs.Cell(rowIdx, 4).Value = emp.TaxCode;
            targetWs.Cell(rowIdx, 5).Value = emp.CitizenId;
            targetWs.Cell(rowIdx, 6).Value = emp.ActiveMonthsInYear;
            targetWs.Cell(rowIdx, 7).Value = emp.TotalGrossIncome;
            targetWs.Cell(rowIdx, 8).Value = emp.TotalInsuranceDeducted;
            targetWs.Cell(rowIdx, 9).Value = emp.TotalFamilyDeductions;
            targetWs.Cell(rowIdx, 10).Value = emp.TotalTaxableIncome;
            targetWs.Cell(rowIdx, 11).Value = emp.AnnualAssessableIncome;
            targetWs.Cell(rowIdx, 12).Value = emp.AnnualFinalTaxLiability;
            targetWs.Cell(rowIdx, 13).Value = emp.TotalPitWithheld;
            targetWs.Cell(rowIdx, 14).Value = emp.AdditionalTaxPayable;
            targetWs.Cell(rowIdx, 15).Value = emp.RefundableTax;
            for (int c = 7; c <= 15; c++)
                targetWs.Cell(rowIdx, c).Style.NumberFormat.Format = "#,##0";

            rowIdx++;
        }

        wsResident.Columns().AdjustToContents();
        wsNonResident.Columns().AdjustToContents();

        var summaryWs = wb.Worksheets.Add("TongHop_05QTT");
        summaryWs.Cell(1, 1).Value = $"TỔNG HỢP TỜ KHAI QUYẾT TOÁN THUẾ TNCN 05/QTT-TNCN NĂM {report.TaxYear}";
        summaryWs.Cell(1, 1).Style.Font.Bold = true;
        summaryWs.Cell(3, 1).Value = "Tổng thu nhập chịu thuế (Gross)";
        summaryWs.Cell(3, 2).Value = report.GrandTotalGross;
        summaryWs.Cell(4, 1).Value = "Tổng thu nhập chịu thuế sau BH";
        summaryWs.Cell(4, 2).Value = report.GrandTotalTaxableIncome;
        summaryWs.Cell(5, 1).Value = "Tổng thuế TNCN đã khấu trừ";
        summaryWs.Cell(5, 2).Value = report.GrandTotalPitWithheld;
        summaryWs.Cell(6, 1).Value = "Tổng nghĩa vụ thuế quyết toán cả năm";
        summaryWs.Cell(6, 2).Value = report.GrandTotalFinalTaxLiability;
        summaryWs.Cell(7, 1).Value = "Tổng thuế phải nộp thêm";
        summaryWs.Cell(7, 2).Value = report.GrandTotalAdditionalPayable;
        summaryWs.Cell(8, 1).Value = "Tổng thuế nộp thừa";
        summaryWs.Cell(8, 2).Value = report.GrandTotalRefundable;
        for (int r = 3; r <= 8; r++)
            summaryWs.Cell(r, 2).Style.NumberFormat.Format = "#,##0";
        summaryWs.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportAccountingJournalFile(PayrollAccountingJournalDto journal, string format)
    {
        ArgumentNullException.ThrowIfNull(journal);

        if (string.Equals(format, "csv", StringComparison.OrdinalIgnoreCase))
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("STT,NgayHachToan,MaKhoanMuc,TK_No,TK_Co,CostCenter,DienGiai,SoTien");
            foreach (var line in journal.Lines)
            {
                sb.AppendLine($"{line.LineNo},{journal.PostingDate:yyyy-MM-dd},{EscapeCsv(line.AccountKey)},{EscapeCsv(line.DebitAccountCode)},{EscapeCsv(line.CreditAccountCode)},{EscapeCsv(line.CostCenterCode)},{EscapeCsv(line.Description)},{line.Amount:0}");
            }
            var preamble = System.Text.Encoding.UTF8.GetPreamble();
            var body = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
            var result = new byte[preamble.Length + body.Length];
            Buffer.BlockCopy(preamble, 0, result, 0, preamble.Length);
            Buffer.BlockCopy(body, 0, result, preamble.Length, body.Length);
            return result;
        }

        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("ButToan_HachToanLuong");
        ws.Cell(1, 1).Value = $"BÚT TOÁN HẠCH TOÁN LƯƠNG KỲ {journal.PeriodCode} – {journal.PeriodTitle}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(2, 1).Value = $"Ngày hạch toán: {journal.PostingDate:dd/MM/yyyy} • Cân đối Nợ/Có: {(journal.IsBalanced ? "CÂN ĐỐI" : "LỆCH")} • TK 3341 Phải trả NLĐ = {journal.PayableToEmployeesAccount334:N0} đ";

        string[] headers = ["STT", "Ngày HT", "Mã khóa TK", "TK Nợ", "TK Có", "Cost Center", "Diễn giải", "Số tiền (VNĐ)"];
        for (int c = 0; c < headers.Length; c++)
        {
            ws.Cell(4, c + 1).Value = headers[c];
            ws.Cell(4, c + 1).Style.Font.Bold = true;
            ws.Cell(4, c + 1).Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            ws.Cell(4, c + 1).Style.Font.FontColor = XLColor.White;
        }

        int row = 5;
        foreach (var line in journal.Lines)
        {
            ws.Cell(row, 1).Value = line.LineNo;
            ws.Cell(row, 2).Value = journal.PostingDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 3).Value = line.AccountKey;
            ws.Cell(row, 4).Value = line.DebitAccountCode;
            ws.Cell(row, 5).Value = line.CreditAccountCode;
            ws.Cell(row, 6).Value = line.CostCenterCode;
            ws.Cell(row, 7).Value = line.Description;
            ws.Cell(row, 8).Value = line.Amount;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        ws.Cell(row, 7).Value = "TỔNG PHÁT SINH NỢ = TỔNG PHÁT SINH CÓ";
        ws.Cell(row, 7).Style.Font.Bold = true;
        ws.Cell(row, 8).Value = journal.TotalDebit;
        ws.Cell(row, 8).Style.Font.Bold = true;
        ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string EscapeCsv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (!value.Contains(',') && !value.Contains('"') && !value.Contains('\n'))
            return value;
        return $"\"{value.Replace("\"", "\"\"")}\"";
    }

    private sealed record SiColumnMappingItem(int ColumnIndex, string HeaderText, string FieldKey, int Width);
}
