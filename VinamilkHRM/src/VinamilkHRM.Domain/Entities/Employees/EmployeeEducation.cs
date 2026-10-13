using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Employees;

/// <summary>
/// Quá trình đào tạo học vấn và văn bằng tốt nghiệp của nhân sự.
/// </summary>
public class EmployeeEducation : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }

    public string DegreeLevel { get; private set; } = string.Empty; // Đại học, Thạc sĩ, Tiến sĩ, Cao đẳng, Trung cấp
    public string Major { get; private set; } = string.Empty;       // Công nghệ thực phẩm, Kỹ thuật cơ điện, Chăn nuôi thú y...
    public string SchoolName { get; private set; } = string.Empty;  // Tên trường đại học / cao đẳng
    public int? GraduationYear { get; private set; }
    public string? Classification { get; private set; }            // Xuất sắc, Giỏi, Khá, Trung bình
    public string? EducationType { get; private set; }              // Chính quy, Vừa học vừa làm, Liên thông

    protected EmployeeEducation()
    {
    }

    public EmployeeEducation(
        int employeeId,
        string degreeLevel,
        string major,
        string schoolName,
        int? graduationYear = null,
        string? classification = null,
        string? educationType = null)
    {
        if (employeeId <= 0)
            throw new DomainException("Mã nhân viên học vấn không hợp lệ.", "ERR_EDU_EMP_INVALID");

        if (string.IsNullOrWhiteSpace(degreeLevel))
            throw new DomainException("Trình độ học vấn không được để trống.", "ERR_EDU_LEVEL_EMPTY");

        if (string.IsNullOrWhiteSpace(major))
            throw new DomainException("Chuyên ngành đào tạo không được để trống.", "ERR_EDU_MAJOR_EMPTY");

        if (string.IsNullOrWhiteSpace(schoolName))
            throw new DomainException("Tên trường đào tạo không được để trống.", "ERR_EDU_SCHOOL_EMPTY");

        EmployeeId = employeeId;
        DegreeLevel = degreeLevel.Trim();
        Major = major.Trim();
        SchoolName = schoolName.Trim();
        GraduationYear = graduationYear;
        Classification = classification?.Trim();
        EducationType = educationType?.Trim();
    }
}
