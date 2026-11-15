using Microsoft.AspNetCore.Mvc;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.ViewComponents;

public record SidebarMenuItem(
    string Title,
    string Icon,
    string Url,
    string? BadgeText = null,
    string? BadgeClass = null,
    string? RequiredPermission = null,
    bool OpenInNewTab = false);

public record SidebarMenuGroup(
    string Key,
    string GroupTitle,
    string Icon,
    IReadOnlyList<SidebarMenuItem> Items);

public class SidebarViewComponent : ViewComponent
{
    private readonly ICurrentUserService _currentUser;
    private readonly IWebHostEnvironment _env;

    public SidebarViewComponent(ICurrentUserService currentUser, IWebHostEnvironment env)
    {
        _currentUser = currentUser;
        _env = env;
    }

    public IViewComponentResult Invoke()
    {
        var currentPath = HttpContext.Request.Path.Value ?? "/";

        var groups = new List<SidebarMenuGroup>
        {
            new("overview", "Tổng quan", "ti-layout-dashboard", new List<SidebarMenuItem>
            {
                new("Dashboard", "ti-layout-dashboard", "/", RequiredPermission: "Dashboard.View"),
                new("Báo cáo BM01–BM04", "ti-file-spreadsheet", "/Reports"),
                new("Trợ lý AI nhân sự", "ti-sparkles", "/AiExpert")
            }),
            new("corehr", "Nhân sự", "ti-users", new List<SidebarMenuItem>
            {
                new("Cơ cấu tổ chức", "ti-sitemap", "/Organization", RequiredPermission: "Departments.View"),
                new("Hồ sơ nhân viên", "ti-id-badge-2", "/Employees", RequiredPermission: "Employees.View"),
                new("Hợp đồng lao động", "ti-file-certificate", "/Contracts", RequiredPermission: "Contracts.View"),
                new("Điều chuyển & nghỉ hưu", "ti-arrows-exchange", "/Transfers", RequiredPermission: "Transfers.View")
            }),
            new("time", "Chấm công", "ti-clock", new List<SidebarMenuItem>
            {
                new("Ca làm việc & ngày lễ", "ti-calendar-clock", "/Shifts", RequiredPermission: "Attendance.View"),
                new("Bảng chấm công", "ti-calendar-check", "/Attendance", RequiredPermission: "Attendance.View"),
                new("Nghỉ phép", "ti-beach", "/Leaves", RequiredPermission: "Leave.View"),
                new("Duyệt đơn", "ti-checklist", "/Workflows")
            }),
            new("payroll", "Tiền lương", "ti-cash", new List<SidebarMenuItem>
            {
                new("Bảng lương", "ti-cash", "/Payroll", RequiredPermission: "Payroll.View"),
                new("Tham số lương & thuế", "ti-adjustments-horizontal", "/Payroll/Parameters", RequiredPermission: "Payroll.View")
            }),
            new("talent", "Phát triển nhân tài", "ti-star", new List<SidebarMenuItem>
            {
                new("Tuyển dụng", "ti-user-search", "/Recruitment", RequiredPermission: "Recruitment.View"),
                new("Onboarding & tài sản", "ti-clipboard-check", "/Onboarding", RequiredPermission: "Onboarding.View"),
                new("Đào tạo", "ti-school", "/Training", RequiredPermission: "Training.View"),
                new("Đánh giá KPI", "ti-chart-dots-3", "/Kpi", RequiredPermission: "Kpi.View"),
                new("Khen thưởng & kỷ luật", "ti-award", "/Rewards", RequiredPermission: "Rewards.View"),
                new("Trang tuyển dụng công khai", "ti-world-www", "/Careers", OpenInNewTab: true)
            }),
            new("me", "Cá nhân", "ti-user-circle", new List<SidebarMenuItem>
            {
                new("Cổng nhân viên (ESS)", "ti-device-mobile", "/ESS"),
                new("Đổi mật khẩu", "ti-key", "/Auth/ChangePassword"),
                new("Xác thực 2 lớp (2FA)", "ti-shield-lock", "/Auth/Setup2FA")
            }),
            new("system", "Hệ thống", "ti-settings", new List<SidebarMenuItem>
            {
                new("Phân quyền", "ti-lock-access", "/Security/RbacMatrix", RequiredPermission: "System.View"),
                new("Nhật ký hệ thống", "ti-history", "/Security/AuditLogs", RequiredPermission: "System.View"),
                new("Phiên đăng nhập", "ti-devices", "/Security/Sessions", RequiredPermission: "System.View")
            })
        };

        // Công cụ dành cho lập trình viên: chỉ hiển thị ở môi trường Development
        if (_env.IsDevelopment())
        {
            groups.Add(new("dev", "Công cụ phát triển", "ti-code", new List<SidebarMenuItem>
            {
                new("Design System", "ti-palette", "/dev/styleguide"),
                new("Mẫu in A4", "ti-printer", "/dev/styleguide/print-preview"),
                new("API (Swagger)", "ti-api", "/swagger", OpenInNewTab: true)
            }));
        }

        // Lọc menu theo quyền RBAC
        if (_currentUser.IsAuthenticated)
        {
            groups = groups
                .Select(g => g with
                {
                    Items = g.Items.Where(i => string.IsNullOrEmpty(i.RequiredPermission) || _currentUser.HasPermission(i.RequiredPermission)).ToList()
                })
                .Where(g => g.Items.Count > 0)
                .ToList();
        }

        // Mục đang active = URL khớp dài nhất (vd /Payroll/Parameters thắng /Payroll)
        var activeUrl = groups.SelectMany(g => g.Items)
            .Select(i => i.Url)
            .Where(u => u == "/"
                ? currentPath == "/"
                : currentPath.Equals(u, StringComparison.OrdinalIgnoreCase)
                  || currentPath.StartsWith(u + "/", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(u => u.Length)
            .FirstOrDefault();

        ViewBag.ActiveUrl = activeUrl;
        return View(groups);
    }
}
