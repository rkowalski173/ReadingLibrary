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
        var booksTask   = _api.GetBooks(ct);
        var authorsTask = _api.GetAuthors(ct);
        await Task.WhenAll(booksTask, authorsTask);

        var apiBooks   = booksTask.Result;
        var apiAuthors = authorsTask.Result;

        var existingAuthors = await _db.Authors.Select(x => x.Id).ToHashSetAsync(ct);
        var existingBooks   = await _db.Books.Select(x => x.Id).ToHashSetAsync(ct);

        var newAuthors = apiAuthors
            .Where(x => !existingAuthors.Contains(x.Slug))
            .DistinctBy(x => x.Slug)
            .ToList();
        foreach (var newApiAuthor in newAuthors)
        {
            var author = new Author { Id = newApiAuthor.Slug, Name = newApiAuthor.Name.Trim(), CreatedAt = _timeProvider.GetUtcNow() };
            _db.Authors.Add(author);
        }
        await _db.SaveChangesAsync(ct);


        var newBooks = apiBooks.Where(x => !existingBooks.Contains(x.Slug))
            .DistinctBy(x => x.Slug)
            .ToList();
        
        var newBookAuthorsNames = newBooks
            .SelectMany(x => x.Author.Split(","))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var authors = await _db.Authors
            .Where(x => newBookAuthorsNames.Contains(x.Name))
            .GroupBy(x => x.Name)
            .ToDictionaryAsync(x => x.Key, x => x.ToList(), ct);
        
        foreach (var newApiBook in newBooks)
        {
            var authorsOfThisBook = newApiBook.Author
                .Split(",")
                .Select(xx => xx.Trim())
                .ToList();

            var authorsToAttach = new List<Author>();
            foreach (var authorName in authorsOfThisBook)
            {
                var hasAmbiguousName = authors[authorName].Count > 1;
                if (hasAmbiguousName is false)
                {
                    authorsToAttach.Add(authors[authorName][0]);
                }
                else
                {
                    var bookDetails = await _api.GetBookDetail(newApiBook.Slug, ct);
                    var authorSlug = bookDetails.Authors.First(x => x.Name == authorName).Slug;
                    authorsToAttach.Add(authors[authorName].First(x => x.Id == authorSlug));
                }
            }
            
            var book = new Book
            {
                Id = newApiBook.Slug,
                Title = newApiBook.Title,
                Url = newApiBook.Url,
                ThumbnailUrl = newApiBook.SimpleThumb,
                Kind = newApiBook.Kind.ToLowerInvariant(),
                Epoch = newApiBook.Epoch.ToLowerInvariant(),
                Genre = newApiBook.Genre.ToLowerInvariant(),
                Authors = authorsToAttach,
                CreatedAt = _timeProvider.GetUtcNow()
            };
            _db.Books.Add(book);        
        }
        
        await _db.SaveChangesAsync(ct);
    }
}
