using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Giao diện cung cấp tham số pháp lý nạp động từ CSDL có cache.
/// Tuyệt đối không dùng giá trị mặc định, ném lỗi DomainException nếu thiếu tham số tại ngày tính toán.
/// </summary>
public interface IStatutoryParameterProvider
{
    /// <summary>
    /// Lấy giá trị tham số pháp lý theo mã tại một ngày cụ thể (đồng bộ - phục vụ domain logic & unit test).
    /// </summary>
    decimal Get(string code, DateOnly onDate);

    /// <summary>
    /// Lấy giá trị tham số pháp lý theo mã tại một ngày cụ thể (bất đồng bộ).
    /// </summary>
    Task<decimal> GetAsync(string code, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy mức lương tối thiểu vùng tại một ngày cụ thể.
    /// </summary>
    MinimumWage GetMinimumWage(MinimumWageRegion region, DateOnly onDate);
    Task<MinimumWage> GetMinimumWageAsync(MinimumWageRegion region, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy các bậc thuế lũy tiến theo biểu thuế tại một ngày cụ thể.
    /// </summary>
    IReadOnlyList<TaxBracket> GetTaxBrackets(string scheduleCode, DateOnly onDate);
    Task<IReadOnlyList<TaxBracket>> GetTaxBracketsAsync(string scheduleCode, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy danh sách tỷ lệ đóng bảo hiểm bắt buộc tại một ngày cụ thể.
    /// </summary>
    IReadOnlyList<InsuranceRate> GetInsuranceRates(DateOnly onDate);
    Task<IReadOnlyList<InsuranceRate>> GetInsuranceRatesAsync(DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Lấy hệ số lương làm thêm giờ (OT multiplier).
    /// </summary>
    decimal GetOvertimeMultiplier(OvertimeDayKind dayKind, bool isNight, DateOnly onDate);
    Task<decimal> GetOvertimeMultiplierAsync(OvertimeDayKind dayKind, bool isNight, DateOnly onDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra ngày có phải là ngày nghỉ lễ tết có hưởng lương.
    /// </summary>
    bool IsPublicHoliday(DateOnly date);
    Task<bool> IsPublicHolidayAsync(DateOnly date, CancellationToken cancellationToken = default);
}
