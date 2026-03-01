using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;
using ReadingLibrary.Clients.FreeReadingApi;

namespace ReadingLibrary.Sync;

public class LibrarySyncer
{
    private readonly ReadingLibraryDbContext _db;
    private readonly IFreeReadingApi _api;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<LibrarySyncer> _logger;

    public LibrarySyncer(ReadingLibraryDbContext db, IFreeReadingApi api, TimeProvider timeProvider, ILogger<LibrarySyncer> logger)
    {
        _db = db;
        _api = api;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task SyncAsync(CancellationToken ct)
    {
        _logger.LogInformation("Sync started");

        var (apiBooks, apiAuthors) = await FetchFromApiAsync(ct);

        var syncedAuthors = await SyncAuthorsAsync(apiAuthors, ct);
        _logger.LogInformation("Authors synced: {Count} new", syncedAuthors);

        var syncedBooks = await SyncBooksAsync(apiBooks, ct);
        _logger.LogInformation("Books synced: {Count} new", syncedBooks);

        _logger.LogInformation("Sync completed");
    }

    private async Task<(IFreeReadingApi.Book[] Books, IFreeReadingApi.Author[] Authors)> FetchFromApiAsync(CancellationToken ct)
    {
        var booksTask   = _api.GetBooks(ct);
        var authorsTask = _api.GetAuthors(ct);
        await Task.WhenAll(booksTask, authorsTask);
        return (await booksTask, await authorsTask);
    }

    private async Task<int> SyncAuthorsAsync(IFreeReadingApi.Author[] apiAuthors, CancellationToken ct)
    {
        var existingIds = await _db.Authors.Select(x => x.Id).ToHashSetAsync(ct);

        var newAuthors = apiAuthors
            .Where(x => !existingIds.Contains(x.Slug))
            .DistinctBy(x => x.Slug)
            .ToList();

        foreach (var apiAuthor in newAuthors)
        {
            _db.Authors.Add(new Author
            {
                Id        = apiAuthor.Slug,
                Name      = apiAuthor.Name.Trim(),
                CreatedAt = _timeProvider.GetUtcNow()
            });
        }

        await _db.SaveChangesAsync(ct);
        return newAuthors.Count;
    }

    private async Task<int> SyncBooksAsync(IFreeReadingApi.Book[] apiBooks, CancellationToken ct)
    {
        var existingIds = await _db.Books.Select(x => x.Id).ToHashSetAsync(ct);

        var newBooks = apiBooks
            .Where(x => !existingIds.Contains(x.Slug))
            .DistinctBy(x => x.Slug)
            .ToList();

        var authorsByName = await LoadAuthorsByNameAsync(newBooks, ct);

        // Phase 1: try to resolve all books by name
        var resolvedBooks = new List<(IFreeReadingApi.Book ApiBook, List<Author> Authors)>();
        var failedBooks   = new List<IFreeReadingApi.Book>();

        foreach (var apiBook in newBooks)
        {
            if (TryResolveAuthorsByName(apiBook, authorsByName, out var authors))
                resolvedBooks.Add((apiBook, authors));
            else
                failedBooks.Add(apiBook);
        }

        // Phase 2: fetch details for failed books in parallel (max 10 concurrent)
        using var semaphore = new SemaphoreSlim(10);
        var detailResults = await Task.WhenAll(
            failedBooks.Select(async b =>
            {
                await semaphore.WaitAsync(ct);
                try
                {
                    var detail = await _api.GetBookDetail(b.Slug, ct);
                    return (Book: b, Detail: (IFreeReadingApi.BookDetail?)detail);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to fetch details for book '{Slug}', skipping", b.Slug);
                    return (Book: b, Detail: (IFreeReadingApi.BookDetail?)null);
                }
                finally
                {
                    semaphore.Release();
                }
            })
        );

        // Phase 3: resolve failed books by author slug
        var allSlugs = detailResults
            .Where(r => r.Detail is not null)
            .SelectMany(r => r.Detail!.Authors)
            .Select(a => a.Slug)
            .Distinct()
            .ToList();

        var authorsBySlug = await LoadAuthorsBySlugAsync(allSlugs, ct);

        foreach (var (apiBook, detail) in detailResults.Where(r => r.Detail is not null))
        {
            var authors = ResolveAuthorsBySlug(apiBook.Slug, detail!.Authors, authorsBySlug);
            resolvedBooks.Add((apiBook, authors));
        }

        // Phase 4: add all resolved books
        foreach (var (apiBook, authors) in resolvedBooks)
            _db.Books.Add(BuildBook(apiBook, authors));

        await _db.SaveChangesAsync(ct);
        return resolvedBooks.Count;
    }

    private async Task<Dictionary<string, List<Author>>> LoadAuthorsByNameAsync(List<IFreeReadingApi.Book> books, CancellationToken ct)
    {
        var authorNames = books
            .SelectMany(x => x.Author.Split(","))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        return await _db.Authors
            .Where(x => authorNames.Contains(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionaryAsync(x => x.Key, x => x.ToList(), ct);
    }

    private async Task<Dictionary<string, Author>> LoadAuthorsBySlugAsync(List<string> slugs, CancellationToken ct)
    {
        if (slugs.Count == 0)
            return [];

        return await _db.Authors
            .Where(a => slugs.Contains(a.Id))
            .ToDictionaryAsync(a => a.Id, ct);
    }

    private bool TryResolveAuthorsByName(IFreeReadingApi.Book apiBook, Dictionary<string, List<Author>> authorsByName, out List<Author> authors)
    {
        var authorNames = apiBook.Author.Split(",").Select(x => x.Trim()).ToList();
        var resolved = new List<Author>();

        foreach (var name in authorNames)
        {
            if (!authorsByName.TryGetValue(name, out var candidates) || candidates.Count != 1)
            {
                authors = [];
                return false;
            }
            resolved.Add(candidates[0]);
        }

        authors = resolved;
        return true;
    }

    private List<Author> ResolveAuthorsBySlug(string bookSlug, IFreeReadingApi.BookDetailAuthor[] detailAuthors, Dictionary<string, Author> authorsBySlug)
    {
        var resolved = new List<Author>();

        foreach (var detailAuthor in detailAuthors)
        {
            if (authorsBySlug.TryGetValue(detailAuthor.Slug, out var author))
                resolved.Add(author);
            else
                _logger.LogWarning("Book '{Slug}': author '{AuthorSlug}' not found by slug, saving without", bookSlug, detailAuthor.Slug);
        }

        return resolved;
    }

    private Book BuildBook(IFreeReadingApi.Book apiBook, List<Author> authors) => new()
    {
        Id           = apiBook.Slug,
        Title        = apiBook.Title,
        Url          = apiBook.Url,
        ThumbnailUrl = apiBook.SimpleThumb,
        Kind         = apiBook.Kind.ToLowerInvariant(),
        Epoch        = apiBook.Epoch.ToLowerInvariant(),
        Genre        = apiBook.Genre.ToLowerInvariant(),
        Authors      = authors,
        CreatedAt    = _timeProvider.GetUtcNow()
    };
}
