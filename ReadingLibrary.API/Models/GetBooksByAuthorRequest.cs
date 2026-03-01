using FluentValidation;

namespace ReadingLibrary.API.Models;

public record GetBooksByAuthorRequest
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class GetBooksByAuthorRequestValidator : AbstractValidator<GetBooksByAuthorRequest>
{
    public GetBooksByAuthorRequestValidator()
    {
        RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
