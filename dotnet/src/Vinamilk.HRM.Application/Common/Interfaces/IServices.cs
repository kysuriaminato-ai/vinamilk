using Vinamilk.HRM.Domain.Common;

namespace Vinamilk.HRM.Application.Common.Interfaces;

/// <summary>
/// Hằng số tài khoản hệ thống dùng riêng cho background job (Hangfire),
/// không có quyền phê duyệt hay mở khóa kỳ lương.
/// </summary>
public static class SystemJobAccount
{
    public const string Username = "system.job";
    public const string Email = "system.job@vinamilk.com.vn";
}

public interface ICurrentUserService
{
    int? UserId { get; }
    string Username { get; }
    int? EmployeeId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionCode);
    IReadOnlyList<string> RoleCodes => Array.Empty<string>();
    string? ScopeLevelClaim => null;
}

public static class CurrentUserServiceExtensions
{
    public static int RequireUserId(this ICurrentUserService? currentUser)
    {
        if (currentUser?.UserId is not int userId || userId <= 0)
        {
            throw new UnauthorizedAccessException(
                "Không xác định được danh tính người dùng thực hiện thao tác (UserId trống hoặc không hợp lệ).");
        }

        return userId;
    }

    public static string RequireUsername(this ICurrentUserService? currentUser)
    {
        if (currentUser is null || string.IsNullOrWhiteSpace(currentUser.Username))
        {
            throw new UnauthorizedAccessException(
                "Không xác định được tên tài khoản người dùng thực hiện thao tác (Username trống).");
        }

        return currentUser.Username;
    }
}

public interface IPiiEncryptionService
{
    string Encrypt(string? plainText);
    string Decrypt(string? cipherText);
    string ComputeDeterministicHash(string? input);
}

public interface IGenericRepository<TEntity, TKey> where TEntity : BaseAuditableEntity<TKey>
{
    Task<TEntity?> GetByIdAsync(TKey id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Update(TEntity entity);
    void SoftDelete(TEntity entity);
}

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

