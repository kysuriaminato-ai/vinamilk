using System;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.P2;

// 1. ATS (Applicant Tracking System)
public class JobRequisition : BaseAuditableEntity
{
    public string RequisitionCode { get; private set; } = string.Empty;
    public string Title { get; private set; } = string.Empty;
    public int OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }
    public int HeadcountNeeded { get; private set; } = 1;
    public decimal BudgetSalary { get; private set; }
    public string Status { get; private set; } = "Open"; // "Draft", "Approved", "Open", "Filled", "Cancelled"
    public DateTime Deadline { get; private set; }

    protected JobRequisition() { }

    public JobRequisition(string requisitionCode, string title, int organizationalUnitId, int headcountNeeded, decimal budgetSalary, DateTime deadline)
    {
        RequisitionCode = requisitionCode.Trim().ToUpperInvariant();
        Title = title.Trim();
        OrganizationalUnitId = organizationalUnitId;
        HeadcountNeeded = headcountNeeded;
        BudgetSalary = budgetSalary;
        Deadline = deadline;
        Status = "Open";
    }
}

// 2. Đào tạo + Chứng chỉ có hạn (Training & Certification)
public class TrainingCourse : BaseAuditableEntity
{
    public string CourseCode { get; private set; } = string.Empty;
    public string CourseName { get; private set; } = string.Empty;
    public string Provider { get; private set; } = "Vinamilk Academy";
    public int DurationHours { get; private set; } = 8;
    public decimal CostPerPerson { get; private set; } = 0m;
    public bool IsMandatory { get; private set; } = true;

    protected TrainingCourse() { }

    public TrainingCourse(string courseCode, string courseName, string provider, int durationHours, decimal costPerPerson, bool isMandatory)
    {
        CourseCode = courseCode.Trim().ToUpperInvariant();
        CourseName = courseName.Trim();
        Provider = provider.Trim();
        DurationHours = durationHours;
        CostPerPerson = costPerPerson;
        IsMandatory = isMandatory;
    }
}

public class EmployeeCertification : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }
    public string CertificateName { get; private set; } = string.Empty;
    public string IssuingBody { get; private set; } = string.Empty;
    public string CertificateNumber { get; private set; } = string.Empty;
    public DateOnly IssueDate { get; private set; }
    public DateOnly? ExpiryDate { get; private set; }
    public string Status { get; private set; } = "Active"; // "Active", "Expired", "RenewalPending"

    protected EmployeeCertification() { }

    public EmployeeCertification(int employeeId, string certificateName, string issuingBody, string certificateNumber, DateOnly issueDate, DateOnly? expiryDate)
    {
        EmployeeId = employeeId;
        CertificateName = certificateName.Trim();
        IssuingBody = issuingBody.Trim();
        CertificateNumber = certificateNumber.Trim();
        IssueDate = issueDate;
        ExpiryDate = expiryDate;
        Status = expiryDate.HasValue && expiryDate.Value < DateOnly.FromDateTime(DateTime.Today) ? "Expired" : "Active";
    }
}

// 3. Năng lực / 360° / 9 ô (Competency, 360, 9-Box Grid)
public class CompetencyFramework : BaseAuditableEntity
{
    public string CompetencyCode { get; private set; } = string.Empty;
    public string CompetencyName { get; private set; } = string.Empty;
    public string Description { get; private set; } = string.Empty;
    public int TargetLevel { get; private set; } = 3; // 1 - 5

    protected CompetencyFramework() { }

    public CompetencyFramework(string code, string name, string description, int targetLevel)
    {
        CompetencyCode = code.Trim().ToUpperInvariant();
        CompetencyName = name.Trim();
        Description = description.Trim();
        TargetLevel = targetLevel;
    }
}

