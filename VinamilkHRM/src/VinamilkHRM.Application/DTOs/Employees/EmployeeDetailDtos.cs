using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Application.DTOs.Employees;

public record DependentDto(
    int Id,
    int EmployeeId,
    string FullName,
    string Relationship,
    DateOnly? DateOfBirth,
    string? TaxCode,
    string? CitizenId,
    DateOnly FromMonth,
    DateOnly? ToMonth,
    bool IsActive,
    string? Note);

public record CreateDependentRequest(
    string FullName,
    string Relationship,
    DateOnly FromMonth,
    string? TaxCode = null,
    string? CitizenId = null,
    DateOnly? DateOfBirth = null,
    DateOnly? ToMonth = null,
    string? Note = null);

public record EmployeeEducationDto(
    int Id,
    int EmployeeId,
    string DegreeLevel,
    string Major,
    string SchoolName,
    int? GraduationYear,
    string? Classification,
    string? EducationType);

public record CreateEducationRequest(
    string DegreeLevel,
    string Major,
    string SchoolName,
    int? GraduationYear = null,
    string? Classification = null,
    string? EducationType = null);

public record EmployeeCertificateDto(
    int Id,
    int EmployeeId,
    string CertificateName,
    string IssuingAuthority,
    DateOnly IssueDate,
    DateOnly? ExpiryDate,
    string? CertificateCode,
    string? ScoreOrGrade);

public record CreateCertificateRequest(
    string CertificateName,
    string IssuingAuthority,
    DateOnly IssueDate,
    DateOnly? ExpiryDate = null,
    string? CertificateCode = null,
    string? ScoreOrGrade = null);

public record EmployeeWorkHistoryDto(
    int Id,
    int EmployeeId,
    DateOnly FromDate,
    DateOnly? ToDate,
    string CompanyName,
    string? JobTitle,
    string? MainResponsibilities,
    string? ReferenceContact,
    string? Note);

public record CreateWorkHistoryRequest(
    DateOnly FromDate,
    string CompanyName,
    DateOnly? ToDate = null,
    string? JobTitle = null,
    string? MainResponsibilities = null,
    string? ReferenceContact = null,
    string? Note = null);

public record EmployeeAttachmentDto(
    int Id,
    int EmployeeId,
    string FileName,
    string DocumentType,
    string FilePath,
    long FileSizeBytes,
    string ChecksumSha256,
    DateTime UploadedAt);

public record EmployeeDetailDto(
    int Id,
    string EmployeeCode,
    string FirstName,
    string LastName,
    string FullName,
    string MaskedCitizenId,
    string? RawCitizenId, // Chỉ hiển thị khi có claim NHANSU.ViewSensitive
    DateTime DateOfBirth,
    Gender Gender,
    string Email,
    string PhoneNumber,
    string Address,
    DateTime HireDate,
    EmployeeType EmployeeType,
    int OrganizationalUnitId,
    string? UnitName,
    string? CostCenterCode,
    MinimumWageRegion MinimumWageRegion,
    decimal BaseSalary,
    decimal Allowance,
    bool IsActive,
    string? MaskedBankAccountNumber,
    string? RawBankAccountNumber, // Chỉ hiển thị khi có claim NHANSU.ViewSensitive
    string? BankName,
    string? BankBranch,
    string? TaxCode,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    string? EmergencyContactRelationship,
    int? ManagerId,
    string? ManagerName,
    int? StandardJobTitleId,
    string? StandardJobTitleName,
    int? JobGradeId,
    int SalaryStep,
    List<DependentDto> Dependents,
    List<EmployeeEducationDto> Educations,
    List<EmployeeCertificateDto> Certificates,
    List<EmployeeWorkHistoryDto> WorkHistories,
    List<EmployeeAttachmentDto> Attachments);
