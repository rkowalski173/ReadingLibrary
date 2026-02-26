using ReadingLibrary.Books;

namespace ReadingLibrary.Authors;

public class Author
{
    public required string Id { get; init; }
    public required string Name { get; set; }

    public ICollection<Book> Books { get; init; } = [];
}
