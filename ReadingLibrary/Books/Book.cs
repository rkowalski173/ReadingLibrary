using ReadingLibrary.Authors;

namespace ReadingLibrary.Books;

public class Book
{
    public required string Id { get; init; }
    public required string Title { get; set; }
    public required string Url { get; set; }
    public required string ThumbnailUrl { get; set; }
    public required string Kind { get; set; }
    public required string Epoch { get; set; }
    public required string Genre { get; set; }

    public ICollection<Author> Authors { get; init; } = [];
}
