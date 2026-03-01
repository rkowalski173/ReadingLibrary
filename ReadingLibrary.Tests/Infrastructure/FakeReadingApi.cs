using ReadingLibrary.Clients.FreeReadingApi;

namespace ReadingLibrary.Tests.Infrastructure;

public class FakeReadingApi : IFreeReadingApi
{
    public IFreeReadingApi.Author[] Authors { get; set; } = [];
    public IFreeReadingApi.Book[]   Books   { get; set; } = [];

    public Dictionary<string, IFreeReadingApi.BookDetail> BookDetails  { get; set; } = new();
    public HashSet<string>                                FailingSlugs { get; set; } = [];

    public Task<IFreeReadingApi.Author[]> GetAuthors(CancellationToken ct) =>
        Task.FromResult(Authors);

    public Task<IFreeReadingApi.Book[]> GetBooks(CancellationToken ct) =>
        Task.FromResult(Books);

    public Task<IFreeReadingApi.BookDetail> GetBookDetail(string slug, CancellationToken ct)
    {
        if (FailingSlugs.Contains(slug))
            throw new HttpRequestException($"Simulated fetch failure for book '{slug}'");

        return Task.FromResult(BookDetails[slug]);
    }
}
