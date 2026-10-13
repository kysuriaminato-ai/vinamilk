using System.Text.Json;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.WebApi.Middleware;

/// <summary>
/// Bộ xử lý ngoại lệ toàn cục RFC 7807 (ProblemDetails).
/// Bắt lỗi DomainException trả mã 422 kèm ErrorCode ERR_*, lỗi phân quyền 403, không để lộ stack trace.
/// </summary>
public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;
    private readonly IHostEnvironment _env;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Đã xảy ra lỗi trong quá trình xử lý yêu cầu HTTP {Path}: {Message}", 
            httpContext.Request.Path, exception.Message);

        var problemDetails = new ProblemDetails
        {
            Instance = httpContext.Request.Path
        };

        switch (exception)
        {
            case DomainException domainEx:
                problemDetails.Status = StatusCodes.Status422UnprocessableEntity;
                problemDetails.Title = "Lỗi quy tắc nghiệp vụ (Domain Rule Violation)";
                problemDetails.Detail = domainEx.Message;
                problemDetails.Extensions["errorCode"] = domainEx.ErrorCode;
                break;

            case UnauthorizedAccessException authEx:
                problemDetails.Status = StatusCodes.Status403Forbidden;
                problemDetails.Title = "Truy cập bị từ chối (Forbidden)";
                problemDetails.Detail = authEx.Message;
                problemDetails.Extensions["errorCode"] = "ERR_FORBIDDEN";
                break;

            case KeyNotFoundException notFoundEx:
                problemDetails.Status = StatusCodes.Status404NotFound;
                problemDetails.Title = "Không tìm thấy tài nguyên (Not Found)";
                problemDetails.Detail = notFoundEx.Message;
                problemDetails.Extensions["errorCode"] = "ERR_NOT_FOUND";
                break;

            default:
                problemDetails.Status = StatusCodes.Status500InternalServerError;
                problemDetails.Title = "Lỗi máy chủ nội bộ (Internal Server Error)";
                // Bảo mật: Không bao giờ lộ stack trace cho người dùng
                problemDetails.Detail = "Đã xảy ra sự cố nội bộ trong quá trình xử lý. Vui lòng liên hệ Quản trị hệ thống Vinamilk HRM.";
                problemDetails.Extensions["errorCode"] = "ERR_INTERNAL_SERVER";
                break;
        }

        httpContext.Response.StatusCode = problemDetails.Status.Value;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);
        return true;
    }
}
