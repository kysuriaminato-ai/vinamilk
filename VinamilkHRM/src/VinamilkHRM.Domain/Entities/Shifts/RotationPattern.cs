using System;
using System.Linq;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Entities.OrganizationalUnits;

namespace VinamilkHRM.Domain.Entities.Shifts;

/// <summary>
/// Mẫu chu kỳ xoay ca (RotationPattern).
/// Ví dụ: 8 ngày: CA_1,CA_1,CA_2,CA_2,CA_3,CA_3,OFF,OFF (S,S,C,C,D,D,OFF,OFF).
/// </summary>
public class RotationPattern : BaseAuditableEntity
{
    public string PatternCode { get; private set; } = string.Empty;
    public string PatternName { get; private set; } = string.Empty;
    public int CycleLengthDays { get; private set; } = 8;
    public string PatternSequence { get; private set; } = string.Empty; // Chuỗi các mã ca/OFF cách nhau bởi dấu phẩy
    public string? Description { get; private set; }

    public int? OrganizationalUnitId { get; private set; }
    public virtual OrganizationalUnit? OrganizationalUnit { get; private set; }

    protected RotationPattern()
    {
    }

    public RotationPattern(
        string patternCode,
        string patternName,
        int cycleLengthDays,
        string patternSequence,
        string? description = null,
        int? organizationalUnitId = null)
    {
        if (string.IsNullOrWhiteSpace(patternCode))
            throw new DomainException("Mã mẫu chu kỳ ca không được để trống.", "ERR_ROT_CODE_EMPTY");

        if (string.IsNullOrWhiteSpace(patternName))
            throw new DomainException("Tên mẫu chu kỳ ca không được để trống.", "ERR_ROT_NAME_EMPTY");

        if (cycleLengthDays <= 0)
            throw new DomainException("Độ dài chu kỳ (ngày) phải lớn hơn 0.", "ERR_ROT_CYCLE_INVALID");

        if (string.IsNullOrWhiteSpace(patternSequence))
            throw new DomainException("Chuỗi phân bổ ca không được để trống.", "ERR_ROT_SEQUENCE_EMPTY");

        var parts = patternSequence.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != cycleLengthDays)
            throw new DomainException($"Số lượng phần tử trong chuỗi ({parts.Length}) phải đúng bằng độ dài chu kỳ ({cycleLengthDays} ngày).", "ERR_ROT_SEQUENCE_MISMATCH");

        PatternCode = patternCode.Trim().ToUpperInvariant();
        PatternName = patternName.Trim();
        CycleLengthDays = cycleLengthDays;
        PatternSequence = string.Join(",", parts);
        Description = description?.Trim();
        OrganizationalUnitId = organizationalUnitId;
    }

    public string[] GetSequenceArray()
    {
        return PatternSequence.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>
    /// Lấy mã ca làm việc tại chỉ số ngày trong chu kỳ (0-indexed).
    /// </summary>
    public string GetShiftCodeForDay(int dayIndexInCycle)
    {
        var seq = GetSequenceArray();
        int normalizedIndex = ((dayIndexInCycle % CycleLengthDays) + CycleLengthDays) % CycleLengthDays;
        return seq[normalizedIndex];
    }
}
