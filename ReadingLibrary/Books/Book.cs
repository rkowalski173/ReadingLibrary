using ReadingLibrary.Authors;

namespace ReadingLibrary.Books;

public class Book
{
    public required string Id { get; init; }
    public required string Title { get; init; }
    public required string Url { get; init; }
    public required string ThumbnailUrl { get; init; }
    public required string Kind { get; init; }
    public required string Epoch { get; init; }
    public required string Genre { get; init; }
    
    public required DateTimeOffset CreatedAt { get; init; }

    public ICollection<Author> Authors { get; init; } = [];
}
