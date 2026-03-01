using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Refit;

namespace ReadingLibrary.Clients.FreeReadingApi;

public static class Registration
{
    public static IServiceCollection AddFreeReadingApi(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new RefitSettings
        {
            ContentSerializer = new SystemTextJsonContentSerializer(new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
            })
        };

        var url = configuration.GetValue<string>("WolneLekturyApi:Url");

        services.AddRefitClient<IFreeReadingApi>(settings)
            .ConfigureHttpClient(c => c.BaseAddress = new Uri(url));

        return services;
    }
}