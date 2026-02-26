using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
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

public static class Registration
{
    public static void AddFreeReadingApi(this IServiceCollection services)
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })
        };

        services.AddRefitClient<IFreeReadingApi>(settings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri("https://wolnelektury.pl/api"));
    }
}
