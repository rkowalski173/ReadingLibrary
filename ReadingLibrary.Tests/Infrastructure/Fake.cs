using ReadingLibrary.Authors;
using ReadingLibrary.Books;

namespace ReadingLibrary.Tests.Infrastructure;

public static class Fake
{
    public static Author Author(string id, string name) => new()
    {
        Id = id,
        Name = name,
        CreatedAt = DateTimeOffset.UtcNow
    };

    public static Book Book(
        string id,
        string title,
        string kind = "liryka",
        string genre = "wiersz",
        string epoch = "romantyzm",
        Author[]? authors = null) => new()
    {
        Id = id,
        Title = title,
        Kind = kind,
        Genre = genre,
        Epoch = epoch,
        Url = $"https://wolnelektury.pl/katalog/lektura/{id}/",
        ThumbnailUrl = $"https://wolnelektury.pl/media/book/cover/{id}.jpg",
        CreatedAt = DateTimeOffset.UtcNow,
        Authors = (authors ?? []).ToList()
    };
}
