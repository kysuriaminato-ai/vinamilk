using Microsoft.AspNetCore.Http;
using VinamilkHRM.Domain.Enums;
using VinamilkHRM.Domain.Interfaces;

namespace VinamilkHRM.Infrastructure.Services;

/// <summary>
/// Dịch vụ quản lý phân vùng dữ liệu và quản lý chuỗi đa điểm của Tập đoàn Vinamilk (Regional Scoping & Multi-Unit).
/// Đọc thiết lập phân vùng từ Cookie / Session của người dùng hiện tại và cung cấp cho Global Query Filter của EF Core.
/// </summary>
public class RegionScopeService : IRegionScopeService
{
    private readonly IHttpContextAccessor? _httpContextAccessor;
    public const string CookieName = "vnm_regional_scope";

    private string? _cachedScopeCode;

    public RegionScopeService(IHttpContextAccessor? httpContextAccessor = null)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string CurrentScopeCode
    {
        get
        {
            if (_cachedScopeCode != null)
                return _cachedScopeCode;

            var httpContext = _httpContextAccessor?.HttpContext;
            if (httpContext == null)
                return "ALL";

            // Kiểm tra Query string ưu tiên cao hơn (vd: ?scope=REGION_NORTH)
            if (httpContext.Request.Query.TryGetValue("scope", out var queryScope) && !string.IsNullOrWhiteSpace(queryScope))
            {
                _cachedScopeCode = queryScope.ToString().ToUpperInvariant();
                return _cachedScopeCode;
            }

            // Kiểm tra Cookie người dùng đã chọn
            if (httpContext.Request.Cookies.TryGetValue(CookieName, out var cookieScope) && !string.IsNullOrWhiteSpace(cookieScope))
            {
                _cachedScopeCode = cookieScope.ToUpperInvariant();
                return _cachedScopeCode;
            }

            return "ALL";
        }
    }

    public int? CurrentRegionId
    {
        get
        {
            return CurrentScopeCode switch
            {
                "REGION_NORTH" => 1,
                "REGION_CENTRAL" => 2,
                "REGION_SOUTH" => 3,
                _ => null
            };
        }
    }

    public UnitType? CurrentUnitType
    {
        get
        {
            return CurrentScopeCode switch
            {
                "FACTORY_MEGA" => UnitType.Factory,
                "RETAIL_STORES" => UnitType.RetailStore,
                "DAIRY_FARMS" => UnitType.DairyFarm,
                "HQ" => UnitType.Headquarters,
                _ => null
            };
        }
    }

    public int? CurrentUnitId
    {
        get
        {
            // Trong seed database: Mega Factory có ID = 2, Cửa hàng GMSV có ID = 4 hoặc 5
            return CurrentScopeCode switch
            {
                "FACTORY_MEGA" => 2,
                _ => null
            };
        }
    }

    public string CurrentScopeName
    {
        get
        {
            return CurrentScopeCode switch
            {
                "REGION_NORTH" => "Vùng Miền Bắc (Hà Nội, Tiên Sơn,...)",
                "REGION_CENTRAL" => "Vùng Miền Trung (Đà Nẵng, Bình Định,...)",
                "REGION_SOUTH" => "Vùng Miền Nam (TP.HCM, Bình Dương, Tây Ninh)",
                "FACTORY_MEGA" => "Nhà máy Mega Bình Dương (Tetra Pak)",
                "RETAIL_STORES" => "Chuỗi Cửa hàng Giấc Mơ Sữa Việt",
                "DAIRY_FARMS" => "Trang trại Sinh thái Vinamilk Eco Farm",
                "HQ" => "Trụ sở chính Vinamilk Tower (Quận 7)",
                _ => "Toàn tập đoàn Vinamilk (Toàn quốc)"
            };
        }
    }

    public bool IsFilterActive => CurrentScopeCode != "ALL";

    public void SetScope(string scopeCode)
    {
        string normalized = string.IsNullOrWhiteSpace(scopeCode) ? "ALL" : scopeCode.Trim().ToUpperInvariant();
        _cachedScopeCode = normalized;

        var httpContext = _httpContextAccessor?.HttpContext;
        if (httpContext != null)
        {
            var cookieOptions = new CookieOptions
            {
                Expires = DateTimeOffset.UtcNow.AddDays(30),
                HttpOnly = false, // Cho phép JavaScript đọc để đồng bộ UI
                SameSite = SameSiteMode.Lax,
                Path = "/"
            };

            httpContext.Response.Cookies.Append(CookieName, normalized, cookieOptions);
        }
    }
}
