using FluentValidation;
using ReadingLibrary.Authors;

namespace ReadingLibrary.API.Models;

public record GetAuthorsRequest
{
    public string? SortBy { get; init; }
    public string? SortOrder { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetAuthorsRequestValidator : AbstractValidator<GetAuthorsRequest>
{
    private static readonly string[] ValidSortBy = AuthorPresenter.SortBy.All;
    private static readonly string[] ValidSortOrder = ["asc", "desc"];

    public GetAuthorsRequestValidator()
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
