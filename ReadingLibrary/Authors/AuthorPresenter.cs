using Microsoft.EntityFrameworkCore;
using ReadingLibrary.Contracts;
using ReadingLibrary.Tools;

namespace ReadingLibrary.Authors;

public record GetAuthorsQuery(SortOptions Sorting, PageOptions Paging);

public class AuthorPresenter(ReadingLibraryDbContext db)
{
    public static class SortBy
    {
        public const string Name = "name";

        public static readonly string[] All = [Name];
    }

    public async Task<bool> AuthorExistsAsync(string authorId, CancellationToken ct = default)
        => await db.Authors.AnyAsync(a => a.Id == authorId, ct);

    public async Task<(IReadOnlyList<AuthorDto> Items, int TotalCount)> GetAuthorsAsync(
        GetAuthorsQuery query, CancellationToken ct = default)
    {
        return await ApplySorting(db.Authors, query.Sorting)
            .Select(a => new AuthorDto(a.Id, a.Name))
            .PaginateAsync(query.Paging, ct);
    }

    private static IQueryable<Author> ApplySorting(IQueryable<Author> q, SortOptions sorting) =>
        sorting switch
        {
            { SortBy: SortBy.Name, InAscendingOrder: false } => q.OrderByDescending(b => b.Name).ThenByDescending(b => b.Id),
            { SortBy: SortBy.Name, InAscendingOrder: true  } => q.OrderBy(b => b.Name).ThenBy(b => b.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(SortOptions.SortBy), sorting.SortBy, null)
        };
}
