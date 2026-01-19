using CarLine.ExternalCarSellerStub.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddOpenApi();
builder.Services.AddSingleton<CarInventoryService>();

// Controllers (instead of Minimal API endpoints)
builder.Services.AddControllers();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment()) app.MapOpenApi();

app.UseHttpsRedirection();

// Map attribute-routed controllers
app.MapControllers();

app.Run();