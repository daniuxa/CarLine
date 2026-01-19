using CarLine.API.Services;
using CarLine.Common.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddScoped<ICarsSearchService, CarsSearchService>();
builder.Services.AddScoped<ICarLineElasticsearchClient, CarLineElasticsearchClient>();

builder.Services.AddCarLineElasticsearch(builder.Configuration, disableDirectStreaming: false);

builder.Services.AddMlInferenceClient(builder.Configuration);

builder.Services.AddSubscriptionServiceClient(builder.Configuration);


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseCors();
app.MapControllers();

app.Run();