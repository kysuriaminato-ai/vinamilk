using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Vinamilk.HRM.Application.Payroll;
using Vinamilk.HRM.Domain.Entities.Payroll;

namespace Vinamilk.HRM.Infrastructure.Services;

/// <summary>
/// Lớp cơ sở mở rộng cho các mẫu file chuyển khoản ngân hàng (Common, Vietcombank, BIDV, Techcombank...).
/// </summary>
public abstract class BaseBankTransferTemplateFormatter : IBankTransferTemplateFormatter
{
    public abstract string TemplateCode { get; }
    public abstract string DisplayName { get; }

    protected abstract string[] GetHeaders();
    protected abstract object[] MapRowCells(BankTransferRowDto row, PayrollPeriod period);
    protected virtual int AmountColumnOneBasedIndex => 7;

    public virtual byte[] ExportCsv(PayrollPeriod period, IReadOnlyList<BankTransferRowDto> rows)
    {
        var sb = new StringBuilder();
        var headers = GetHeaders();
        sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));

        foreach (var row in rows)
        {
            var cells = MapRowCells(row, period);
            sb.AppendLine(string.Join(",", cells.Select(c => EscapeCsv(FormatCsvCell(c)))));
        }

        // Dòng tổng cộng
        var utf8Bom = Encoding.UTF8.GetPreamble();
        var bodyBytes = Encoding.UTF8.GetBytes(sb.ToString());
        var result = new byte[utf8Bom.Length + bodyBytes.Length];
        Buffer.BlockCopy(utf8Bom, 0, result, 0, utf8Bom.Length);
        Buffer.BlockCopy(bodyBytes, 0, result, utf8Bom.Length, bodyBytes.Length);
        return result;
    }

    public virtual byte[] ExportExcel(PayrollPeriod period, IReadOnlyList<BankTransferRowDto> rows)
    {
        using var wb = new XLWorkbook();
        var ws = wb.Worksheets.Add($"ChuyenKhoan_{TemplateCode}");
        var headers = GetHeaders();

        ws.Cell(1, 1).Value = $"DANH SÁCH CHI LƯƠNG CHUYỂN KHOẢN ({DisplayName}) — {period.PeriodCode}";
        ws.Cell(1, 1).Style.Font.Bold = true;
        ws.Cell(1, 1).Style.Font.FontSize = 12;

        for (int c = 0; c < headers.Length; c++)
        {
            var cell = ws.Cell(3, c + 1);
            cell.Value = headers[c];
            cell.Style.Font.Bold = true;
            cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#003DA5");
            cell.Style.Font.FontColor = XLColor.White;
        }

        int rowIdx = 4;
        foreach (var item in rows)
        {
            var cells = MapRowCells(item, period);
            for (int c = 0; c < cells.Length; c++)
            {
                var cell = ws.Cell(rowIdx, c + 1);
                if (cells[c] is decimal decVal)
                {
                    cell.Value = decVal;
                    cell.Style.NumberFormat.Format = "#,##0";
                }
                else if (cells[c] is int intVal)
                {
                    cell.Value = intVal;
                }
                else
                {
                    cell.Value = cells[c]?.ToString() ?? string.Empty;
                }
            }
            rowIdx++;
        }

        ws.Cell(rowIdx, 1).Value = "TỔNG CỘNG";
        ws.Cell(rowIdx, AmountColumnOneBasedIndex).Value = rows.Sum(r => r.Amount);
        ws.Cell(rowIdx, AmountColumnOneBasedIndex).Style.NumberFormat.Format = "#,##0";
        for (int c = 1; c <= headers.Length; c++)
        {
            ws.Cell(rowIdx, c).Style.Font.Bold = true;
            ws.Cell(rowIdx, c).Style.Fill.BackgroundColor = XLColor.FromHtml("#eef4ff");
        }

        ws.Columns().AdjustToContents();
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }

    private static string FormatCsvCell(object? value) =>
        value is decimal dec
            ? dec.ToString("0.##", CultureInfo.InvariantCulture)
            : value?.ToString() ?? string.Empty;

    private static string EscapeCsv(string input)
    {
        if (input.Contains(',') || input.Contains('"') || input.Contains('\n'))
            return $"\"{input.Replace("\"", "\"\"")}\"";
        return input;
    }
}

public sealed class CommonBankTransferTemplateFormatter : BaseBankTransferTemplateFormatter
{
    public override string TemplateCode => "COMMON";
    public override string DisplayName => "Mẫu chung liên ngân hàng (NAPAS / Citad)";

    protected override string[] GetHeaders() =>
    [
        "STT",
        "MaNV",
        "SoTaiKhoan",
        "TenChuTaiKhoan",
        "NganHang",
        "ChiNhanh",
        "SoTien",
        "NoiDungChuyenKhoan"
    ];

    protected override int AmountColumnOneBasedIndex => 7;

    protected override object[] MapRowCells(BankTransferRowDto row, PayrollPeriod period) =>
    [
        row.Stt,
        row.EmployeeCode,
        row.AccountNumber,
        row.AccountHolderUppercaseNoAccent,
        row.BankName,
        row.BankBranch,
        row.Amount,
        row.TransferDescription
    ];
}

public sealed class VietcombankTransferTemplateFormatter : BaseBankTransferTemplateFormatter
{
    public override string TemplateCode => "VCB";
    public override string DisplayName => "Mẫu chuyển khoản Vietcombank VCB-Digibiz";

    protected override string[] GetHeaders() =>
    [
        "STT",
        "TAI KHOAN THU HUONG",
        "TEN NGUOI THU HUONG",
        "SO TIEN VND",
        "NGAN HANG",
        "NOI DUNG THANH TOAN"
    ];

    protected override int AmountColumnOneBasedIndex => 4;

    protected override object[] MapRowCells(BankTransferRowDto row, PayrollPeriod period) =>
    [
        row.Stt,
        row.AccountNumber,
        row.AccountHolderUppercaseNoAccent,
        row.Amount,
        row.BankName,
        row.TransferDescription
    ];
}

