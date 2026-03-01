using Microsoft.EntityFrameworkCore;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;
using ReadingLibrary.Clients.FreeReadingApi;

namespace ReadingLibrary.Sync;

public class LibrarySyncer
{
    private readonly ReadingLibraryDbContext _db;
    private readonly IFreeReadingApi _api;
    private readonly TimeProvider _timeProvider;


    public LibrarySyncer(ReadingLibraryDbContext db, IFreeReadingApi api, TimeProvider timeProvider)
    {
        _db = db;
        _api = api;
        _timeProvider = timeProvider;
    }

    public async Task SyncAsync(CancellationToken ct)
    {
        var booksTask = _api.GetBooks();
        var authorsTask = _api.GetAuthors();
        await Task.WhenAll(booksTask, authorsTask);

        var apiBooks = await booksTask;
        var apiAuthors = await authorsTask;

        var existingAuthors = await _db.Authors.Select(x => x.Id).ToHashSetAsync(ct);
        var existingBooks = await _db.Books.Select(x => x.Id).ToHashSetAsync(ct);
        
        var newAuthors = apiAuthors.Where(x => !existingAuthors.Contains(x.Slug)).ToList();
        foreach (var newApiAuthor in newAuthors)
        {
            var author = new Author { Id = newApiAuthor.Slug, Name = newApiAuthor.Name.Trim(), CreatedAt = _timeProvider.GetUtcNow()};
            _db.Authors.Add(author);
        }
        await _db.SaveChangesAsync(ct);
        
        
        var newBooks = apiBooks.Where(x => !existingBooks.Contains(x.Slug)).ToList();
        var newBookAuthorsNames = newBooks
            .SelectMany(x => x.Author.Split(","))
            .Select(x => x.Trim())
            .Distinct()
            .ToList();

        var authors = await _db.Authors
            .Where(x => newBookAuthorsNames.Contains(x.Name))
            .ToDictionaryAsync(x => x.Name, ct);
        
        foreach (var newApiBook in newBooks)
        {
            var book = new Book
            {
                Id = newApiBook.Slug,
                Title = newApiBook.Title,
                Url = newApiBook.Url,
                ThumbnailUrl = newApiBook.SimpleThumb,
                Kind = newApiBook.Kind.ToLowerInvariant(),
                Epoch = newApiBook.Epoch.ToLowerInvariant(),
                Genre = newApiBook.Genre.ToLowerInvariant(),
                Authors = authors
                    .Where(x => 
                        newApiBook.Author.Split(",").Select(xx => xx.Trim()).Contains(x.Key)
                        )
                    .Select(x => x.Value)
                    .ToList(),
                CreatedAt = _timeProvider.GetUtcNow()
            };
            _db.Books.Add(book);        
        }
        
        await _db.SaveChangesAsync(ct);
    }
}
