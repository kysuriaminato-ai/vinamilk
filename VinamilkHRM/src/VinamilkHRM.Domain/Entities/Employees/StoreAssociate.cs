using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Đại diện cho Nhân viên & Cửa hàng trưởng Chuỗi Cửa hàng Giấc Mơ Sữa Việt.
/// </summary>
public class StoreAssociate : Employee
{
    public string CashRegisterId { get; private set; } = string.Empty;
    public decimal RetailSalesBonusRate { get; private set; }
    public string UniformSize { get; private set; } = "M";
    public decimal CustomerServiceRating { get; private set; } = 5.0m;

    protected StoreAssociate() : base()
    {
    }

    public StoreAssociate(
        string employeeCode,
        string firstName,
        string lastName,
        string citizenId,
        DateTime dateOfBirth,
        Gender gender,
        string email,
        string phoneNumber,
        string address,
        DateTime hireDate,
        int organizationalUnitId,
        decimal baseSalary,
        decimal allowance,
        string cashRegisterId,
        decimal retailSalesBonusRate,
        string uniformSize,
        decimal customerServiceRating,
        int? managerId = null)
        : base(
            employeeCode,
            firstName,
            lastName,
            citizenId,
            dateOfBirth,
            gender,
            email,
            phoneNumber,
            address,
            hireDate,
            EmployeeType.StoreAssociate,
            organizationalUnitId,
            baseSalary,
            allowance,
            managerId)
    {
        if (retailSalesBonusRate < 0)
            throw new DomainException("Tỷ lệ hoa hồng bán lẻ không thể âm.", "ERR_STORE_BONUS_NEGATIVE");

        if (customerServiceRating < 0 || customerServiceRating > 5.0m)
            throw new DomainException("Điểm đánh giá chất lượng phục vụ phải nằm trong khoảng từ 0 đến 5 sao.", "ERR_STORE_RATING_INVALID");

        CashRegisterId = cashRegisterId?.Trim() ?? string.Empty;
        RetailSalesBonusRate = retailSalesBonusRate;
        UniformSize = string.IsNullOrWhiteSpace(uniformSize) ? "M" : uniformSize.Trim().ToUpperInvariant();
        CustomerServiceRating = customerServiceRating;
    }

    public void UpdateSalesPerformance(decimal bonusRate, decimal rating)
    {
        if (bonusRate < 0)
            throw new DomainException("Tỷ lệ hoa hồng bán lẻ không thể âm.", "ERR_STORE_BONUS_NEGATIVE");

        if (rating < 0 || rating > 5.0m)
            throw new DomainException("Điểm chất lượng dịch vụ phải từ 0 đến 5.", "ERR_STORE_RATING_INVALID");

        RetailSalesBonusRate = bonusRate;
        CustomerServiceRating = rating;
    }

    public void UpdateStoreAssignment(string cashRegisterId, string uniformSize)
    {
        CashRegisterId = cashRegisterId?.Trim() ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(uniformSize))
            UniformSize = uniformSize.Trim().ToUpperInvariant();
    }

    /// <summary>
    /// Ghi đè phương thức tính lương ngoài giờ (Polymorphism):
    /// Khối bán lẻ: Tính theo mức lương cơ bản (150% ngày thường, 300% ngày lễ),
    /// cộng phụ cấp phục vụ khách hàng giờ cao điểm cuối tuần/ngày lễ: 40,000 VNĐ / giờ OT.
    /// </summary>
    public override decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        decimal hourlyRate = BaseSalary / standardMonthlyHours;
        decimal multiplier = isHoliday ? 3.0m : 1.5m;
        decimal overtimePay = hours * hourlyRate * multiplier;

        // Phụ cấp đứng quầy phục vụ khách hàng cao điểm ca tối / ngày lễ
        decimal retailPeakBonus = hours * 40_000m;

        return Math.Round(overtimePay + retailPeakBonus, 0, MidpointRounding.AwayFromZero);
    }
}
