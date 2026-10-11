using VinamilkHRM.Domain.Enums;

namespace VinamilkHRM.Domain.Interfaces;

/// <summary>
/// Giao diện dịch vụ phân vùng dữ liệu và quản lý chuỗi đa điểm (Regional Scoping & Multi-Unit).
/// Cung cấp thông tin ngữ cảnh để Global Query Filter trong EF Core DbContext lọc dữ liệu tự động.
/// </summary>
public interface IRegionScopeService
{
    /// <summary>
    /// Mã định danh khu vực địa lý (1: Miền Bắc, 2: Miền Trung, 3: Miền Nam). Null nếu xem toàn quốc.
    /// </summary>
    int? CurrentRegionId { get; }

    /// <summary>
    /// Loại hình đơn vị tổ chức đang được lọc (Factory, RetailStore, DairyFarm, Headquarters). Null nếu xem tất cả.
    /// </summary>
    UnitType? CurrentUnitType { get; }

    /// <summary>
    /// Mã định danh đơn vị cụ thể (vd: 2 cho Mega Factory Bình Dương). Null nếu không giới hạn 1 đơn vị.
    /// </summary>
    int? CurrentUnitId { get; }

    /// <summary>
    /// Mã chuỗi Scope đại diện (vd: ALL, REGION_NORTH, REGION_CENTRAL, REGION_SOUTH, FACTORY_MEGA, RETAIL_STORES, DAIRY_FARMS, HQ).
    /// </summary>
    string CurrentScopeCode { get; }

    /// <summary>
    /// Tên hiển thị người dùng của phân vùng hiện tại trên thanh công cụ Header.
    /// </summary>
    string CurrentScopeName { get; }

    /// <summary>
    /// Kiểm tra xem chế độ lọc phân vùng có đang hoạt động hay không. True nếu không phải ALL.
    /// </summary>
    bool IsFilterActive { get; }

    /// <summary>
    /// Thiết lập phân vùng dữ liệu hoạt động.
    /// </summary>
    void SetScope(string scopeCode);
}
