namespace Vinamilk.HRM.Domain.ValueObjects;

public sealed record Money(decimal Amount, string Currency = "VND")
{
    public static Money Zero(string currency = "VND") => new(0m, currency);

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Không thể cộng hai đơn vị tiền tệ khác nhau.");
        return new Money(Math.Round(Amount + other.Amount, 2), Currency);
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException("Không thể trừ hai đơn vị tiền tệ khác nhau.");
        return new Money(Math.Round(Amount - other.Amount, 2), Currency);
    }
}

public sealed record DateRange(DateOnly StartDate, DateOnly? EndDate)
{
    public bool IsActiveOn(DateOnly date) =>
        date >= StartDate && (!EndDate.HasValue || date <= EndDate.Value);
}

