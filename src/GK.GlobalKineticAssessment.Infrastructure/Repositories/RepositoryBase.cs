using GK.GlobalKineticAssessment.Domain.Interfaces;
using GK.GlobalKineticAssessment.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace GK.GlobalKineticAssessment.Infrastructure.Repositories;

public abstract class RepositoryBase<T, TKey> : IRepository<T, TKey>
    where T : class
    where TKey : struct
{
    protected readonly AppDbContext _context;
    protected readonly DbSet<T> _dbSet;

    protected RepositoryBase(AppDbContext context)
    {
        _context = context;
        _dbSet   = context.Set<T>();
    }

    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await _dbSet.AsNoTracking().ToListAsync(ct);

    public virtual async Task<T?> GetByIdAsync(TKey id, CancellationToken ct = default) =>
        await _dbSet.FindAsync(new object?[] { id }, cancellationToken: ct);

    public virtual async Task<T> AddAsync(T entity, CancellationToken ct = default)
    {
        await _dbSet.AddAsync(entity, ct);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task<T> UpdateAsync(T entity, CancellationToken ct = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task<bool> DeleteAsync(TKey id, CancellationToken ct = default)
    {
        var entity = await GetByIdAsync(id, ct);
        if (entity is null) return false;
        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(ct);
        return true;
    }

    public virtual async Task<bool> ExistsAsync(TKey id, CancellationToken ct = default) =>
        await _dbSet.FindAsync(new object?[] { id }, cancellationToken: ct) is not null;
}
