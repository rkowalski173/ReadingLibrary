using Refit;

namespace ReadingLibrary.Clients.FreeReadingApi;

public interface IFreeReadingApi
{
    [Get("/books")]
    Task<Book[]> GetBooks(CancellationToken ct = default);

    [Get("/authors")]
    Task<Author[]> GetAuthors(CancellationToken ct = default);

    [Get("/books/{slug}/")]
    Task<BookDetail> GetBookDetail(string slug, CancellationToken ct = default);

    public record Book(
        string Kind,
        string Title,
        string Author,
        string Epoch,
        string Genre,
        string SimpleThumb,
        string Slug,
        string Url
    );
    
    public record Author(
        string Name,
        string Slug
    );

    public record BookDetail(string Slug, BookDetailAuthor[] Authors);
    public record BookDetailAuthor(string Slug, string Name);
}