using FluentValidation;
using Microsoft.EntityFrameworkCore;
using ReadingLibrary;
using ReadingLibrary.API.Models;
using ReadingLibrary.API.Sync;
using ReadingLibrary.API.Validation;
using ReadingLibrary.Authors;
using ReadingLibrary.Books;
using ReadingLibrary.Clients.FreeReadingApi;
using ReadingLibrary.Sync;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<ValidationFilter>());
builder.Services.AddOpenApi();

builder.Services.AddValidatorsFromAssemblyContaining<GetBooksRequestValidator>();

builder.Services.AddDbContext<ReadingLibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"), 
        b => b.MigrationsAssembly("ReadingLibrary"))
    );

builder.Services.AddScoped<BookPresenter>();
builder.Services.AddScoped<AuthorPresenter>();
builder.Services.AddScoped<LibrarySyncer>();

builder.Services.AddFreeReadingApi(builder.Configuration);
builder.Services.AddHostedService<SyncWorker>();

builder.Services.AddSingleton(TimeProvider.System);

var app = builder.Build();

app.MapOpenApi();
app.MapScalarApiReference();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();

public partial class Program { }
