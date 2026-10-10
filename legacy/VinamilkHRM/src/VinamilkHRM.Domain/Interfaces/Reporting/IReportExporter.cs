namespace VinamilkHRM.Domain.Interfaces.Reporting;

/// <summary>
/// Giao diện đa hình (Polymorphism) cung cấp chức năng trích xuất các báo cáo nhân sự,
/// bảng chấm công, thống kê KPI ra các định dạng bảng tính và văn bản chuẩn (Excel, PDF, CSV).
/// </summary>
public interface IReportExporter
{
    /// <summary>
    /// Xuất dữ liệu báo cáo sang định dạng Microsoft Excel (.xlsx).
    /// </summary>
    Task<byte[]> ExportToExcelAsync<T>(IEnumerable<T> data, string reportTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất dữ liệu báo cáo sang định dạng Adobe Acrobat PDF (.pdf).
    /// </summary>
    Task<byte[]> ExportToPdfAsync<T>(IEnumerable<T> data, string reportTitle, CancellationToken cancellationToken = default);

    /// <summary>
    /// Xuất dữ liệu dạng giá trị phân tách dấu phẩy (CSV UTF-8).
    /// </summary>
    Task<byte[]> ExportToCsvAsync<T>(IEnumerable<T> data, CancellationToken cancellationToken = default);
}