public class Evaluation360 : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }
    public int EvaluatorId { get; private set; }
    public string Relationship { get; private set; } = "Peer"; // "Self", "Manager", "Peer", "DirectReport"
    public string Period { get; private set; } = string.Empty; // YYYY-MM
    public decimal OverallScore { get; private set; }
    public string? Feedback { get; private set; }

    protected Evaluation360() { }

    public Evaluation360(int employeeId, int evaluatorId, string relationship, string period, decimal overallScore, string? feedback)
    {
        EmployeeId = employeeId;
        EvaluatorId = evaluatorId;
        Relationship = relationship.Trim();
        Period = period.Trim();
        OverallScore = overallScore;
        Feedback = feedback?.Trim();
    }
}

public class NineBoxPlacement : BaseAuditableEntity
{
    public int EmployeeId { get; private set; }
    public virtual Employee? Employee { get; private set; }
    public string Period { get; private set; } = string.Empty; // YYYY
    public int PerformanceLevel { get; private set; } = 2;     // 1=Low, 2=Medium, 3=High
    public int PotentialLevel { get; private set; } = 2;       // 1=Low, 2=Medium, 3=High
    public string BoxCategory { get; private set; } = "Core Player";

    protected NineBoxPlacement() { }

    public NineBoxPlacement(int employeeId, string period, int performanceLevel, int potentialLevel, string boxCategory)
    {
        EmployeeId = employeeId;
        Period = period.Trim();
        PerformanceLevel = Math.Clamp(performanceLevel, 1, 3);
        PotentialLevel = Math.Clamp(potentialLevel, 1, 3);
        BoxCategory = boxCategory.Trim();
    }
}

// 4. ATVSLĐ (An toàn vệ sinh lao động - OHS)
public class SafetyIncident : BaseAuditableEntity
{
    public string IncidentCode { get; private set; } = string.Empty;
    public DateTime IncidentDate { get; private set; }
    public int OrganizationalUnitId { get; private set; }
    public string Severity { get; private set; } = "Minor"; // "Minor", "Moderate", "Severe", "Fatal"
    public string Description { get; private set; } = string.Empty;
    public string RootCause { get; private set; } = string.Empty;
    public string CorrectiveAction { get; private set; } = string.Empty;

    protected SafetyIncident() { }

    public SafetyIncident(string incidentCode, DateTime incidentDate, int organizationalUnitId, string severity, string description, string rootCause, string correctiveAction)
    {
        IncidentCode = incidentCode.Trim().ToUpperInvariant();
        IncidentDate = incidentDate;
        OrganizationalUnitId = organizationalUnitId;
        Severity = severity.Trim();
        Description = description.Trim();
        RootCause = rootCause.Trim();
        CorrectiveAction = correctiveAction.Trim();
    }
}

// 5. Đa pháp nhân (Multi-Legal Entity)
public class LegalEntity : BaseAuditableEntity
{
    public string TaxCode { get; private set; } = string.Empty;
    public string LegalName { get; private set; } = string.Empty;
    public string TradingName { get; private set; } = string.Empty;
    public string RegisteredAddress { get; private set; } = string.Empty;
    public string LegalRepresentative { get; private set; } = string.Empty;

    protected LegalEntity() { }

    public LegalEntity(string taxCode, string legalName, string tradingName, string registeredAddress, string legalRepresentative)
    {
        TaxCode = taxCode.Trim();
        LegalName = legalName.Trim();
        TradingName = tradingName.Trim();
        RegisteredAddress = registeredAddress.Trim();
        LegalRepresentative = legalRepresentative.Trim();
    }
}

// 6. Trình tạo báo cáo Nhà nước (BM01 - BM04)
public class ReportTemplateDefinition : BaseAuditableEntity
{
    public string ReportCode { get; private set; } = string.Empty; // "BM01", "BM02", "BM03", "BM04"
    public string Title { get; private set; } = string.Empty;
    public string LegalBasis { get; private set; } = string.Empty;
    public string ExportFormat { get; private set; } = "Excel";

    protected ReportTemplateDefinition() { }

    public ReportTemplateDefinition(string code, string title, string legalBasis, string exportFormat = "Excel")
    {
        ReportCode = code.Trim().ToUpperInvariant();
        Title = title.Trim();
        LegalBasis = legalBasis.Trim();
        ExportFormat = exportFormat.Trim();
    }
}