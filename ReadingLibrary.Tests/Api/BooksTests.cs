using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ReadingLibrary.Tests.Infrastructure;

namespace ReadingLibrary.Tests.Api;

[Collection(ApiCollection.Name)]
public class BooksTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public BooksTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetAsync();

    [Fact]
    public async Task GetBooks_ReturnsAllBooks()
    {
        await _factory.SeedAsync(db =>
        {
            var author = Fake.Author("mickiewicz", "Adam Mickiewicz");
            db.Authors.Add(author);
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz", authors: [author]));
            db.Books.Add(Fake.Book("dziady", "Dziady", authors: [author]));
        });

        var response = await _client.GetAsync("/books");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookResult>>(JsonOptions);
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetBooks_FilterByKind_ReturnsMatchingOnly()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-1", "Book 1", kind: "epopeja"));
            db.Books.Add(Fake.Book("book-2", "Book 2", kind: "epopeja"));
            db.Books.Add(Fake.Book("book-3", "Book 3", kind: "liryka"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>("/books?kind=epopeja", JsonOptions);

        result!.Items.Should().HaveCount(2).And.OnlyContain(b => b.Kind == "epopeja");
    }

    [Fact]
    public async Task GetBooks_FilterByGenre_ReturnsMatchingOnly()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-1", "Book 1", genre: "poemat"));
            db.Books.Add(Fake.Book("book-2", "Book 2", genre: "wiersz"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>("/books?genre=poemat", JsonOptions);

        result!.Items.Should().HaveCount(1).And.OnlyContain(b => b.Genre == "poemat");
    }

    [Fact]
    public async Task GetBooks_FilterByEpoch_ReturnsMatchingOnly()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-1", "Book 1", epoch: "romantyzm"));
            db.Books.Add(Fake.Book("book-2", "Book 2", epoch: "pozytywizm"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>("/books?epoch=romantyzm", JsonOptions);

        result!.Items.Should().HaveCount(1).And.OnlyContain(b => b.Epoch == "romantyzm");
    }

    [Fact]
    public async Task GetBooks_SortByTitleAsc_ReturnsBooksInOrder()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("zorro", "Zorro"));
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz"));
            db.Books.Add(Fake.Book("balladyna", "Balladyna"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=title&sortOrder=asc", JsonOptions);

        result!.Items.Select(b => b.Title).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetBooks_SortByTitleDesc_ReturnsBooksInOrder()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("zorro", "Zorro"));
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz"));
            db.Books.Add(Fake.Book("balladyna", "Balladyna"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=title&sortOrder=desc", JsonOptions);

        result!.Items.Select(b => b.Title).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetBooks_SortByAuthorNameAsc_ReturnsBooksInOrder()
    {
        await _factory.SeedAsync(db =>
        {
            var zeromski = Fake.Author("zeromski", "Stefan Żeromski");
            var mickiewicz = Fake.Author("mickiewicz", "Adam Mickiewicz");
            var norwid = Fake.Author("norwid", "Cyprian Kamil Norwid");
            db.Authors.AddRange(zeromski, mickiewicz, norwid);
            db.Books.Add(Fake.Book("rozdziobi", "Rozdziobią nas kruki", authors: [zeromski]));
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz", authors: [mickiewicz]));
            db.Books.Add(Fake.Book("fortepian", "Fortepian Szopena", authors: [norwid]));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=authorName&sortOrder=asc", JsonOptions);

        result!.Items.Select(b => b.Authors.First().Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetBooks_SortByAuthorNameDesc_ReturnsBooksInOrder()
    {
        await _factory.SeedAsync(db =>
        {
            var zeromski = Fake.Author("zeromski", "Stefan Żeromski");
            var mickiewicz = Fake.Author("mickiewicz", "Adam Mickiewicz");
            var norwid = Fake.Author("norwid", "Cyprian Kamil Norwid");
            db.Authors.AddRange(zeromski, mickiewicz, norwid);
            db.Books.Add(Fake.Book("rozdziobi", "Rozdziobią nas kruki", authors: [zeromski]));
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz", authors: [mickiewicz]));
            db.Books.Add(Fake.Book("fortepian", "Fortepian Szopena", authors: [norwid]));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=authorName&sortOrder=desc", JsonOptions);

        result!.Items.Select(b => b.Authors.First().Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetBooks_Pagination_ReturnsCorrectPage()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-a", "Book A"));
            db.Books.Add(Fake.Book("book-b", "Book B"));
            db.Books.Add(Fake.Book("book-c", "Book C"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=title&sortOrder=asc&page=1&pageSize=2", JsonOptions);

        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetBooks_SecondPage_ReturnsRemainingItems()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-a", "Book A"));
            db.Books.Add(Fake.Book("book-b", "Book B"));
            db.Books.Add(Fake.Book("book-c", "Book C"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>(
            "/books?sortBy=title&sortOrder=asc&page=2&pageSize=2", JsonOptions);

        result!.Items.Should().HaveCount(1);
        result.Items.Single().Title.Should().Be("Book C");
    }

    [Fact]
    public async Task GetBooks_FilterByKind_IsCaseInsensitive()
    {
        await _factory.SeedAsync(db =>
        {
            db.Books.Add(Fake.Book("book-1", "Book 1", kind: "epopeja"));
            db.Books.Add(Fake.Book("book-2", "Book 2", kind: "liryka"));
        });

        var result = await _client.GetFromJsonAsync<PagedResult<BookResult>>("/books?kind=EPOPEJA", JsonOptions);

        result!.Items.Should().ContainSingle(b => b.Kind == "epopeja");
    }

    [Fact]
    public async Task GetBooks_InvalidSortBy_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/books?sortBy=invalid");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBooks_PageSizeTooLarge_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/books?pageSize=101");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBooks_PageZero_ReturnsBadRequest()
    {
        var response = await _client.GetAsync("/books?page=0");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetBook_ExistingId_ReturnsBook()
    {
        await _factory.SeedAsync(db =>
        {
            var author = Fake.Author("mickiewicz", "Adam Mickiewicz");
            db.Authors.Add(author);
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz", authors: [author]));
        });

        var response = await _client.GetAsync("/books/pan-tadeusz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var book = await response.Content.ReadFromJsonAsync<BookResult>(JsonOptions);
        book!.Id.Should().Be("pan-tadeusz");
        book.Title.Should().Be("Pan Tadeusz");
        book.Authors.Should().ContainSingle(a => a.Name == "Adam Mickiewicz");
    }

    [Fact]
    public async Task GetBook_NonExistentId_ReturnsNotFound()
    {
        var response = await _client.GetAsync("/books/nonexistent-slug");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
