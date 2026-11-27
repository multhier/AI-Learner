namespace Kivilcim.Core.Abstractions;

/// <summary>
/// Sorguları asenkron çalıştırır. Bll katmanı EF Core'a bağımlı olmadan asenkron sorgu yapabilsin diye vardır (Dependency Inversion).
/// </summary>
public interface IQueryExecutor
{
    Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default);

    Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct = default);

    Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct = default);

    Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default);
}
