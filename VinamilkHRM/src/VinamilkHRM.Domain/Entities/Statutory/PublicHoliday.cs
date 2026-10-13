using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Statutory;

/// <summary>
/// Ngày nghỉ lễ, tết theo quy định của Bộ luật Lao động và thông báo nghỉ lễ hàng năm.
/// </summary>
public class PublicHoliday : BaseAuditableEntity
{
    public DateOnly Date { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public bool IsPaid { get; private set; } = true;
    public int Year { get; private set; }

    protected PublicHoliday()
    {
    }

    public PublicHoliday(DateOnly date, string name, bool isPaid = true)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Tên ngày nghỉ lễ không được để trống.", "ERR_HOLIDAY_NAME_EMPTY");

        Date = date;
        Name = name.Trim();
        IsPaid = isPaid;
        Year = date.Year;
    }
}
