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

        modelBuilder.Entity<Book>()
            .Property(b => b.Title)
            .UseCollation("und-x-icu");

        modelBuilder.Entity<Book>()
            .HasIndex(b => new { b.Title, b.Id });

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Kind);

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Genre);

        modelBuilder.Entity<Book>()
            .HasIndex(b => b.Epoch);

        modelBuilder.Entity<Author>()
            .Property(a => a.Name)
            .UseCollation("und-x-icu");

        modelBuilder.Entity<Author>()
            .HasIndex(a => new { a.Name, a.Id });
    }
}
