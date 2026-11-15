using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace Vinamilk.HRM.Web.TagHelpers;

/// <summary>
/// Renders a Vinamilk HRM Executive Stat Card (<vnm-stat-card>) with count-up animation, icon, and trend indicator.
/// </summary>
[HtmlTargetElement("vnm-stat-card")]
public class VnmStatCardTagHelper : TagHelper
{
    public string Title { get; set; } = string.Empty;
    public string Value { get; set; } = "0";
    public string Prefix { get; set; } = string.Empty;
    public string Suffix { get; set; } = string.Empty;
    public int Decimals { get; set; } = 0;
    public string Subtitle { get; set; } = string.Empty;
    public string Trend { get; set; } = string.Empty;
    public string TrendType { get; set; } = "success"; // success | warning | danger | info
    public string Icon { get; set; } = "ti-users";
    public string Accent { get; set; } = "primary"; // primary | success | warning | danger | info

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var enc = HtmlEncoder.Default;
        var accentVar = Accent switch
        {
            "success" => "var(--vnm-success)",
            "warning" => "var(--vnm-warning)",
            "danger" => "var(--vnm-danger)",
            "info" => "var(--vnm-info)",
            _ => "var(--vnm-primary)"
        };
        var accentSoftVar = Accent switch
        {
            "success" => "var(--vnm-success-bg)",
            "warning" => "var(--vnm-warning-bg)",
            "danger" => "var(--vnm-danger-bg)",
            "info" => "var(--vnm-info-bg)",
            _ => "var(--vnm-primary-soft)"
        };

        var trendBadgeClass = TrendType switch
        {
            "success" => "vnm-badge-success",
            "warning" => "vnm-badge-warning",
            "danger" => "vnm-badge-danger",
            _ => "vnm-badge-info"
        };

        output.TagName = "div";
        output.Attributes.SetAttribute("class", "vnm-card vnm-stat-card p-3 h-100");
        output.Attributes.SetAttribute("style", $"--stat-accent: {accentVar}; --stat-accent-soft: {accentSoftVar};");

        var trendHtml = !string.IsNullOrWhiteSpace(Trend)
            ? $"<span class=\"vnm-badge {trendBadgeClass}\">{enc.Encode(Trend)}</span>"
            : string.Empty;

        output.Content.SetHtmlContent($@"
            <div class=""d-flex align-items-start justify-content-between mb-2"">
                <div>
                    <div class=""text-secondary fw-semibold fs-6 mb-1"">{enc.Encode(Title)}</div>
                    <div class=""vnm-stat-value""
                         data-vnm-countup=""{enc.Encode(Value)}""
                         data-vnm-prefix=""{enc.Encode(Prefix)}""
                         data-vnm-suffix=""{enc.Encode(Suffix)}""
                         data-vnm-decimals=""{Decimals}"">{enc.Encode(Prefix)}{enc.Encode(Value)}{enc.Encode(Suffix)}</div>
                </div>
                <div class=""vnm-stat-icon"" aria-hidden=""true"">
                    <i class=""ti {enc.Encode(Icon)}""></i>
                </div>
            </div>
            <div class=""d-flex align-items-center justify-content-between mt-2 pt-2 border-top"" style=""border-color: var(--vnm-border-subtle) !important;"">
                <span class=""text-muted fs-7"">{enc.Encode(Subtitle)}</span>
                {trendHtml}
            </div>");
    }
}

/// <summary>
/// Renders a semantic Vinamilk HRM Status Badge (<vnm-status-badge status="success" text="Đang làm việc" />).
/// </summary>
[HtmlTargetElement("vnm-status-badge")]
public class VnmStatusBadgeTagHelper : TagHelper
{
    public string Status { get; set; } = "info"; // success | warning | danger | info | neutral
    public string Text { get; set; } = string.Empty;
    public bool ShowDot { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var enc = HtmlEncoder.Default;
        var badgeClass = Status.ToLowerInvariant() switch
        {
            "active" or "success" or "approved" => "vnm-badge-success",
            "probation" or "warning" or "pending" => "vnm-badge-warning",
            "resigned" or "terminated" or "danger" or "rejected" => "vnm-badge-danger",
            "neutral" or "draft" => "vnm-badge-neutral",
            _ => "vnm-badge-info"
        };

        output.TagName = "span";
        output.Attributes.SetAttribute("class", $"vnm-badge {badgeClass}");
        var dotHtml = ShowDot ? "<span class=\"vnm-badge-dot\" aria-hidden=\"true\"></span>" : string.Empty;
        output.Content.SetHtmlContent($"{dotHtml}{enc.Encode(Text)}");
    }
}

