namespace ReadingLibrary.Tests.Infrastructure;

public record PagedResult<T>(List<T> Items, int Page, int PageSize, int TotalCount);

public record BookResult(
    string Id,
    string Title,
    string Kind,
    string Genre,
    string Epoch,
    string Url,
    string ThumbnailUrl,
    List<AuthorSummaryResult> Authors);

public record AuthorSummaryResult(string Id, string Name);

public record AuthorResult(string Id, string Name);
