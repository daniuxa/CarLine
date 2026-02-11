using CarLine.Common.DependencyInjection;
using CarLine.Crawler;
using CarLine.Crawler.Services;
using Microsoft.AspNetCore.Http.Features;

var builder = WebApplication.CreateBuilder(args);

// Increase multipart body length limit globally (300 MB)
builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = 314_572_800; // 300 MB
});

// Configure Kestrel server to accept larger request bodies
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = 314_572_800; // 300 MB
});

builder.Services.Configure<CrawlerSettings>(
    builder.Configuration.GetSection("CrawlerSettings"));

builder.Services.AddHttpClient();

// These depend on IMongoDatabase (scoped via AddCarLineMongoDatabase), so they must not be singletons.
builder.Services.AddScoped<ICarCrawlerService, CarCrawlerService>();
builder.Services.AddHostedService<Worker>();

builder.Services.AddControllers();

builder.Services.AddCarLineMongoClient(builder.Configuration, allowLocalFallback: false);

builder.Services.AddCarLineMongoDatabase("carsnosql");

// Repository depends on IMongoDatabase (scoped)
builder.Services.AddScoped<ICrawledCarsRepository, MongoCrawledCarsRepository>();

var app = builder.Build();

app.MapControllers();

app.Run();