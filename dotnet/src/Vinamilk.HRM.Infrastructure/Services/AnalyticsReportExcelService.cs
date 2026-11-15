using ClosedXML.Excel;
using Vinamilk.HRM.Application.Analytics;

namespace Vinamilk.HRM.Infrastructure.Services;

public class AnalyticsReportExcelService
{
    public byte[] ExportBm01(List<Bm01EmployeeRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BM01_NhanSu");
        ws.Cell(1, 1).Value = "BÁO CÁO BM01 - DANH SÁCH NHÂN SỰ TOÀN CÔNG TY";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;

        string[] headers = ["STT", "Mã NV", "Họ và tên", "Giới tính", "Ngày sinh", "Đơn vị", "Chức danh", "Trình độ", "Ngày vào làm", "Trạng thái", "Loại HĐ", "Lương cơ bản"];
        WriteHeaderRow(ws, 3, headers);

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 4;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.EmployeeCode;
            ws.Cell(row, 3).Value = r.FullName;
            ws.Cell(row, 4).Value = r.Gender;
            ws.Cell(row, 5).Value = r.DateOfBirth.ToString("dd/MM/yyyy");
            ws.Cell(row, 6).Value = r.UnitName;
            ws.Cell(row, 7).Value = r.PositionName;
            ws.Cell(row, 8).Value = r.EducationLevel;
            ws.Cell(row, 9).Value = r.HireDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 10).Value = r.WorkStatus;
            ws.Cell(row, 11).Value = r.ContractType;
            ws.Cell(row, 12).Value = r.BaseSalary;
            ws.Cell(row, 12).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportBm02(List<Bm02MovementRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BM02_BienDong");
        ws.Cell(1, 1).Value = "BÁO CÁO BM02 - BIẾN ĐỘNG NHÂN SỰ & ĐIỀU CHUYỂN";
        ws.Cell(1, 1).Style.Font.Bold = true;

        string[] headers = ["STT", "Số QĐ", "Mã NV", "Họ và tên", "Loại biến động", "Đơn vị cũ", "Đơn vị mới", "Chức danh cũ", "Chức danh mới", "Ngày hiệu lực", "Trạng thái"];
        WriteHeaderRow(ws, 3, headers);

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 4;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.DecisionNumber;
            ws.Cell(row, 3).Value = r.EmployeeCode;
            ws.Cell(row, 4).Value = r.FullName;
            ws.Cell(row, 5).Value = r.TransferType;
            ws.Cell(row, 6).Value = r.FromUnitName;
            ws.Cell(row, 7).Value = r.ToUnitName;
            ws.Cell(row, 8).Value = r.FromPositionName;
            ws.Cell(row, 9).Value = r.ToPositionName;
            ws.Cell(row, 10).Value = r.EffectiveDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 11).Value = r.ApprovalStatus;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportBm03(List<Bm03RewardDisciplineRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BM03_KhenThuongKyLuat");
        ws.Cell(1, 1).Value = "BÁO CÁO BM03 - KHEN THƯỞNG & KỶ LUẬT";
        ws.Cell(1, 1).Style.Font.Bold = true;

        string[] headers = ["STT", "Số QĐ", "Mã NV", "Họ và tên", "Đơn vị", "Phân loại", "Hình thức", "Số tiền", "Ngày hiệu lực", "Lý do", "Trạng thái"];
        WriteHeaderRow(ws, 3, headers);

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 4;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.DecisionNumber;
            ws.Cell(row, 3).Value = r.EmployeeCode;
            ws.Cell(row, 4).Value = r.FullName;
            ws.Cell(row, 5).Value = r.UnitName;
            ws.Cell(row, 6).Value = r.RecordType;
            ws.Cell(row, 7).Value = r.CategoryName;
            ws.Cell(row, 8).Value = r.Amount;
            ws.Cell(row, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(row, 9).Value = r.EffectiveDate.ToString("dd/MM/yyyy");
            ws.Cell(row, 10).Value = r.Reason;
            ws.Cell(row, 11).Value = r.ApprovalStatus;
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportBm04(List<Bm04PayrollSummaryRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BM04_QuyLuong");
        ws.Cell(1, 1).Value = "BÁO CÁO BM04 - TỔNG HỢP QUỸ LƯƠNG, BHXH & THUẾ TNCN";
        ws.Cell(1, 1).Style.Font.Bold = true;

        string[] headers = ["STT", "Mã ĐV", "Tên đơn vị", "Số NV", "Tổng Thu nhập Gross", "BHXH NV đóng", "BHXH DN đóng", "Thuế TNCN", "Tổng Thực nhận Net"];
        WriteHeaderRow(ws, 3, headers);

        for (int i = 0; i < rows.Count; i++)
        {
            var r = rows[i];
            var row = i + 4;
            ws.Cell(row, 1).Value = i + 1;
            ws.Cell(row, 2).Value = r.UnitCode;
            ws.Cell(row, 3).Value = r.UnitName;
            ws.Cell(row, 4).Value = r.Headcount;
            ws.Cell(row, 5).Value = r.TotalGross;
            ws.Cell(row, 6).Value = r.TotalEmployeeSi;
            ws.Cell(row, 7).Value = r.TotalCompanySi;
            ws.Cell(row, 8).Value = r.TotalPit;
            ws.Cell(row, 9).Value = r.TotalNet;
            for (int c = 5; c <= 9; c++)
                ws.Cell(row, c).Style.NumberFormat.Format = "#,##0";
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    public byte[] ExportDynamicReport(DynamicReportResultDto report)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add("BaoCaoTuyBien");
        ws.Cell(1, 1).Value = "BÁO CÁO TÙY BIẾN (DYNAMIC REPORT BUILDER)";
        ws.Cell(1, 1).Style.Font.Bold = true;

        var headers = new[] { "STT" }.Concat(report.Columns.Select(c => c.Label)).ToArray();
        WriteHeaderRow(ws, 3, headers);

        for (int i = 0; i < report.Rows.Count; i++)
        {
            var row = i + 4;
            ws.Cell(row, 1).Value = i + 1;
            for (int c = 0; c < report.Columns.Count; c++)
            {
                var key = report.Columns[c].Key;
                ws.Cell(row, c + 2).Value = report.Rows[i].TryGetValue(key, out var val) ? val : string.Empty;
            }
        }

        if (report.Groups.Count > 0)
        {
            var gWs = wb.Worksheets.Add("TongHopNhom");
            WriteHeaderRow(gWs, 1, [$"Nhóm ({report.GroupBy})", "Số lượng", "Tổng lương cơ bản"]);
            for (int i = 0; i < report.Groups.Count; i++)
            {
                gWs.Cell(i + 2, 1).Value = report.Groups[i].GroupKey;
                gWs.Cell(i + 2, 2).Value = report.Groups[i].Count;
                gWs.Cell(i + 2, 3).Value = report.Groups[i].TotalBaseSalary;
                gWs.Cell(i + 2, 3).Style.NumberFormat.Format = "#,##0";
            }
            gWs.Columns().AdjustToContents();
        }

        ws.Columns().AdjustToContents();
        return ToBytes(wb);
    }

    private static void WriteHeaderRow(IXLWorksheet ws, int row, string[] headers)
    {
        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(row, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#00338D");
            cell.Style.Font.FontColor = XLColor.White;
        }
    }

    private static byte[] ToBytes(XLWorkbook wb)
    {
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}

