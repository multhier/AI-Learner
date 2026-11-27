using Kivilcim.Core.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace Kivilcim.Dal.Repositories;

/// <summary>Asenkron sorgu çalıştırıcısının EF Core gerçeklemesi.</summary>
public sealed class EfQueryExecutor : IQueryExecutor
{
    public Task<List<T>> ToListAsync<T>(IQueryable<T> query, CancellationToken ct = default) =>
        query.ToListAsync(ct);

    public Task<T?> FirstOrDefaultAsync<T>(IQueryable<T> query, CancellationToken ct = default) =>
        query.FirstOrDefaultAsync(ct);

    public Task<bool> AnyAsync<T>(IQueryable<T> query, CancellationToken ct = default) =>
        query.AnyAsync(ct);

    public Task<int> CountAsync<T>(IQueryable<T> query, CancellationToken ct = default) =>
        query.CountAsync(ct);
}
