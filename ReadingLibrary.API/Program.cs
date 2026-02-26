using Microsoft.EntityFrameworkCore;
using ReadingLibrary;
using ReadingLibrary.API.Sync;
using ReadingLibrary.Clients.FreeReadingApi;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddDbContext<ReadingLibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Postgres"), 
        b => b.MigrationsAssembly("ReadingLibrary"))
    );

builder.Services.AddFreeReadingApi();
builder.Services.AddHostedService<SyncWorker>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ReadingLibraryDbContext>();
    await db.Database.MigrateAsync();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
