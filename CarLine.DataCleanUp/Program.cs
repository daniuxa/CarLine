using CarLine.DataCleanUp;
using CarLine.Common.DependencyInjection;
using CarLine.DataCleanUp.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddHostedService<Worker>();

builder.Services.AddControllers();

builder.Services.AddCarLineMongoClient(builder.Configuration, allowLocalFallback: false);

builder.Services.AddCarLineBlobStorage(builder.Configuration);

builder.Services.AddCarLineElasticsearch(builder.Configuration, disableDirectStreaming: true);

builder.Services.AddScoped<DataCleanupService>();

var app = builder.Build();

app.MapControllers();

app.Run();