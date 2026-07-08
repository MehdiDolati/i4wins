using IoT.Twin.Application.Services;
using IoT.Twin.Domain.Interfaces;
using IoT.Twin.Infrastructure.Data;
using IoT.Twin.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite("Data Source=iot_twin.db"));

// Repositories & Services
builder.Services.AddScoped<IReadingRepository, ReadingRepository>();
builder.Services.AddScoped<ReadingService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Scalar API Reference
app.MapScalarApiReference(options =>
{
    options
        .WithTitle("IoT Twin API")
        .WithTheme(ScalarTheme.Kepler)
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient);
});

// Swagger
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "IoT Twin API v1");
    options.RoutePrefix = "swagger";
});

// Auto-load data at startup
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();

    var readingCount = await db.Readings.CountAsync();
    Console.WriteLine($"[Startup] Current readings in DB: {readingCount}");

    if (readingCount == 0)
    {
        var dataPath = Path.Combine(AppContext.BaseDirectory, "readings.jsonl");
        Console.WriteLine($"[Startup] Looking for data at: {dataPath}");
        Console.WriteLine($"[Startup] File exists: {File.Exists(dataPath)}");

        if (File.Exists(dataPath))
        {
            var records = JsonlReader.Read(dataPath);
            Console.WriteLine($"[Startup] Parsed {records.Count} records from JSONL");

            var cleaned = DataCleaner.Clean(records);
            Console.WriteLine($"[Startup] Cleaned to {cleaned.Count} records");

            var repo = scope.ServiceProvider.GetRequiredService<IReadingRepository>();
            await repo.AddRangeAsync(cleaned);
            Console.WriteLine($"[Startup] Loaded {cleaned.Count} readings into database");
        }
    }
}

app.MapControllers();

app.Run();
