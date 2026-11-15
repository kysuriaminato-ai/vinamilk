using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Vinamilk.HRM.Application.Common.Interfaces;

namespace Vinamilk.HRM.Web.ViewComponents;

public record NotificationItemVm(
    long Id,
    string Title,
    string Description,
    string TimeAgo,
    string Icon,
    string ColorClass,
    bool IsUnread,
    string TargetUrl);

public class NotificationBellViewComponent : ViewComponent
{
    private readonly IApplicationDbContext _context;

    public NotificationBellViewComponent(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IViewComponentResult> InvokeAsync()
    {
        var dbNotifications = await _context.Notifications
            .AsNoTracking()
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        var items = new List<NotificationItemVm>();

        foreach (var n in dbNotifications)
        {
            var (icon, color) = n.NotificationType switch
            {
                "WorkflowApproval" => ("ti-git-pull-request", "text-primary"),
                "WorkflowDelegation" => ("ti-user-share", "text-warning"),
                "WorkflowResult" => ("ti-circle-check", "text-success"),
                "ContractExpiry" => ("ti-alert-triangle", "text-danger"),
                _ => ("ti-bell-ringing", "text-info")
            };

            var span = DateTime.UtcNow - n.CreatedAt;
            var timeAgo = span.TotalMinutes < 1
                ? "Vừa xong"
                : span.TotalMinutes < 60
                    ? $"{(int)span.TotalMinutes} phút trước"
                    : span.TotalHours < 24
                        ? $"{(int)span.TotalHours} giờ trước"
                        : n.CreatedAt.ToLocalTime().ToString("dd/MM/yyyy HH:mm");

            items.Add(new NotificationItemVm(
                n.Id,
                n.Title,
                n.Message,
                timeAgo,
                icon,
                color,
                !n.IsRead,
                n.TargetUrl ?? "/Workflows"));
        }

        if (items.Count == 0)
        {
            items.AddRange(new[]
            {
                new NotificationItemVm(1, "[Cần duyệt] Đơn xin nghỉ chế độ Hiếu / Hỷ (3 ngày)", "Phạm Thanh Tùng gửi đơn chờ phê duyệt tại Bước 1.", "5 phút trước", "ti-git-pull-request", "text-primary", true, "/Workflows"),
                new NotificationItemVm(2, "[Cần duyệt] Đăng ký OT200 (8h ngày cuối tuần)", "Trần Thị Mai đăng ký làm thêm giờ kiểm kê kho sữa bột.", "18 phút trước", "ti-clock-plus", "text-warning", true, "/Attendance?tab=overtime"),
                new NotificationItemVm(3, "Cảnh báo Hợp đồng sắp hết hạn (30 ngày)", "3 nhân sự cần đánh giá tái ký HĐLĐ trong tháng tới.", "1 giờ trước", "ti-alert-triangle", "text-danger", false, "/Contracts")
            });
        }

        return View(items);
    }
}
