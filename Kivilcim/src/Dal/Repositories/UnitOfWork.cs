using Kivilcim.Core.Abstractions;
using Kivilcim.Dal.Data;

namespace Kivilcim.Dal.Repositories;

/// <summary>
/// Unit of Work: aynı DbContext'i paylaşan repository'leri tutar. Bir istek içinde
/// yapılan tüm değişiklikler <see cref="SaveChangesAsync"/> çağrısıyla birlikte kaydedilir.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;
    private readonly Dictionary<Type, object> _repositories = new();

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
    }

    public IRepository<T> Repository<T>() where T : class
    {
        if (!_repositories.TryGetValue(typeof(T), out var repo))
        {
            repo = new Repository<T>(_db);
            _repositories[typeof(T)] = repo;
        }
        return (IRepository<T>)repo;
    }

    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);
}
