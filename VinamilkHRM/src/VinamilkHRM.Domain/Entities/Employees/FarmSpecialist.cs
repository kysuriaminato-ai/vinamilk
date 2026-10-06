using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Đại diện cho Chuyên gia Thú y, Kỹ sư Chăn nuôi Khối Trang trại Bò sữa (vd: Resort Bò Sữa Tây Ninh).
/// </summary>
public class FarmSpecialist : Employee
{
    public string SpecializationArea { get; private set; } = string.Empty;
    public string BioSecurityClearanceCode { get; private set; } = string.Empty;
    public decimal AnimalCareAllowanceRate { get; private set; }
    public string VeterinaryLicenseNumber { get; private set; } = string.Empty;

    protected FarmSpecialist() : base()
    {
    }

    public FarmSpecialist(
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
        string specializationArea,
        string bioSecurityClearanceCode,
        decimal animalCareAllowanceRate,
        string veterinaryLicenseNumber,
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
            EmployeeType.FarmSpecialist,
            organizationalUnitId,
            baseSalary,
            allowance,
            managerId)
    {
        if (animalCareAllowanceRate < 0)
            throw new DomainException("Phụ cấp chăm sóc đàn bò không thể là số âm.", "ERR_FARM_ALLOWANCE_NEGATIVE");

        SpecializationArea = specializationArea?.Trim() ?? string.Empty;
        BioSecurityClearanceCode = bioSecurityClearanceCode?.Trim() ?? string.Empty;
        AnimalCareAllowanceRate = animalCareAllowanceRate;
        VeterinaryLicenseNumber = veterinaryLicenseNumber?.Trim() ?? string.Empty;
    }

    public void UpdateSpecialization(string specializationArea, string bioSecurityCode, decimal animalCareAllowance)
    {
        if (animalCareAllowance < 0)
            throw new DomainException("Phụ cấp chăm sóc thú y đàn bò không thể âm.", "ERR_FARM_ALLOWANCE_NEGATIVE");

        SpecializationArea = specializationArea?.Trim() ?? string.Empty;
        BioSecurityClearanceCode = bioSecurityCode?.Trim() ?? string.Empty;
        AnimalCareAllowanceRate = animalCareAllowance;
    }

    public void UpdateVeterinaryLicense(string licenseNumber)
    {
        VeterinaryLicenseNumber = licenseNumber?.Trim() ?? string.Empty;
    }

    /// <summary>
    /// Ghi đè phương thức tính lương ngoài giờ (Polymorphism):
    /// Chuyên gia trang trại: Lương OT bao gồm hệ số lương cơ bản + phụ cấp chăm sóc thú y/theo dõi sinh học,
    /// cộng thêm phụ cấp trực cấp cứu đàn bò/vắt sữa sinh thái: 60,000 VNĐ / giờ.
    /// </summary>
    public override decimal CalculateOvertimePay(decimal hours, bool isHoliday)
    {
        if (hours <= 0)
            return 0m;

        const decimal standardMonthlyHours = 176m;
        decimal baseHourlyRate = BaseSalary / standardMonthlyHours;
        decimal careHourlyRate = AnimalCareAllowanceRate / standardMonthlyHours;
        decimal combinedHourlyRate = baseHourlyRate + careHourlyRate;

        decimal multiplier = isHoliday ? 3.0m : 1.5m;
        decimal overtimePay = hours * combinedHourlyRate * multiplier;

        // Phụ cấp trực ca cấp cứu thú y & hỗ trợ đàn bê sơ sinh ngoài giờ
        decimal emergencyDutyBonus = hours * 60_000m;

        return Math.Round(overtimePay + emergencyDutyBonus, 0, MidpointRounding.AwayFromZero);
    }
}
