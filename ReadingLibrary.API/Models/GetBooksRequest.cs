using FluentValidation;
using ReadingLibrary.Books;

namespace ReadingLibrary.API.Models;

public record GetBooksRequest
{
    public string? Kind { get; init; }
    public string? Genre { get; init; }
    public string? Epoch { get; init; }
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetBooksRequestValidator : AbstractValidator<GetBooksRequest>
{
    private static readonly string[] ValidSortBy = BookPresenter.SortBy.All;
    private static readonly string[] ValidSortOrder = ["asc", "desc"];

    public GetBooksRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
        RuleFor(x => x.SortBy)
            .Must(v => v is null || ValidSortBy.Contains(v))
            .WithMessage($"'SortBy' must be one of: {string.Join(", ", ValidSortBy)}.");
        RuleFor(x => x.SortOrder)
            .Must(v => v is null || ValidSortOrder.Contains(v))
            .WithMessage($"'SortOrder' must be one of: {string.Join(", ", ValidSortOrder)}.");
    }
}
