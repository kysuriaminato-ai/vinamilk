using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using VinamilkHRM.Domain.Interfaces.Contracts;

namespace VinamilkHRM.Infrastructure.Services.Contracts;

/// <summary>
/// Nhà cung cấp Ký số giả lập (Mock ESign Provider) phục vụ kiểm thử và phát triển,
/// sẵn sàng chuyển đổi cấu hình sang VNPT SmartCA hoặc Viettel CA trong môi trường sản xuất.
/// </summary>
public class MockESignProvider : IESignProvider
{
    private readonly ILogger<MockESignProvider> _logger;

    public MockESignProvider(ILogger<MockESignProvider> logger)
    {
        _logger = logger;
    }

    public Task<ESignResponse> SendDocumentForSigningAsync(ESignRequest request, CancellationToken ct = default)
    {
        string transactionId = $"TX-ESIGN-{Guid.NewGuid():N}".Substring(0, 18).ToUpperInvariant();
        _logger.LogInformation("Đã khởi tạo phiên ký số {TxId} cho tài liệu '{Title}' ({Count} người ký).", 
            transactionId, request.DocumentTitle, request.Signers.Count);

        var response = new ESignResponse
        {
            TransactionId = transactionId,
            DocumentId = request.DocumentId,
            Status = "Pending",
            SigningUrl = $"https://esign.vinamilk.com.vn/sign/{transactionId}",
            CreatedAt = DateTimeOffset.UtcNow
        };

        return Task.FromResult(response);
    }

    public Task<ESignStatusResponse> CheckSigningStatusAsync(string transactionId, CancellationToken ct = default)
    {
        var response = new ESignStatusResponse
        {
            TransactionId = transactionId,
            Status = "Signed",
            SignedAt = DateTimeOffset.UtcNow,
            SignerStatuses = new Dictionary<string, string>
            {
                { "Employee", "Completed" },
                { "EmployerRepresentative", "Completed" }
            }
        };

        return Task.FromResult(response);
    }

    public Task<byte[]> DownloadSignedDocumentAsync(string transactionId, CancellationToken ct = default)
    {
        string mockPdf = $"%PDF-1.4\n% VINAMILK DIGITAL SIGNED DOCUMENT [{transactionId}]\n%%EOF";
        return Task.FromResult(Encoding.UTF8.GetBytes(mockPdf));
    }
}