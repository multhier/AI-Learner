namespace Kivilcim.Core.Abstractions;

/// <summary>Bir varlık türü için veri erişimi. Sorgular <see cref="IQueryable{T}"/> olarak döner; çalıştırma <see cref="IQueryExecutor"/> ile yapılır.</summary>
public interface IRepository<T> where T : class
{
    IQueryable<T> Query();

    ValueTask<T?> GetByIdAsync(params object[] keyValues);

    Task AddAsync(T entity, CancellationToken ct = default);

    void Remove(T entity);

    void RemoveRange(IEnumerable<T> entities);
}

/// <summary>
/// Unit of Work: tüm repository'ler aynı bağlamı paylaşır, değişiklikler <see cref="SaveChangesAsync"/> ile tek işlemde kaydedilir.
/// </summary>
public interface IUnitOfWork
{
    IRepository<T> Repository<T>() where T : class;

    Task<int> SaveChangesAsync(CancellationToken ct = default);
}
