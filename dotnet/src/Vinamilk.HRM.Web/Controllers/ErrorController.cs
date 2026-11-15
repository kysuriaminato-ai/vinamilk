using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Vinamilk.HRM.Web.Controllers;

[AllowAnonymous]
[Route("Error")]
public class ErrorController : Controller
{
    [HttpGet("{statusCode:int}")]
    public IActionResult ErrorStatus(int statusCode)
    {
        ViewBag.StatusCode = statusCode;
        (ViewBag.ErrorTitle, ViewBag.ErrorSubtitle, ViewBag.ErrorBadge) = statusCode switch
        {
            403 => (
                "Truy cập bị từ chối theo Chính sách RBAC (403 Forbidden)",
                "Tài khoản của bạn chưa được cấp quyền (Permission Policy) hoặc phạm vi Đơn vị (Data Scope) để truy cập tài nguyên này. Thao tác đã được ghi vào SecurityEvent.",
                "RBAC Policy Violation"
            ),
            404 => (
                "Không tìm thấy trang hoặc hồ sơ yêu cầu (404 Not Found)",
                "Đường dẫn có thể đã thay đổi, hoặc bản ghi nhân sự đã được chuyển vào trạng thái Lưu trữ mềm (Soft-Delete Global Filter).",
                "Resource Not Found"
            ),
            _ => (
                "Gián đoạn dịch vụ máy chủ nội bộ (500 Internal Server Error)",
                "Hệ thống vừa ghi nhận một ngoại lệ kỹ thuật vào Serilog & SQL Server. Đội ngũ Kỹ thuật Nền tảng Vinamilk HRM đã nhận được cảnh báo tự động.",
                "Serilog Correlation Tracked"
            )
        };

        ViewData["Title"] = $"Lỗi {statusCode}";
        return View("ErrorStatus");
    }

    [HttpGet("Maintenance")]
    public IActionResult Maintenance()
    {
        ViewData["Title"] = "Hệ thống đang Bảo trì Nâng cấp";
        return View("Maintenance");
    }
}

