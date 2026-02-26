using Microsoft.EntityFrameworkCore;
using Npgsql;
using ReadingLibrary;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;
using ReadingLibrary.Clients.FreeReadingApi;

namespace ReadingLibrary.API.Sync;

public class SyncWorker(
    IServiceScopeFactory scopeFactory,
    IConfiguration configuration,
    ILogger<SyncWorker> logger) : BackgroundService
{
    private readonly TimeSpan _interval =
        TimeSpan.FromMinutes(configuration.GetValue("Sync:IntervalMinutes", 60));

    private const long SyncAdvisoryLockId = 7483920156L;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(_interval);

        while (true)
        {
            try
            {
                logger.LogInformation("Starting books/authors sync");
                await SyncAsync(stoppingToken);
                logger.LogInformation("Sync completed successfully");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Sync failed");
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private async Task SyncAsync(CancellationToken ct)
    {
        if (await IsAnotherSyncInProgress(ct)) 
            return;

        using var scope = scopeFactory.CreateScope();
        var api = scope.ServiceProvider.GetRequiredService<IFreeReadingApi>();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();

        var booksTask = api.GetBooks();
        var authorsTask = api.GetAuthors();
        await Task.WhenAll(booksTask, authorsTask);

        var apiBooks = await booksTask;
        var apiAuthors = await authorsTask;
        
        var existingAuthors = await db.Authors.ToDictionaryAsync(a => a.Id, ct);
        var existingBooks = await db.Books.Include(b => b.Authors).ToDictionaryAsync(b => b.Id, ct);

        var trackedAuthors = SyncAuthors(db, apiAuthors, existingAuthors);
        SyncBooks(db, apiBooks, existingBooks, trackedAuthors);

        await db.SaveChangesAsync(ct);
    }

    private async Task<bool> IsAnotherSyncInProgress(CancellationToken ct)
    {
        var connectionString = configuration.GetConnectionString("Postgres");

        await using var lockConn = new NpgsqlConnection(connectionString);
        await lockConn.OpenAsync(ct);

        await using var lockCmd = new NpgsqlCommand("SELECT pg_try_advisory_lock(@id)", lockConn);
        lockCmd.Parameters.AddWithValue("id", SyncAdvisoryLockId);

        var acquired = (bool)(await lockCmd.ExecuteScalarAsync(ct))!;
        if (!acquired)
        {
            logger.LogInformation("Sync skipped — another instance is already syncing");
            return true;
        }

        return false;
    }

    private static Dictionary<string, Author> SyncAuthors(
        ReadingLibraryDbContext db,
        IFreeReadingApi.Author[] apiAuthors,
        Dictionary<string, Author> existing)
    {
        var tracked = new Dictionary<string, Author>();

        foreach (var apiAuthor in apiAuthors)
        {
            if (existing.TryGetValue(apiAuthor.Slug, out var author))
                author.Name = apiAuthor.Name;
            else
            {
                author = new Author { Id = apiAuthor.Slug, Name = apiAuthor.Name };
                db.Authors.Add(author);
            }

            tracked[apiAuthor.Slug] = author;
        }

        return tracked;
    }

    private static void SyncBooks(
        ReadingLibraryDbContext db,
        IFreeReadingApi.Book[] apiBooks,
        Dictionary<string, Book> existing,
        Dictionary<string, Author> authors)
    {
        foreach (var apiBook in apiBooks)
        {
            if (existing.TryGetValue(apiBook.Slug, out var book))
            {
                book.Title = apiBook.Title;
                book.Url = apiBook.Url;
                book.ThumbnailUrl = apiBook.SimpleThumb;
            }
            else
            {
                book = new Book
                {
                    Id = apiBook.Slug,
                    Title = apiBook.Title,
                    Url = apiBook.Url,
                    ThumbnailUrl = apiBook.SimpleThumb,
                    Kind = apiBook.Kind,
                    Epoch = apiBook.Epoch,
                    Genre = apiBook.Genre,
                };
                db.Books.Add(book);
            }

            SyncBookAuthor(book, apiBook.Author, authors);
        }
    }

    private static void SyncBookAuthor(Book book, string authorSlug, Dictionary<string, Author> authors)
    {
        if (string.IsNullOrEmpty(authorSlug) || !authors.TryGetValue(authorSlug, out var author))
            return;

        if (!book.Authors.Any(a => a.Id == authorSlug))
            book.Authors.Add(author);
    }
}
