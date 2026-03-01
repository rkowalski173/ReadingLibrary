using Microsoft.EntityFrameworkCore;
using ReadingLibrary.Contracts;
using ReadingLibrary.Tools;

namespace ReadingLibrary.Books;

public record GetBooksQuery(string? Kind, string? Genre, string? Epoch, SortOptions Sorting, PageOptions Paging);
public record GetBooksByAuthorQuery(string AuthorId, PageOptions Paging);

public class BookPresenter(ReadingLibraryDbContext db)
{
    public static class SortBy
    {
        public const string Title = "title";
        public const string AuthorName = "authorName";

        public static readonly string[] All = [Title, AuthorName];
    }

    public async Task<(IReadOnlyList<BookDto> Items, int TotalCount)> GetBooksAsync(GetBooksQuery query, CancellationToken ct = default)
    {
        var q = ApplySorting(ApplyFilters(db.Books.AsQueryable(), query), query.Sorting);
        return await ProjectToDto(q).PaginateAsync(query.Paging, ct);
    }

    public async Task<BookDto?> GetBookByIdAsync(string id, CancellationToken ct = default)
    {
        return await ProjectToDto(db.Books.Where(b => b.Id == id))
            .SingleOrDefaultAsync(ct);
    }

    public async Task<(IReadOnlyList<BookDto> Items, int TotalCount)> GetBooksByAuthorAsync(GetBooksByAuthorQuery query, CancellationToken ct = default)
    {
        var q = db.Books
            .Where(b => b.Authors.Any(a => a.Id == query.AuthorId))
            .OrderBy(b => b.Title).ThenBy(b => b.Id);
        return await ProjectToDto(q).PaginateAsync(query.Paging, ct);
    }

    private static IQueryable<Book> ApplyFilters(IQueryable<Book> q, GetBooksQuery query)
    {
        if (query.Kind is not null)  q = q.Where(b => b.Kind == query.Kind.ToLower());
        if (query.Genre is not null) q = q.Where(b => b.Genre == query.Genre.ToLower());
        if (query.Epoch is not null) q = q.Where(b => b.Epoch == query.Epoch.ToLower());
        return q;
    }

    private static IQueryable<Book> ApplySorting(IQueryable<Book> q, SortOptions sorting) =>
        sorting switch
        {
            { SortBy: SortBy.AuthorName, InAscendingOrder: true  } => q.OrderBy(b => b.Authors.Select(a => a.Name).Min()).ThenBy(b => b.Id),
            { SortBy: SortBy.AuthorName, InAscendingOrder: false } => q.OrderByDescending(b => b.Authors.Select(a => a.Name).Max()).ThenByDescending(b => b.Id),
            { SortBy: SortBy.Title,      InAscendingOrder: false } => q.OrderByDescending(b => b.Title).ThenByDescending(b => b.Id),
            { SortBy: SortBy.Title,      InAscendingOrder: true  } => q.OrderBy(b => b.Title).ThenBy(b => b.Id),
            _ => throw new ArgumentOutOfRangeException(nameof(SortOptions.SortBy), sorting.SortBy, null)
        };

    private static IQueryable<BookDto> ProjectToDto(IQueryable<Book> q) =>
        q.Select(b => new BookDto(
            b.Id, b.Title, b.Kind, b.Genre, b.Epoch, b.Url, b.ThumbnailUrl,
            b.Authors.Select(a => new AuthorSummaryDto(a.Id, a.Name)).ToArray()
        ));
}
