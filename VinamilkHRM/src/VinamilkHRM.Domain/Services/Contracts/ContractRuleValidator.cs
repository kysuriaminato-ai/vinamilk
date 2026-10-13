using System.Collections.Generic;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.Contracts;
using VinamilkHRM.Domain.Entities.Employees;
using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Services.Contracts;

/// <summary>
/// Bộ kiểm tra quy tắc pháp lý hợp đồng lao động theo Bộ luật Lao động 2019.
/// </summary>
public static class ContractRuleValidator
{
    /// <summary>
    /// Kiểm soát việc ký hợp đồng xác định thời hạn tối đa 2 lần cho cùng một nhân sự theo Điều 20 BLLĐ 2019.
    /// Ngoại lệ cấu hình: Người lao động cao tuổi (Điều 148), Người lao động nước ngoài (Điều 151), Người quản lý doanh nghiệp.
    /// </summary>
    public static void ValidateDefiniteContractLimit(
        Employee employee,
        ContractType newContractType,
        IEnumerable<LaborContract> previousContracts,
        bool isExempted = false,
        string? exemptionReason = null)
    {
        if (newContractType != ContractType.Definite)
            return;

        int existingDefiniteCount = previousContracts
            .Count(c => c.ContractType == ContractType.Definite);

        if (existingDefiniteCount >= 2)
        {
            if (isExempted)
            {
                // Nếu được miễn trừ theo diện ngoại lệ hợp pháp (người cao tuổi, người nước ngoài, ban lãnh đạo)
                if (string.IsNullOrWhiteSpace(exemptionReason))
                {
                    throw new DomainException(
                        "Trường hợp ngoại lệ ký hợp đồng xác định thời hạn lần thứ 3 phải có lý do và căn cứ pháp lý rõ ràng.",
                        "ERR_CONTRACT_EXEMPTION_REASON_EMPTY");
                }
                return;
            }

            // Vi phạm quy định: Ký HĐLĐ xác định thời hạn lần thứ 3
            throw new DomainException(
                $"Nhân viên {employee.FullName} (Mã: {employee.EmployeeCode}) đã ký {existingDefiniteCount} lần hợp đồng xác định thời hạn. " +
                "Theo quy định tại Điều 20 Bộ luật Lao động 2019, người sử dụng lao động không được ký hợp đồng xác định thời hạn lần thứ 3 cho cùng một người lao động. " +
                "Hợp đồng tiếp theo bắt buộc phải là Hợp đồng không xác định thời hạn.",
                "ERR_CONTRACT_THIRD_DEFINITE");
        }
    }
}