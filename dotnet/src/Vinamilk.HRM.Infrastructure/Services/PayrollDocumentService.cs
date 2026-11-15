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
}
