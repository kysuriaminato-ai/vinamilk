using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.ViewComponents;

public record SidebarMenuItem(
    string Title,
    string Icon,
    string Url,
    string? BadgeText = null,
    string? BadgeClass = null,
    string? RequiredPermission = null);

public record SidebarMenuGroup(
    string GroupTitle,
    IReadOnlyList<SidebarMenuItem> Items);

public class SidebarViewComponent : ViewComponent
{
    private readonly ICurrentUserService _currentUser;

    public SidebarViewComponent(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    public IViewComponentResult Invoke()
    {
        var currentPath = HttpContext.Request.Path.Value ?? "/";

        var groups = new List<SidebarMenuGroup>
        {
            new("QUẢN TRỊ ĐIỀU HÀNH", new List<SidebarMenuItem>
            {
                new("Dashboard Tổng quan", "ti-layout-dashboard", "/", "Live", "bg-success", "Dashboard.View"),
                new("Báo cáo BM01–BM04", "ti-file-spreadsheet", "/Reports", "Excel", "bg-primary"),
                new("Cổng ESS / MSS Mobile", "ti-device-mobile", "/ESS", "PWA", "bg-info"),
                new("AI HR Expert (RAG)", "ti-sparkles", "/AiExpert", "AI", "bg-warning text-dark"),
                new("Design System & UI", "ti-palette", "/dev/styleguide", "Phiên 3", "bg-info"),
                new("Mẫu In Quyết định A4", "ti-printer", "/dev/styleguide/print-preview")
            }),
            new("HỆ THỐNG, RBAC & BẢO MẬT", new List<SidebarMenuItem>
            {
                new("Ma trận RBAC (62 Quyền)", "ti-shield-lock", "/Security/RbacMatrix", "Phiên 4", "bg-warning text-dark", "System.View"),
                new("Nhật ký Audit & JSON Diff", "ti-history", "/Security/AuditLogs", "Live Diff", "bg-success", "System.View"),
                new("Phiên Thiết bị & Mã hóa PII", "ti-devices", "/Security/Sessions", "AES-256", "bg-primary", "System.View"),
                new("Cấu hình 2FA TOTP", "ti-device-mobile-lock", "/Auth/Setup2FA"),
                new("Đăng nhập / Đổi mật khẩu", "ti-login", "/Auth/Login"),
                new("OpenAPI / Swagger v1", "ti-api", "/swagger")
            }),
            new("HỒ SƠ & TỔ CHỨC (CORE HR)", new List<SidebarMenuItem>
            {
                new("Cơ cấu Tổ chức & Sơ đồ", "ti-sitemap", "/Organization", "32 ĐV", "bg-primary", "Departments.View"),
                new("Hồ sơ Nhân viên 360°", "ti-users", "/Employees", "55 NV", "bg-success", "Employees.View"),
                new("Hợp đồng & Phụ lục", "ti-file-certificate", "/Contracts", "3 sắp hết", "bg-danger", "Contracts.View"),
                new("Điều chuyển & Nghỉ hưu", "ti-arrows-transfer-up", "/Transfers", "Phiên 5", "bg-warning text-dark", "Transfers.View")
            }),
            new("CHẤM CÔNG & TIỀN LƯƠNG (C&B)", new List<SidebarMenuItem>
            {
                new("Workflow Engine & Duyệt đơn", "ti-git-branch", "/Workflows", "SignalR", "bg-primary"),
                new("Ca làm việc & Xếp ca Kéo-Thả", "ti-calendar-clock", "/Shifts", "3 Ca", "bg-success", "Attendance.View"),
                new("Bảng công, GPS/QR & OT", "ti-calendar-time", "/Attendance", "Phiên 6", "bg-warning text-dark", "Attendance.View"),
                new("Nghỉ phép, Thâm niên & Team", "ti-beach", "/Leaves", "+1/5 năm", "bg-info", "Leave.View"),
                new("Bảng tính lương & Thuế TNCN", "ti-cash", "/Payroll", "3P Engine", "bg-primary", "Payroll.View"),
                new("Tham số Luật 2026 & Biểu thuế", "ti-adjustments-horizontal", "/Payroll/Parameters", "2026", "bg-warning text-dark", "Payroll.View")
            }),
            new("PHÁT TRIỂN NHÂN TÀI (TALENT)", new List<SidebarMenuItem>
            {
                new("Tuyển dụng (ATS Kanban)", "ti-user-search", "/Recruitment", "Phiên 8", "bg-primary", "Recruitment.View"),
                new("Trang Careers Công khai (SEO)", "ti-world-www", "/Careers", "Public", "bg-success"),
                new("Onboarding & Tài sản", "ti-checklist", "/Onboarding", "Checklist", "bg-info", "Onboarding.View"),
                new("Đào tạo (Vinamilk Academy)", "ti-school", "/Training", "Chứng chỉ", "bg-warning text-dark", "Training.View"),
                new("Đánh giá KPI / OKR & 360°", "ti-chart-dots-3", "/Kpi", "Thưởng P3", "bg-primary", "Kpi.View"),
                new("Khen thưởng, Kỷ luật & BM03", "ti-award", "/Rewards", "BM03", "bg-danger", "Rewards.View")
            })
        };

        // Nếu người dùng đã đăng nhập thực tế, lọc menu theo quyền RBAC (Permission Policy)
        if (_currentUser.IsAuthenticated)
        {
            groups = groups
                .Select(g => new SidebarMenuGroup(
                    g.GroupTitle,
                    g.Items.Where(i => string.IsNullOrEmpty(i.RequiredPermission) || _currentUser.HasPermission(i.RequiredPermission)).ToList()))
                .Where(g => g.Items.Count > 0)
                .ToList();
        }

        ViewBag.CurrentPath = currentPath;
        return View(groups);
    }
}
