namespace VinamilkHRM.Application.Common.Interfaces;

/// <summary>
/// Quản lý phạm vi dữ liệu phân cấp (Data Scoping) theo cây đơn vị tổ chức và vai trò người dùng.
/// </summary>
public interface IDataScope
{
    /// <summary>
    /// Cho biết người dùng có quyền xem toàn bộ tập đoàn hay không (SystemAdmin, Executive, HRDirector, ChiefAccountant).
    /// </summary>
    bool IsGlobalAccess { get; }

    /// <summary>
    /// Mã nhân viên liên kết của người dùng hiện tại (nếu có).
    /// </summary>
    int? CurrentEmployeeId { get; }

    /// <summary>
    /// Lấy danh sách ID các đơn vị tổ chức mà người dùng được phép truy cập (đơn vị trực thuộc + toàn bộ đơn vị con cháu).
    /// </summary>
    Task<IReadOnlyCollection<int>> GetAuthorizedUnitIdsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra người dùng có quyền truy cập vào đơn vị cụ thể hay không.
    /// </summary>
    Task<bool> CanAccessUnitAsync(int unitId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Kiểm tra người dùng có quyền truy cập dữ liệu của nhân viên đích hay không.
    /// </summary>
    Task<bool> CanAccessEmployeeAsync(int targetEmployeeId, CancellationToken cancellationToken = default);
}
