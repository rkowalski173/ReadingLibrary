using Microsoft.EntityFrameworkCore;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;

namespace ReadingLibrary;

public class ReadingLibraryDbContext(DbContextOptions<ReadingLibraryDbContext> options) : DbContext(options)
{
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Author> Authors => Set<Author>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Book>()
            .HasMany(b => b.Authors)
            .WithMany(a => a.Books)
            .UsingEntity("BookAuthor");
    }
}
