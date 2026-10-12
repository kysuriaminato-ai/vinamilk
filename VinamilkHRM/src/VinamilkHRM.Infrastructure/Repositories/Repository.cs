using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using VinamilkHRM.Domain.Common;
using VinamilkHRM.Domain.Interfaces.Repositories;
using VinamilkHRM.Infrastructure.Data;

namespace VinamilkHRM.Infrastructure.Repositories;

public class Repository<T> : IRepository<T> where T : BaseEntity
{
    protected readonly VinamilkHRMDbContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(VinamilkHRMDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _dbSet = context.Set<T>();
    }

    public virtual async Task<T?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync(new object[] { id }, cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(predicate).ToListAsync(cancellationToken);
    }

    public virtual async Task AddAsync(T entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        await _dbSet.AddAsync(entity, cancellationToken);
    }

    public virtual void Update(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Update(entity);
    }

    public virtual void Remove(T entity)
    {
        ArgumentNullException.ThrowIfNull(entity);
        _dbSet.Remove(entity);
    }
}
