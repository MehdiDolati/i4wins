using i4Twins.API.Middleware;
using i4Twins.Infrastructure.Data;
using i4Twins.Infrastructure.Extensions;
using Microsoft.Data.Sqlite;

var builder = WebApplication.CreateBuilder(args);

// Configure SQLite
var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "readings.db");
var connectionString = $"Data Source={dbPath};";

// Add services
builder.Services.AddInfrastructure(connectionString);
builder.Services.AddApplicationServices();

// Initialize database
using (var connection = new SqliteConnection(connectionString))
{
    connection.Open();
    var init = new DatabaseInitializer(connection);
    init.Initialize();
}

// Add API services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "i4Twins Sensor Data API",
        Version = "v1",
        Description = "API for ingesting and aggregating sensor readings"
    });
});

var app = builder.Build();

// Configure pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// Ensure data directory exists
var dataDir = Path.Combine(Directory.GetCurrentDirectory(), "data");
if (!Directory.Exists(dataDir))
    Directory.CreateDirectory(dataDir);

app.Run();