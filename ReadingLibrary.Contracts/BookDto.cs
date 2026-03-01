namespace ReadingLibrary.Contracts;

public record AuthorSummaryDto(string Id, string Name);
public record BookDto(string Id, string Title, string Kind, string Genre, string Epoch, string Url, string ThumbnailUrl, AuthorSummaryDto[] Authors);
