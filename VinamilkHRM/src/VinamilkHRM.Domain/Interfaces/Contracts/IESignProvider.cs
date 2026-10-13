using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace VinamilkHRM.Domain.Interfaces.Contracts;

public class SignerInfo
{
    public string SignerName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string? CitizenId { get; set; }
    public int SignOrder { get; set; } = 1;
    public string Role { get; set; } = "Employee"; // "Employee", "EmployerRepresentative"
}

public class ESignRequest
{
    public string DocumentId { get; set; } = string.Empty;
    public string DocumentTitle { get; set; } = string.Empty;
    public byte[] DocumentBytes { get; set; } = Array.Empty<byte>();
    public List<SignerInfo> Signers { get; set; } = new();
    public string? CallbackUrl { get; set; }
}

public class ESignResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? SigningUrl { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}

public class ESignStatusResponse
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // "Pending", "Signed", "Rejected"
    public DateTimeOffset? SignedAt { get; set; }
    public Dictionary<string, string> SignerStatuses { get; set; } = new();
}

/// <summary>
/// Giao diện chuẩn tích hợp cổng Ký số điện tử (VNPT SmartCA, Viettel CA, FPT.eContract).
/// </summary>
public interface IESignProvider
{
    Task<ESignResponse> SendDocumentForSigningAsync(ESignRequest request, CancellationToken ct = default);
    Task<ESignStatusResponse> CheckSigningStatusAsync(string transactionId, CancellationToken ct = default);
    Task<byte[]> DownloadSignedDocumentAsync(string transactionId, CancellationToken ct = default);
}