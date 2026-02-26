namespace ReadingLibrary.Books;

public class BookService
{
    
}

public record GetBooksQuery(string? Kind, string? Genre, string? Epoch, SortOptions? Sorting);

public record SortOptions(string SortBy, bool InAscendingOrder);