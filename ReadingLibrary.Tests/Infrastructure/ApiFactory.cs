using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ReadingLibrary.API.Sync;
using Testcontainers.PostgreSql;

namespace ReadingLibrary.Tests.Infrastructure;

public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:17-alpine")
        .Build();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            var syncWorker = services.SingleOrDefault(d => d.ImplementationType == typeof(SyncWorker));
            if (syncWorker is not null) services.Remove(syncWorker);

            services.RemoveAll<DbContextOptions<ReadingLibraryDbContext>>();
            services.AddDbContext<ReadingLibraryDbContext>(o =>
                o.UseNpgsql(_postgres.GetConnectionString(),
                    b => b.MigrationsAssembly("ReadingLibrary")));
        });
    }

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        using var scope = Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>()
            .Database.MigrateAsync();
    }

    public Task SeedAsync(Action<ReadingLibraryDbContext> seed) =>
        SeedAsync(db => { seed(db); return Task.CompletedTask; });

    public async Task SeedAsync(Func<ReadingLibraryDbContext, Task> seed)
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await seed(db);
        await db.SaveChangesAsync();
    }

    public async Task ResetAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            """TRUNCATE TABLE "BookAuthor", "Books", "Authors" RESTART IDENTITY""");
    }

    async Task IAsyncLifetime.DisposeAsync() => await _postgres.DisposeAsync();
}
