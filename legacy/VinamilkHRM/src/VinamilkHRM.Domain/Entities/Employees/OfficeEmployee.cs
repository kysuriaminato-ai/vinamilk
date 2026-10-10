using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Đại diện cho Cán bộ / Nhân viên Khối Văn phòng Trụ sở chính (Vinamilk Tower Quận 7).
/// </summary>
public class OfficeEmployee : Employee
{
    public string JobTitle { get; private set; } = string.Empty;
    public string DeskLocation { get; private set; } = string.Empty;
    public string ExtensionNumber { get; private set; } = string.Empty;
    public string CubicleNumber { get; private set; } = string.Empty;

    protected OfficeEmployee() : base()
    {
    }

    public OfficeEmployee(
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
        string jobTitle,
        string deskLocation,
        string extensionNumber,
        string cubicleNumber,
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
            EmployeeType.Office,
            organizationalUnitId,
            baseSalary,
            allowance,
            managerId)
    {
        if (string.IsNullOrWhiteSpace(jobTitle))
            throw new DomainException("Chức danh khối văn phòng không được để trống.", "ERR_OFFICE_JOBTITLE_EMPTY");

        JobTitle = jobTitle.Trim();
        DeskLocation = deskLocation?.Trim() ?? string.Empty;
        ExtensionNumber = extensionNumber?.Trim() ?? string.Empty;
        CubicleNumber = cubicleNumber?.Trim() ?? string.Empty;
    }

    public void UpdateOfficeLocation(string deskLocation, string extensionNumber, string cubicleNumber)
    {
        DeskLocation = deskLocation?.Trim() ?? string.Empty;
        ExtensionNumber = extensionNumber?.Trim() ?? string.Empty;
        CubicleNumber = cubicleNumber?.Trim() ?? string.Empty;
    }

    public void UpdateJobTitle(string jobTitle)
    {
        if (string.IsNullOrWhiteSpace(jobTitle))
            throw new DomainException("Chức danh văn phòng không được để trống.", "ERR_OFFICE_JOBTITLE_EMPTY");

        JobTitle = jobTitle.Trim();
    }

    /// <summary>
    /// Ghi đè phương thức tính lương làm thêm giờ (Polymorphism):
    /// Khối văn phòng: Tính 150% ngày thường, 300% ngày lễ, cộng thêm phụ cấp ăn tối làm việc ngoài giờ
    /// cho các ca làm thêm từ 2 giờ trở lên (70,000 VNĐ).
    /// </summary>
    public override decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        decimal hourlyRate = BaseSalary / standardMonthlyHours;
        decimal multiplier = isHoliday ? 3.0m : 1.5m;
        decimal baseOvertime = hours * hourlyRate * multiplier;

        // Phụ cấp ăn ca ngoài giờ khối văn phòng nếu làm thêm từ 2 giờ trở lên
        decimal mealAllowance = hours >= 2.0m ? 70_000m : 0m;

        return Math.Round(baseOvertime + mealAllowance, 0, MidpointRounding.AwayFromZero);
    }
}
