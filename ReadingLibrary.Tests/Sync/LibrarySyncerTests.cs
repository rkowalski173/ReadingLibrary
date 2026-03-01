using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using ReadingLibrary.Clients.FreeReadingApi;
using ReadingLibrary.Sync;
using ReadingLibrary.Tests.Infrastructure;

namespace ReadingLibrary.Tests.Sync;

[Collection(ApiCollection.Name)]
[IntegrationTest]
public class LibrarySyncerTests : IAsyncLifetime
{
    private readonly ApiFactory     _factory;
    private readonly FakeReadingApi _api = new();

    public LibrarySyncerTests(ApiFactory factory) => _factory = factory;

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync()    => _factory.ResetAsync();

    private LibrarySyncer CreateSut(ReadingLibraryDbContext db) =>
        new(db, _api, TimeProvider.System, NullLogger<LibrarySyncer>.Instance);

    [Fact]
    public async Task SyncAsync_NewAuthors_AreAddedToDb()
    {
        _api.Authors = [new("Adam Mickiewicz",  "mickiewicz"),
                        new("Juliusz Słowacki", "slowacki")];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var authors = await db.Authors.OrderBy(a => a.Id).ToListAsync();
        authors.Should().HaveCount(2);
        authors.Should().ContainSingle(a => a.Id == "mickiewicz" && a.Name == "Adam Mickiewicz");
        authors.Should().ContainSingle(a => a.Id == "slowacki"   && a.Name == "Juliusz Słowacki");
    }

    [Fact]
    public async Task SyncAsync_ExistingAuthors_AreNotDuplicated()
    {
        await _factory.SeedAsync(db =>
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz")));

        _api.Authors = [new("Adam Mickiewicz", "mickiewicz"),
                        new("Juliusz Słowacki", "slowacki")];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        (await db.Authors.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task SyncAsync_NewBook_IsAddedToDb()
    {
        await _factory.SeedAsync(db =>
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz")));

        _api.Authors = [];
        _api.Books   = [ApiBook("pan-tadeusz", "Pan Tadeusz", "Adam Mickiewicz")];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var book = await db.Books.Include(b => b.Authors).SingleAsync();
        book.Id.Should().Be("pan-tadeusz");
        book.Title.Should().Be("Pan Tadeusz");
        book.Authors.Should().ContainSingle(a => a.Id == "mickiewicz");
    }

    [Fact]
    public async Task SyncAsync_BookWithMultipleAuthors_AllAuthorsAreLinked()
    {
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("slowacki",   "Juliusz Słowacki"));
        });

        _api.Authors = [];
        _api.Books   = [ApiBook("wspolna", "Wspólna książka", "Adam Mickiewicz, Juliusz Słowacki")];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var book = await db.Books.Include(b => b.Authors).SingleAsync();
        book.Authors.Should().HaveCount(2);
        book.Authors.Should().ContainSingle(a => a.Id == "mickiewicz");
        book.Authors.Should().ContainSingle(a => a.Id == "slowacki");
    }

    [Fact]
    public async Task SyncAsync_BookWithUnknownAuthor_IsAddedWithoutThatAuthor()
    {
        _api.Authors = [];
        _api.Books   = [ApiBook("jakas-ksiazka", "Jakaś książka", "Jan Nieznany")];
        _api.BookDetails["jakas-ksiazka"] = new IFreeReadingApi.BookDetail("jakas-ksiazka",
        [
            new IFreeReadingApi.BookDetailAuthor("jan-nieznany", "Jan Nieznany")
        ]);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var book = await db.Books.Include(b => b.Authors).SingleAsync();
        book.Authors.Should().BeEmpty();
    }

    [Fact]
    public async Task SyncAsync_AmbiguousAuthor_ResolvesViaBookDetail()
    {
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("mickiewicz-a", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("mickiewicz-b", "Adam Mickiewicz"));
        });

        _api.Authors = [];
        _api.Books   = [ApiBook("oda", "Oda do młodości", "Adam Mickiewicz")];
        _api.BookDetails["oda"] = new IFreeReadingApi.BookDetail("oda",
        [
            new IFreeReadingApi.BookDetailAuthor("mickiewicz-a", "Adam Mickiewicz")
        ]);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var book = await db.Books.Include(b => b.Authors).SingleAsync();
        book.Authors.Should().ContainSingle(a => a.Id == "mickiewicz-a");
    }

    [Fact]
    public async Task SyncAsync_ExistingBook_IsNotDuplicated()
    {
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz"));
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz"));
        });

        _api.Authors = [];
        _api.Books   = [ApiBook("pan-tadeusz", "Pan Tadeusz", "Adam Mickiewicz")];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        (await db.Books.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task SyncAsync_BookWhoseDetailFetchFails_IsSkipped_OtherBooksAreSynced()
    {
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("mickiewicz-a", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("mickiewicz-b", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("slowacki",     "Juliusz Słowacki"));
        });

        _api.Authors      = [];
        _api.FailingSlugs = ["problematyczna"];
        _api.Books        =
        [
            ApiBook("problematyczna", "Problematyczna", "Adam Mickiewicz"),
            ApiBook("dobra",          "Dobra książka",  "Juliusz Słowacki"),
        ];

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var bookIds = await db.Books.Select(b => b.Id).ToListAsync();
        bookIds.Should().ContainSingle().Which.Should().Be("dobra");
    }

    [Fact]
    public async Task SyncAsync_BookWithPartiallyUnresolvableAuthors_IsAddedWithResolvedAuthorsOnly()
    {
        await _factory.SeedAsync(db =>
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz")));

        _api.Authors = [];
        _api.Books   = [ApiBook("wspolna", "Wspólna książka", "Adam Mickiewicz, Jan Nieznany")];
        _api.BookDetails["wspolna"] = new IFreeReadingApi.BookDetail("wspolna",
        [
            new IFreeReadingApi.BookDetailAuthor("mickiewicz",  "Adam Mickiewicz"),
            new IFreeReadingApi.BookDetailAuthor("jan-nieznany", "Jan Nieznany"),
        ]);

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
        await CreateSut(db).SyncAsync(CancellationToken.None);

        var book = await db.Books.Include(b => b.Authors).SingleAsync();
        book.Authors.Should().ContainSingle(a => a.Id == "mickiewicz");
    }

    private static IFreeReadingApi.Book ApiBook(string slug, string title, string author) =>
        new("liryka", title, author, "romantyzm", "wiersz", "thumb.jpg", slug, $"https://example.com/{slug}");
}
