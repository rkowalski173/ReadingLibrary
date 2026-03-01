using FluentAssertions;
using ReadingLibrary.API.Models;

namespace ReadingLibrary.Tests.Unit;

public class GetAuthorsRequestValidatorTests
{
    private readonly GetAuthorsRequestValidator _sut = new();

    [Fact]
    public void DefaultRequest_IsValid()
    {
        // Arrange
        var request = new GetAuthorsRequest();

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("name")]
    public void ValidSortBy_IsValid(string sortBy)
    {
        // Arrange
        var request = new GetAuthorsRequest { SortBy = sortBy };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("title")]
    [InlineData("NAME")]
    public void InvalidSortBy_IsInvalid(string sortBy)
    {
        // Arrange
        var request = new GetAuthorsRequest { SortBy = sortBy };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void ValidSortOrder_IsValid(string sortOrder)
    {
        // Arrange
        var request = new GetAuthorsRequest { SortOrder = sortOrder };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("ASC")]
    public void InvalidSortOrder_IsInvalid(string sortOrder)
    {
        // Arrange
        var request = new GetAuthorsRequest { SortOrder = sortOrder };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.SortOrder));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        // Arrange
        var request = new GetAuthorsRequest { Page = page };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetAuthorsRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        // Arrange
        var request = new GetAuthorsRequest { PageSize = pageSize };

        // Act
        var result = _sut.Validate(request);

        // Assert
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
        // Arrange
        var request = new GetBooksRequest();

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("title")]
    [InlineData("authorName")]
    public void ValidSortBy_IsValid(string sortBy)
    {
        // Arrange
        var request = new GetBooksRequest { SortBy = sortBy };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("invalid")]
    [InlineData("name")]
    [InlineData("TITLE")]
    public void InvalidSortBy_IsInvalid(string sortBy)
    {
        // Arrange
        var request = new GetBooksRequest { SortBy = sortBy };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.SortBy));
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public void ValidSortOrder_IsValid(string sortOrder)
    {
        // Arrange
        var request = new GetBooksRequest { SortOrder = sortOrder };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("ascending")]
    [InlineData("DESC")]
    public void InvalidSortOrder_IsInvalid(string sortOrder)
    {
        // Arrange
        var request = new GetBooksRequest { SortOrder = sortOrder };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.SortOrder));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        // Arrange
        var request = new GetBooksRequest { Page = page };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        // Arrange
        var request = new GetBooksRequest { PageSize = pageSize };

        // Act
        var result = _sut.Validate(request);

        // Assert
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
        // Arrange
        var request = new GetBooksByAuthorRequest();

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void InvalidPage_IsInvalid(int page)
    {
        // Arrange
        var request = new GetBooksByAuthorRequest { Page = page };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksByAuthorRequest.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void InvalidPageSize_IsInvalid(int pageSize)
    {
        // Arrange
        var request = new GetBooksByAuthorRequest { PageSize = pageSize };

        // Act
        var result = _sut.Validate(request);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == nameof(GetBooksByAuthorRequest.PageSize));
    }
}
