using Kivilcim.Core.Abstractions;
using Kivilcim.Dal.Data;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Dal.Repositories;

/// <summary>EF Core üzerinde genel repository. Sorgular IQueryable döner; çalıştırma IQueryExecutor'a bırakılır.</summary>
public sealed class Repository<T> : IRepository<T> where T : class
{
    private readonly DbSet<T> _set;

    public Repository(ApplicationDbContext db)
    {
        _set = db.Set<T>();
    }

    public IQueryable<T> Query() => _set.AsQueryable();

    public ValueTask<T?> GetByIdAsync(params object[] keyValues) => _set.FindAsync(keyValues);

    public Task AddAsync(T entity, CancellationToken ct = default) => _set.AddAsync(entity, ct).AsTask();

    public void Remove(T entity) => _set.Remove(entity);

    public void RemoveRange(IEnumerable<T> entities) => _set.RemoveRange(entities);
}
