using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using ReadingLibrary.Tests.Infrastructure;

namespace ReadingLibrary.Tests.Api;

[Collection(ApiCollection.Name)]
[IntegrationTest]
public class AuthorsTests : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    private readonly HttpClient _client;

    private static readonly JsonSerializerOptions JsonOptions = TestJsonOptions.Default;

    public AuthorsTests(ApiFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task InitializeAsync() => Task.CompletedTask;
    public Task DisposeAsync() => _factory.ResetAsync();

    [Fact]
    public async Task GetAuthors_ReturnsAllAuthors()
    {
        // Arrange
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("slowacki", "Juliusz Słowacki"));
        });

        // Act
        var response = await _client.GetAsync("/authors");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AuthorResult>>(JsonOptions);
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(2);
    }

    [Fact]
    public async Task GetAuthors_SortByNameAsc_ReturnsAuthorsInOrder()
    {
        // Arrange
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("zeromski", "Stefan Żeromski"));
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("norwid", "Cyprian Kamil Norwid"));
        });

        // Act
        var result = await _client.GetFromJsonAsync<PagedResult<AuthorResult>>(
            "/authors?sortBy=name&sortOrder=asc", JsonOptions);

        // Assert
        result!.Items.Should().HaveCount(3);
        result!.Items.Select(a => a.Name).Should().BeInAscendingOrder();
    }

    [Fact]
    public async Task GetAuthors_SortByNameDesc_ReturnsAuthorsInOrder()
    {
        // Arrange
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("zeromski", "Stefan Żeromski"));
            db.Authors.Add(Fake.Author("mickiewicz", "Adam Mickiewicz"));
            db.Authors.Add(Fake.Author("norwid", "Cyprian Kamil Norwid"));
        });

        // Act
        var result = await _client.GetFromJsonAsync<PagedResult<AuthorResult>>(
            "/authors?sortBy=name&sortOrder=desc", JsonOptions);

        // Assert
        result!.Items.Should().HaveCount(3);
        result!.Items.Select(a => a.Name).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task GetAuthors_Pagination_ReturnsCorrectPage()
    {
        // Arrange
        await _factory.SeedAsync(db =>
        {
            db.Authors.Add(Fake.Author("author-a", "Author A"));
            db.Authors.Add(Fake.Author("author-b", "Author B"));
            db.Authors.Add(Fake.Author("author-c", "Author C"));
        });

        // Act
        var result = await _client.GetFromJsonAsync<PagedResult<AuthorResult>>(
            "/authors?sortBy=name&sortOrder=asc&page=1&pageSize=2", JsonOptions);

        // Assert
        result!.Items.Should().HaveCount(2);
        result.TotalCount.Should().Be(3);
        result.Page.Should().Be(1);
    }

    [Fact]
    public async Task GetAuthors_InvalidSortBy_ReturnsBadRequest()
    {
        // Act
        var response = await _client.GetAsync("/authors?sortBy=invalid");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetAuthorBooks_NonExistentAuthor_ReturnsNotFound()
    {
        // Act
        var response = await _client.GetAsync("/authors/nonexistent/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetAuthorBooks_ReturnsOnlyBooksForGivenAuthor()
    {
        // Arrange
        await _factory.SeedAsync(db =>
        {
            var mickiewicz = Fake.Author("mickiewicz", "Adam Mickiewicz");
            var slowacki = Fake.Author("slowacki", "Juliusz Słowacki");
            db.Authors.AddRange(mickiewicz, slowacki);
            db.Books.Add(Fake.Book("pan-tadeusz", "Pan Tadeusz", authors: [mickiewicz]));
            db.Books.Add(Fake.Book("dziady", "Dziady", authors: [mickiewicz]));
            db.Books.Add(Fake.Book("balladyna", "Balladyna", authors: [slowacki]));
        });

        // Act
        var response = await _client.GetAsync("/authors/mickiewicz/books");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<BookResult>>(JsonOptions);
        result!.Items.Should().HaveCount(2)
            .And.OnlyContain(b => b.Authors.Any(a => a.Id == "mickiewicz"));
    }
}