/// <summary>
/// Renders a clean SVG Empty State component (<vnm-empty-state>).
/// </summary>
[HtmlTargetElement("vnm-empty-state")]
public class VnmEmptyStateTagHelper : TagHelper
{
    public string Title { get; set; } = "Chưa có dữ liệu hiển thị";
    public string Description { get; set; } = "Dữ liệu sẽ xuất hiện tại đây sau khi bạn tạo bản ghi mới hoặc thay đổi bộ lọc.";
    public string ActionText { get; set; } = string.Empty;
    public string ActionHref { get; set; } = "#";

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var enc = HtmlEncoder.Default;
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "vnm-empty-state");

        var actionHtml = !string.IsNullOrWhiteSpace(ActionText)
            ? $"<a href=\"{enc.Encode(ActionHref)}\" class=\"btn vnm-btn-primary mt-3\"><i class=\"ti ti-plus me-1\"></i>{enc.Encode(ActionText)}</a>"
            : string.Empty;

        output.Content.SetHtmlContent($@"
            <svg width=""120"" height=""96"" viewBox=""0 0 120 96"" fill=""none"" xmlns=""http://www.w3.org/2000/svg"" class=""mb-3"" aria-hidden=""true"">
                <rect x=""14"" y=""18"" width=""92"" height=""64"" rx=""10"" fill=""var(--vnm-primary-soft)"" stroke=""var(--vnm-primary)"" stroke-width=""2"" stroke-dasharray=""4 4""/>
                <rect x=""28"" y=""32"" width=""42"" height=""6"" rx=""3"" fill=""var(--vnm-primary)"" opacity=""0.65""/>
                <rect x=""28"" y=""44"" width=""64"" height=""5"" rx=""2.5"" fill=""var(--vnm-text-muted)"" opacity=""0.4""/>
                <rect x=""28"" y=""55"" width=""50"" height=""5"" rx=""2.5"" fill=""var(--vnm-text-muted)"" opacity=""0.3""/>
                <circle cx=""92"" cy=""66"" r=""16"" fill=""var(--vnm-bg-surface)"" stroke=""var(--vnm-primary)"" stroke-width=""2.5""/>
                <path d=""M87 66H97M92 61V71"" stroke=""var(--vnm-primary)"" stroke-width=""2.5"" stroke-linecap=""round""/>
            </svg>
            <h4 class=""fw-bold mb-1"" style=""font-size: var(--vnm-fs-md);"">{enc.Encode(Title)}</h4>
            <p class=""text-secondary mb-0 mx-auto"" style=""max-width: 440px; font-size: var(--vnm-fs-sm);"">{enc.Encode(Description)}</p>
            {actionHtml}");
    }
}

/// <summary>
/// Renders a Skeleton Shimmer placeholder (<vnm-skeleton rows="3" />).
/// </summary>
[HtmlTargetElement("vnm-skeleton")]
public class VnmSkeletonTagHelper : TagHelper
{
    public int Rows { get; set; } = 3;
    public bool ShowAvatar { get; set; } = true;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        output.TagName = "div";
        output.Attributes.SetAttribute("class", "p-3");
        output.Attributes.SetAttribute("aria-busy", "true");
        output.Attributes.SetAttribute("aria-label", "Đang tải dữ liệu...");

        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < Rows; i++)
        {
            var widthPct = i % 2 == 0 ? "85%" : "65%";
            var avatarHtml = ShowAvatar ? "<div class=\"vnm-skeleton vnm-skeleton-avatar flex-shrink-0\"></div>" : "";
            sb.Append($@"
                <div class=""d-flex align-items-center gap-3 mb-3"">
                    {avatarHtml}
                    <div class=""flex-grow-1"">
                        <div class=""vnm-skeleton vnm-skeleton-title"" style=""width: 45%;""></div>
                        <div class=""vnm-skeleton vnm-skeleton-text mb-0"" style=""width: {widthPct};""></div>
                    </div>
                </div>");
        }
        output.Content.SetHtmlContent(sb.ToString());
    }
}

