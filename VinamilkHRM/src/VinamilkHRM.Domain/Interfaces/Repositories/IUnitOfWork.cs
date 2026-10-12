namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện Unit of Work quản lý các transaction và đảm bảo tính nhất quán dữ liệu (ACID).
/// </summary>
public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
