using Refit;

namespace ReadingLibrary.Clients.FreeReadingApi;

public interface IFreeReadingApi
{
    [Get("/books")]
    Task<Book[]> GetBooks();
    
    [Get("/authors")]
    Task<Author[]> GetAuthors();
    
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
}