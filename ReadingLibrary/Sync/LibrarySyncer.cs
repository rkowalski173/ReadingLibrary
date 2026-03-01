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
        return (booksTask.Result, authorsTask.Result);
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

        var syncedCount = 0;
        foreach (var apiBook in newBooks)
        {
            try
            {
                var book = await BuildBookAsync(apiBook, authorsByName, ct);
                _db.Books.Add(book);
                syncedCount++;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to sync book '{Slug}', skipping", apiBook.Slug);
            }
        }

        await _db.SaveChangesAsync(ct);
        return syncedCount;
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

    private async Task<Book> BuildBookAsync(IFreeReadingApi.Book apiBook, Dictionary<string, List<Author>> authorsByName, CancellationToken ct)
    {
        var authorNames = apiBook.Author.Split(",").Select(x => x.Trim()).ToList();
        var authors     = await ResolveAuthorsAsync(apiBook.Slug, authorNames, authorsByName, ct);

        return new Book
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

    private async Task<List<Author>> ResolveAuthorsAsync(string bookSlug, List<string> authorNames, Dictionary<string, List<Author>> authorsByName, CancellationToken ct)
    {
        var resolved = new List<Author>();

        foreach (var name in authorNames)
        {
            if (!authorsByName.TryGetValue(name, out var candidates))
            {
                _logger.LogWarning("Book '{Slug}': unknown author '{Name}', skipping", bookSlug, name);
                continue;
            }

            if (candidates.Count == 1)
            {
                resolved.Add(candidates[0]);
                continue;
            }

            _logger.LogDebug("Book '{Slug}': ambiguous author '{Name}', fetching book details", bookSlug, name);
            var detail     = await _api.GetBookDetail(bookSlug, ct);
            var authorSlug = detail.Authors.First(x => x.Name == name).Slug;
            resolved.Add(candidates.First(x => x.Id == authorSlug));
        }

        return resolved;
    }
}
