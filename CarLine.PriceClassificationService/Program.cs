using CarLine.PriceClassificationService;
using CarLine.Common.DependencyInjection;
using CarLine.PriceClassificationService.Services;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddCarLineMongoClient(builder.Configuration, allowLocalFallback: true);

builder.Services.AddMlInferenceClient(builder.Configuration);

builder.Services.AddCarLineElasticsearch(builder.Configuration, disableDirectStreaming: true);

builder.Services.AddSingleton<PriceClassificationService>();
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();