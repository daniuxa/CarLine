using Microsoft.ML;
using CarLine.Common;
using CarLine.Common.DependencyInjection;
using CarLine.MLInterferenceService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddSingleton<MLContext>();
builder.Services.AddSingleton<CarPricePredictionService>();

builder.Services.AddControllers();

builder.Services.AddCarLineBlobStorage(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.MapControllers();

app.Run();