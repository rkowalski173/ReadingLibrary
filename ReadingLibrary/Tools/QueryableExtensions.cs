using Microsoft.EntityFrameworkCore;

namespace ReadingLibrary.Tools;

internal static class QueryableExtensions
{
    internal static async Task<(IReadOnlyList<T> Items, int TotalCount)> PaginateAsync<T>(
        this IQueryable<T> q, PageOptions pageOptions, CancellationToken ct)
    {
        var items = await q
            .Skip((pageOptions.Page - 1) * pageOptions.PageSize)
            .Take(pageOptions.PageSize)
            .ToListAsync(ct);

        var totalCount = (items.Count < pageOptions.PageSize && items.Count > 0)
            ? (pageOptions.Page - 1) * pageOptions.PageSize + items.Count
            : await q.CountAsync(ct);

        return (items, totalCount);
    }
}
