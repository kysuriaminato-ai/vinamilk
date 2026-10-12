using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Đại diện cho Kỹ thuật viên & Công nhân Khối Nhà máy Sản xuất (vd: Mega Factory Bình Dương).
/// </summary>
public class FactoryWorker : Employee
{
    public string ProductionShiftGroup { get; private set; } = string.Empty;
    public decimal HazardousAllowanceRate { get; private set; }
    public string MachineCertificationCode { get; private set; } = string.Empty;
    public string SafetyLevel { get; private set; } = "Level 3 - Chuẩn An toàn Nhà máy Sữa Mega";

    protected FactoryWorker() : base()
    {
    }

    public FactoryWorker(
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
        string productionShiftGroup,
        decimal hazardousAllowanceRate,
        string machineCertificationCode,
        string safetyLevel,
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
            EmployeeType.FactoryWorker,
            organizationalUnitId,
            baseSalary,
            allowance,
            managerId)
    {
        if (hazardousAllowanceRate < 0)
            throw new DomainException("Phụ cấp môi trường độc hại/tiếng ồn không thể âm.", "ERR_FACTORY_ALLOWANCE_NEGATIVE");

        ProductionShiftGroup = productionShiftGroup?.Trim() ?? string.Empty;
        HazardousAllowanceRate = hazardousAllowanceRate;
        MachineCertificationCode = machineCertificationCode?.Trim() ?? string.Empty;
        SafetyLevel = string.IsNullOrWhiteSpace(safetyLevel) ? "Level 3 - Chuẩn An toàn Nhà máy Sữa Mega" : safetyLevel.Trim();
    }

    public void UpdateShiftGroup(string newGroup)
    {
        if (string.IsNullOrWhiteSpace(newGroup))
            throw new DomainException("Tổ đội sản xuất không được để trống.", "ERR_FACTORY_GROUP_EMPTY");

        ProductionShiftGroup = newGroup.Trim();
    }

    public void UpdateCertifications(string certCode, string safetyLevel, decimal hazardousAllowance)
    {
        if (hazardousAllowance < 0)
            throw new DomainException("Phụ cấp nặng nhọc/độc hại không thể âm.", "ERR_FACTORY_ALLOWANCE_NEGATIVE");

        MachineCertificationCode = certCode?.Trim() ?? string.Empty;
        SafetyLevel = safetyLevel?.Trim() ?? SafetyLevel;
        HazardousAllowanceRate = hazardousAllowance;
    }

    /// <summary>
    /// Ghi đè phương thức tính lương ngoài giờ (Polymorphism):
    /// Khối sản xuất Nhà máy: Lương giờ cơ sở được cộng gộp hệ số phụ cấp độc hại/tiếng ồn
    /// và được hưởng thêm phụ cấp ca sản xuất chuyên sâu theo đặc thù dây chuyền Mega Factory.
    /// </summary>
    public override decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        // Đơn giá giờ có tính thêm hệ số trách nhiệm nặng nhọc/độc hại
        decimal baseHourlyRate = BaseSalary / standardMonthlyHours;
        decimal hazardousHourlyRate = HazardousAllowanceRate / standardMonthlyHours;
        decimal combinedHourlyRate = baseHourlyRate + hazardousHourlyRate;

        // Ngày thường 150%, Ngày lễ 300%
        decimal multiplier = isHoliday ? 3.0m : 1.5m;
        decimal overtimePay = hours * combinedHourlyRate * multiplier;

        // Phụ cấp tăng ca ca kíp sản xuất (hỗ trợ bồi dưỡng chống kiệt sức dây chuyền): 50,000 VNĐ / giờ OT
        decimal plantFloorBonus = hours * 50_000m;

        return Math.Round(overtimePay + plantFloorBonus, 0, MidpointRounding.AwayFromZero);
    }
}
