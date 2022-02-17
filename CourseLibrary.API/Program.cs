using CourseLibrary.API.DbContexts;
using CourseLibrary.API.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddScoped<ICourseLibraryRepository, CourseLibraryRepoistory>();

builder.Services.AddDbContext<CourseLibraryContext>(options =>
    options.UseSqlite("Data Source=CourseLibraryDB.db"));

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();

app.MapControllers();

using var scope = app.Services.CreateScope();
try
{
    var context = scope.ServiceProvider.GetService<CourseLibraryContext>();
    context.Database.EnsureDeleted();
    context.Database.Migrate();
}
catch (Exception ex)
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    logger.LogError(ex, "An error occurred while migrating the database.");
}

app.Run();