using Microsoft.AspNetCore.Mvc;
using ReadingLibrary.API.Models;
using ReadingLibrary.Books;
using ReadingLibrary.Contracts;
using ReadingLibrary.Tools;

namespace ReadingLibrary.API.Controllers;

[ApiController]
[Route("[controller]")]
public class BooksController(BookPresenter books) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<BookDto>>> GetBooks([FromQuery] GetBooksRequest req, CancellationToken ct)
    {
        var query = new GetBooksQuery(req.Kind, req.Genre, req.Epoch,
            new SortOptions(req.SortBy ?? BookPresenter.SortBy.Title, req.SortOrder != "desc"),
            new PageOptions(req.Page, req.PageSize));
        var (items, total) = await books.GetBooksAsync(query, ct);
        return Ok(new PaginatedResponse<BookDto>(items, query.Paging.Page, query.Paging.PageSize, total));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<BookDto>> GetBook(string id, CancellationToken ct)
    {
        var book = await books.GetBookByIdAsync(id, ct);
        return book is null ? NotFound() : Ok(book);
    }
}
