using Microsoft.AspNetCore.Mvc;
using ReadingLibrary.API.Models;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;
using ReadingLibrary.Contracts;
using ReadingLibrary.Tools;

namespace ReadingLibrary.API.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthorsController(AuthorsPresenter authors, BookPresenter books) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<AuthorDto>>> GetAuthors([FromQuery] GetAuthorsRequest req, CancellationToken ct)
    {
        var query = new GetAuthorsQuery(
            new SortOptions(req.SortBy ?? AuthorsPresenter.SortBy.Name, req.SortOrder != "desc"),
            new PageOptions(req.Page, req.PageSize));
        var (items, total) = await authors.GetAuthorsAsync(query, ct);
        return Ok(new PaginatedResponse<AuthorDto>(items, query.Paging.Page, query.Paging.PageSize, total));
    }

    [HttpGet("{authorId}/books")]
    public async Task<ActionResult<PaginatedResponse<BookDto>>> GetBooksByAuthor(string authorId, [FromQuery] GetBooksByAuthorRequest req, CancellationToken ct)
    {
        var query = new GetBooksByAuthorQuery(authorId, new PageOptions(req.Page, req.PageSize));
        var (items, total) = await books.GetBooksByAuthorAsync(query, ct);
        return Ok(new PaginatedResponse<BookDto>(items, query.Paging.Page, query.Paging.PageSize, total));
    }
}
