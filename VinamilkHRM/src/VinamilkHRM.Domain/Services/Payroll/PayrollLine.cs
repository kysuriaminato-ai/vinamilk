namespace VinamilkHRM.Domain.Services.Payroll;

/// <summary>
/// Chi tiết từng khoản mục cấu thành thu nhập, trích nộp và khấu trừ trên phiếu lương.
/// Bao gồm FormulaTrace giải thích minh bạch công thức tính để nhân viên tra cứu trực tiếp.
/// </summary>
public record PayrollLine(
    string Code,
    string Name,
    decimal Amount,
    bool IsTaxable,
    bool IsInsurable,
    string FormulaTrace);
