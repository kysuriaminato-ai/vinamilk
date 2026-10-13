using System;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Entities.Analytics;

/// <summary>
/// Nhật ký kiểm toán AI (AiAuditLog) lưu lại prompt ẩn danh và kết quả phân tích phục vụ kiểm toán.
/// </summary>
public class AiAuditLog : BaseAuditableEntity
{
    public string FeatureName { get; private set; } = string.Empty;
    public string PromptSent { get; private set; } = string.Empty;
    public string ResponseReceived { get; private set; } = string.Empty;
    public string ModelName { get; private set; } = "gemini-1.5-pro";
    public bool IsAnonymized { get; private set; } = true;
    public int TriggeredByUserId { get; private set; }
    public DateTimeOffset Timestamp { get; private set; } = DateTimeOffset.UtcNow;

    protected AiAuditLog() { }

    public AiAuditLog(
        string featureName,
        string promptSent,
        string responseReceived,
        int triggeredByUserId,
        string modelName = "gemini-1.5-pro")
    {
        FeatureName = featureName;
        PromptSent = promptSent;
        ResponseReceived = responseReceived;
        TriggeredByUserId = triggeredByUserId;
        ModelName = modelName;
        IsAnonymized = true;
        Timestamp = DateTimeOffset.UtcNow;
    }
}