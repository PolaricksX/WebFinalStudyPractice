using LibraryApi.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddAuthorization();

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")));


builder.Services.AddScoped<IRepositoryFactory, RepositoryFactory>(); // Scoped because it holds DbContext[cite: 2, 12]
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();               // Scoped because it holds DbContext[cite: 2, 12]
builder.Services.AddScoped<IBookService, BookService>();

var app = builder.Build();


app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
    app.MapOpenApi();

app.UseAuthorization();
app.MapControllers();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await db.Database.EnsureCreatedAsync();
    await LibrarySeed.ApplyAsync(db);
}

app.Run();

public partial class Program;
