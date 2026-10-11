using System.Text;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;
using VinamilkHRM.Domain.Entities.Salaries;
using VinamilkHRM.Domain.Interfaces.Printing;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Dịch vụ kết xuất và in phiếu lương nhân viên Vinamilk.
/// Đóng vai trò Dispatcher điều phối (Polymorphic Delegation) qua IPayslipPrinterFactory
/// để xuất mẫu phiếu lương chính xác tương ứng với từng khối nhân sự (Văn phòng, Nhà máy, Chuỗi cửa hàng).
/// </summary>
public class PayslipPrinter : IPayslipPrinter
{
    private readonly IPayslipPrinterFactory _printerFactory;

    public PayslipPrinter(IPayslipPrinterFactory printerFactory)
    {
        _printerFactory = printerFactory ?? throw new ArgumentNullException(nameof(printerFactory));
    }

    public byte[] PrintPayslipToPdf(PaySlip payslip, Employee employee, OrganizationalUnit unit)
    {
        var printer = _printerFactory.GetPrinter(employee.EmployeeType);
        return printer.PrintPayslipToPdf(payslip, employee, unit);
    }

    public byte[] PrintBatchPayslipsToPdf(IEnumerable<(PaySlip Payslip, Employee Employee, OrganizationalUnit Unit)> batch)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html><html><head><meta charset='utf-8'/><style>.page-break{page-break-after:always;}</style></head><body>");

        foreach (var item in batch)
        {
            var printer = _printerFactory.GetPrinter(item.Employee.EmployeeType);
            sb.AppendLine("<div class='page-break'>");
            sb.AppendLine(printer.GenerateHtmlPreview(item.Payslip, item.Employee, item.Unit));
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body></html>");
        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    public string GenerateHtmlPreview(PaySlip payslip, Employee employee, OrganizationalUnit unit)
    {
        var printer = _printerFactory.GetPrinter(employee.EmployeeType);
        return printer.GenerateHtmlPreview(payslip, employee, unit);
    }
}
