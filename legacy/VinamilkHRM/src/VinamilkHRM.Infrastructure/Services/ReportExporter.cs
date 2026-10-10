using System.Reflection;
using System.Text;
using VinamilkHRM.Domain.Interfaces.Reporting;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Dịch vụ kết xuất báo cáo nhân sự tổng hợp ra Excel, PDF và CSV (Polymorphic implementation của IReportExporter).
/// </summary>
public class ReportExporter : IReportExporter
{
    public Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string reportTitle, CancellationToken cancellationToken = default)
    {
        // Sinh cấu trúc XML Spreadsheet 2003 / TSV tương thích 100% với Microsoft Excel mà không phụ thuộc thư viện bên thứ 3
        var sb = new StringBuilder();
        sb.AppendLine("<?xml version=\"1.0\"?>");
        sb.AppendLine("<?mso-application progid=\"Excel.Sheet\"?>");
        sb.AppendLine("<Workbook xmlns=\"urn:schemas-microsoft-com:office:spreadsheet\"");
        sb.AppendLine(" xmlns:o=\"urn:schemas-microsoft-com:office:office\"");
        sb.AppendLine(" xmlns:x=\"urn:schemas-microsoft-com:office:excel\"");
        sb.AppendLine(" xmlns:ss=\"urn:schemas-microsoft-com:office:spreadsheet\">");
        sb.AppendLine(" <Worksheet ss:Name=\"BaoCao\">");
        sb.AppendLine("  <Table>");

        // Tiêu đề báo cáo
        sb.AppendLine($"   <Row><Cell><Data ss:Type=\"String\">{EscapeXml(reportTitle)}</Data></Cell></Row>");
        sb.AppendLine("   <Row></Row>");

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);

        // Hàng tiêu đề cột (Headers)
        sb.AppendLine("   <Row>");
        foreach (var prop in properties)
        {
            sb.AppendLine($"    <Cell><Data ss:Type=\"String\">{EscapeXml(prop.Name)}</Data></Cell>");
        }
        sb.AppendLine("   </Row>");

        // Hàng dữ liệu
        foreach (var item in data)
        {
            sb.AppendLine("   <Row>");
            foreach (var prop in properties)
            {
                object? val = prop.GetValue(item);
                string strVal = val?.ToString() ?? string.Empty;
                string type = (val is int or long or decimal or double or float) ? "Number" : "String";
                sb.AppendLine($"    <Cell><Data ss:Type=\"{type}\">{EscapeXml(strVal)}</Data></Cell>");
            }
            sb.AppendLine("   </Row>");
        }

        sb.AppendLine("  </Table>");
        sb.AppendLine(" </Worksheet>");
        sb.AppendLine("</Workbook>");

        byte[] result = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(result);
    }

    public Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, string reportTitle, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>");
        sb.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
        sb.AppendLine("h1 { color: #0047BA; text-align: center; }");
        sb.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 20px; font-size: 13px; }");
        sb.AppendLine("th { background-color: #0047BA; color: white; padding: 8px; border: 1px solid #0047BA; }");
        sb.AppendLine("td { padding: 6px; border: 1px solid #ddd; }");
        sb.AppendLine("tr:nth-child(even) { background-color: #f9f9f9; }");
        sb.AppendLine("</style></head><body>");

        sb.AppendLine($"<h1>{reportTitle}</h1>");
        sb.AppendLine($"<p style='text-align: center; color: #666;'>Thời gian xuất: {DateTime.UtcNow:dd/MM/yyyy HH:mm:ss} UTC</p>");

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        sb.AppendLine("<table><thead><tr>");
        foreach (var prop in properties)
        {
            sb.AppendLine($"<th>{prop.Name}</th>");
        }
        sb.AppendLine("</tr></thead><tbody>");

        foreach (var item in data)
        {
            sb.AppendLine("<tr>");
            foreach (var prop in properties)
            {
                object? val = prop.GetValue(item);
                sb.AppendLine($"<td>{val?.ToString() ?? string.Empty}</td>");
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</tbody></table></body></html>");
        byte[] result = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(result);
    }

    public Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default)
    {
        var sb = new StringBuilder();
        // UTF-8 BOM để Excel hiển thị tiếng Việt có dấu chuẩn
        sb.Append('\uFEFF');

        var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance);
        sb.AppendLine(string.Join(",", properties.Select(p => EscapeCsv(p.Name))));

        foreach (var item in data)
        {
            var lineValues = properties.Select(p =>
            {
                object? val = p.GetValue(item);
                return EscapeCsv(val?.ToString() ?? string.Empty);
            });
            sb.AppendLine(string.Join(",", lineValues));
        }

        byte[] result = Encoding.UTF8.GetBytes(sb.ToString());
        return Task.FromResult(result);
    }

    private static string EscapeXml(string input)
    {
        return input
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }

    private static string EscapeCsv(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n') || field.Contains('\r'))
        {
            return $"\"{field.Replace("\"", "\"\"")}\"";
        }
        return field;
    }
}
