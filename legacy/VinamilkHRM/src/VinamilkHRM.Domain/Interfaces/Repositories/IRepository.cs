using System.Linq.Expressions;
using VinamilkHRM.Domain.Common;

namespace VinamilkHRM.Domain.Interfaces.Repositories;

/// <summary>
/// Giao diện generic repository cung cấp các thao tác CRUD cơ bản cho thực thể miền.
/// </summary>
public interface IRepository<T> where T : BaseEntity
{
    Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
