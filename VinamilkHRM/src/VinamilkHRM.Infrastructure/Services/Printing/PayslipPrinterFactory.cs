using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces.Printing;

namespace VinamilkHRM.Infrastructure.Services.Printing;

/// <summary>
/// Triển khai thực tế của IPayslipPrinterFactory.
/// Áp dụng mẫu thiết kế Factory Pattern kết hợp Tính đa hình (Polymorphism) để phân giải
/// đúng Printer mẫu in phù hợp cho từng loại đối tượng nhân sự Vinamilk.
/// </summary>
public class PayslipPrinterFactory : IPayslipPrinterFactory
{
    private readonly OfficePayslipPrinter _officePrinter;
    private readonly FactoryPayslipPrinter _factoryPrinter;
    private readonly RetailStorePayslipPrinter _retailStorePrinter;

    public PayslipPrinterFactory()
    {
        _officePrinter = new OfficePayslipPrinter();
        _factoryPrinter = new FactoryPayslipPrinter();
        _retailStorePrinter = new RetailStorePayslipPrinter();
    }

    public IPayslipPrinter GetPrinter(EmployeeType employeeType)
    {
        return employeeType switch
        {
            EmployeeType.Office => _officePrinter,
            EmployeeType.FactoryWorker => _factoryPrinter,
            EmployeeType.StoreAssociate => _retailStorePrinter,
            EmployeeType.FarmSpecialist => _officePrinter,
            _ => _officePrinter
        };
    }
}
