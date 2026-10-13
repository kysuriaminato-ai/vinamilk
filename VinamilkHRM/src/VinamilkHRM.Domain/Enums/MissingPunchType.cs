namespace VinamilkHRM.Domain.Enums;

/// <summary>
/// Loại quẹt thẻ bị quên cần giải trình.
/// </summary>
public enum MissingPunchType
{
    MissingCheckIn = 1,  // Quên quẹt vào đầu ca
    MissingCheckOut = 2, // Quên quẹt ra cuối ca
    MissingBoth = 3      // Quên cả vào và ra
}
