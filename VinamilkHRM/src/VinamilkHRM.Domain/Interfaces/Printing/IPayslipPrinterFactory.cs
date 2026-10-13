using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces.Printing;

/// <summary>
/// Factory cung cấp máy in phiếu lương đa hình (Polymorphic Payslip Printer) tương ứng với từng phân hệ nhân sự.
/// </summary>
public interface IPayslipPrinterFactory
{
    IPayslipPrinter GetPrinter(EmployeeType employeeType);
}
