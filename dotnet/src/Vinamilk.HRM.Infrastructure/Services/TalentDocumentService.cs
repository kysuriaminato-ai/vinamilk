using ClosedXML.Excel;
using Vinamilk.HRM.Application.Talent;

namespace Vinamilk.HRM.Infrastructure.Services;

public class TalentDocumentService
{
    public byte[] ExportBm03RewardDisciplineExcel(RewardsAndBm03OverviewDto data)
    {
        using var workbook = new XLWorkbook();
        var wsSummary = workbook.Worksheets.Add("BM03_TongHop_DonVi");

        wsSummary.Cell(1, 1).Value = "CÔNG TY CỔ PHẦN SỮA VIỆT NAM (VINAMILK)";
        wsSummary.Cell(1, 1).Style.Font.Bold = true;
        wsSummary.Cell(1, 1).Style.Font.FontColor = XLColor.FromHtml("#003DA5");

        wsSummary.Cell(2, 1).Value = $"BIỂU MẪU BM03 — BÁO CÁO TÌNH HÌNH KHEN THƯỞNG & KỶ LUẬT NĂM {data.Year}";
        wsSummary.Cell(2, 1).Style.Font.Bold = true;
        wsSummary.Cell(2, 1).Style.Font.FontSize = 13;

        var headers = new[] { "STT", "Mã Đơn vị", "Tên Đơn vị", "Số lượt Khen thưởng", "Tổng tiền Thưởng (VNĐ)", "Số lượt Kỷ luật", "Tổng Khấu trừ Kỷ luật (VNĐ)" };
        for (int i = 0; i < headers.Length; i++)
        {
            var c = wsSummary.Cell(4, i + 1);
            c.Value = headers[i];
            c.Style.Font.Bold = true;
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
        }

        int row = 5;
        int stt = 1;
        foreach (var u in data.Bm03UnitSummaries)
        {
            wsSummary.Cell(row, 1).Value = stt++;
            wsSummary.Cell(row, 2).Value = u.UnitCode;
            wsSummary.Cell(row, 3).Value = u.UnitName;
            wsSummary.Cell(row, 4).Value = u.RewardCount;
            wsSummary.Cell(row, 5).Value = u.TotalRewardAmount;
            wsSummary.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
            wsSummary.Cell(row, 6).Value = u.DisciplineCount;
            wsSummary.Cell(row, 7).Value = u.TotalDisciplineAmount;
            wsSummary.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
            row++;
        }

        wsSummary.Cell(row, 3).Value = "TỔNG CỘNG TOÀN CÔNG TY";
        wsSummary.Cell(row, 3).Style.Font.Bold = true;
        wsSummary.Cell(row, 4).Value = data.TotalRewardCount;
        wsSummary.Cell(row, 5).Value = data.TotalRewardAmount;
        wsSummary.Cell(row, 5).Style.NumberFormat.Format = "#,##0";
        wsSummary.Cell(row, 6).Value = data.TotalDisciplineCount;
        wsSummary.Cell(row, 7).Value = data.TotalDisciplineAmount;
        wsSummary.Cell(row, 7).Style.NumberFormat.Format = "#,##0";
        wsSummary.Range(row, 1, row, 7).Style.Font.Bold = true;
        wsSummary.Columns().AdjustToContents();

        var wsDetail = workbook.Worksheets.Add("BM03_ChiTiet_QuyetDinh");
        var detailHeaders = new[] { "Số Quyết định", "Loại", "Mã NV", "Họ và tên", "Đơn vị", "Hình thức", "Ngày QĐ", "Số tiền (VNĐ)", "Lý do" };
        for (int i = 0; i < detailHeaders.Length; i++)
        {
            var c = wsDetail.Cell(1, i + 1);
            c.Value = detailHeaders[i];
            c.Style.Font.Bold = true;
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
        }

        int dRow = 2;
        foreach (var item in data.Items)
        {
            wsDetail.Cell(dRow, 1).Value = item.DecisionNumber;
            wsDetail.Cell(dRow, 2).Value = item.Type == "Reward" ? "Khen thưởng" : "Kỷ luật";
            wsDetail.Cell(dRow, 3).Value = item.EmployeeCode;
            wsDetail.Cell(dRow, 4).Value = item.EmployeeName;
            wsDetail.Cell(dRow, 5).Value = item.UnitName;
            wsDetail.Cell(dRow, 6).Value = item.CategoryName;
            wsDetail.Cell(dRow, 7).Value = item.DecisionDate.ToString("dd/MM/yyyy");
            wsDetail.Cell(dRow, 8).Value = item.Amount;
            wsDetail.Cell(dRow, 8).Style.NumberFormat.Format = "#,##0";
            wsDetail.Cell(dRow, 9).Value = item.Reason ?? "";
            dRow++;
        }
        wsDetail.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }

    public byte[] ExportTrainingPlanExcel(TrainingOverviewDto data)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add($"DaoTao_{data.Year}");

        ws.Cell(1, 1).Value = $"VINAMILK ACADEMY — KẾ HOẠCH & CHI PHÍ ĐÀO TẠO NĂM {data.Year}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 13;

        var headers = new[] { "Mã Lớp", "Tên Lớp học", "Khóa học", "Từ ngày", "Đến ngày", "Giảng viên", "Sĩ số", "Tổng chi phí (VNĐ)", "Trạng thái" };
        for (int i = 0; i < headers.Length; i++)
        {
            var c = ws.Cell(3, i + 1);
            c.Value = headers[i];
            c.Style.Font.Bold = true;
            c.Style.Font.FontColor = XLColor.White;
            c.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
        }

        int r = 4;
        foreach (var cls in data.Classes)
        {
            ws.Cell(r, 1).Value = cls.ClassCode;
            ws.Cell(r, 2).Value = cls.ClassName;
            ws.Cell(r, 3).Value = cls.CourseName;
            ws.Cell(r, 4).Value = cls.StartDate.ToString("dd/MM/yyyy");
            ws.Cell(r, 5).Value = cls.EndDate.ToString("dd/MM/yyyy");
            ws.Cell(r, 6).Value = cls.InstructorName ?? "";
            ws.Cell(r, 7).Value = $"{cls.EnrolledCount}/{cls.MaxCapacity}";
            ws.Cell(r, 8).Value = cls.TotalActualCost;
            ws.Cell(r, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(r, 9).Value = cls.Status;
            r++;
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}

