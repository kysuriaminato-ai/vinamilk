namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Loại hợp đồng lao động theo quy định Bộ luật Lao động và chính sách Vinamilk.
/// </summary>
public enum ContractType
{
    Probation = 1,      // Hợp đồng thử việc
    Definite = 2,       // Hợp đồng xác định thời hạn (12 - 36 tháng)
    Indefinite = 3      // Hợp đồng không xác định thời hạn
}
