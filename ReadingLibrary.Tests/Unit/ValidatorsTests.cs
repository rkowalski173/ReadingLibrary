using FluentAssertions;
using ReadingLibrary.API.Models;

namespace ReadingLibrary.Tests.Unit;

public class GetAuthorsRequestValidatorTests
{
    private readonly GetAuthorsRequestValidator _sut = new();

    [Fact]
    public void DefaultRequest_IsValid()
    {
        _sut.Validate(new GetAuthorsRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("name")]
    public void ValidSortBy_IsValid(string sortBy)
    {
        _sut.Validate(new GetAuthorsRequest { SortBy = sortBy }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("title")]
    [InlineData("NAME")]
    public void InvalidSortBy_IsInvalid(string sortBy)
    {
        var result = _sut.Validate(new GetAuthorsRequest { SortBy = sortBy });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void ValidSortOrder_IsValid(string sortOrder)
    {
        _sut.Validate(new GetAuthorsRequest { SortOrder = sortOrder }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("ASC")]
    public void InvalidSortOrder_IsInvalid(string sortOrder)
    {
        var result = _sut.Validate(new GetAuthorsRequest { SortOrder = sortOrder });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.SortOrder));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        var result = _sut.Validate(new GetAuthorsRequest { Page = page });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(new GetAuthorsRequest { PageSize = pageSize });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.PageSize));
    }
}

public class GetBooksRequestValidatorTests
{
    private readonly GetBooksRequestValidator _sut = new();

    [Fact]
    public void DefaultRequest_IsValid()
    {
        _sut.Validate(new GetBooksRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("title")]
    [InlineData("authorName")]
    public void ValidSortBy_IsValid(string sortBy)
    {
        _sut.Validate(new GetBooksRequest { SortBy = sortBy }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("name")]
    [InlineData("TITLE")]
    public void InvalidSortBy_IsInvalid(string sortBy)
    {
        var result = _sut.Validate(new GetBooksRequest { SortBy = sortBy });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void ValidSortOrder_IsValid(string sortOrder)
    {
        _sut.Validate(new GetBooksRequest { SortOrder = sortOrder }).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("DESC")]
    public void InvalidSortOrder_IsInvalid(string sortOrder)
    {
        var result = _sut.Validate(new GetBooksRequest { SortOrder = sortOrder });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.SortOrder));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        var result = _sut.Validate(new GetBooksRequest { Page = page });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(new GetBooksRequest { PageSize = pageSize });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.PageSize));
    }
}

public class GetBooksByAuthorRequestValidatorTests
{
    private readonly GetBooksByAuthorRequestValidator _sut = new();

    [Fact]
    public void DefaultRequest_IsValid()
    {
        _sut.Validate(new GetBooksByAuthorRequest()).IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        var result = _sut.Validate(new GetBooksByAuthorRequest { Page = page });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksByAuthorRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        var result = _sut.Validate(new GetBooksByAuthorRequest { PageSize = pageSize });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksByAuthorRequest.PageSize));
    }
}
