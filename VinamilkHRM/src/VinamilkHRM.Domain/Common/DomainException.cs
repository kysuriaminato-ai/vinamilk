namespace VinamilkHRM.Domain.Common;

/// <summary>
/// Ngoại lệ miền (Domain Exception) được ném ra khi vi phạm các bất biến và quy tắc nghiệp vụ trong Domain Layer.
/// </summary>
public class DomainException : Exception
{
    public string? ErrorCode { get; }

    public DomainException(string message) : base(message)
    {
    }

    public DomainException(string message, string errorCode) : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
