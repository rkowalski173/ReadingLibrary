namespace ReadingLibrary.API.Models;

public record PaginatedResponse<T>(IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount);
