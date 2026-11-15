using Vinamilk.HRM.Domain.Common;

namespace Vinamilk.HRM.Application.Common.Interfaces;

public interface ICurrentUserService
{
    int? UserId { get; }
    string Username { get; }
    int? EmployeeId { get; }
    string? IpAddress { get; }
    string? UserAgent { get; }
    bool IsAuthenticated { get; }
    bool HasPermission(string permissionCode);
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

