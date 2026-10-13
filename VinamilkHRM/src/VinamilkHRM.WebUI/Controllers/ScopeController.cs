using Microsoft.AspNetCore.Mvc;
using VinamilkHRM.Domain.Interfaces;

namespace VinamilkHRM.WebUI.Controllers;

/// <summary>
/// Bộ điều phối phân vùng dữ liệu và quản lý chuỗi đa điểm (Regional Scoping & Multi-Unit).
/// Cho phép Lãnh đạo Tập đoàn chuyển đổi tức thời giữa: Toàn tập đoàn -> Vùng Miền Bắc/Trung/Nam -> Nhà máy Mega -> Cửa hàng GMSV.
/// </summary>
public class ScopeController : Controller
{
    private readonly IRegionScopeService _regionScopeService;

    public ScopeController(IRegionScopeService regionScopeService)
    {
        _regionScopeService = regionScopeService;
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SetScope(string scopeCode, string? returnUrl = null)
    {
        _regionScopeService.SetScope(scopeCode);

        if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
        {
            return Redirect(returnUrl);
        }

        return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public IActionResult Current()
    {
        return Json(new
        {
            scopeCode = _regionScopeService.CurrentScopeCode,
            scopeName = _regionScopeService.CurrentScopeName,
            isFilterActive = _regionScopeService.IsFilterActive,
            regionId = _regionScopeService.CurrentRegionId,
            unitType = _regionScopeService.CurrentUnitType?.ToString(),
            unitId = _regionScopeService.CurrentUnitId
        });
    }
}
