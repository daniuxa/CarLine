using CarLine.Common.DependencyInjection;
using CarLine.SubscriptionService;
using CarLine.SubscriptionService.Data;
using CarLine.SubscriptionService.Email;
using CarLine.SubscriptionService.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// SQL Server (subscriptions DB)
var sqlConnectionString = builder.Configuration.GetConnectionString("subscriptionsdb")
                           ?? builder.Configuration.GetConnectionString("sqlserver")
                           ?? builder.Configuration.GetConnectionString("subscriptions")
                           ?? builder.Configuration["ConnectionStrings:subscriptionsdb"]
                           ?? throw new InvalidOperationException("No connection string configured for subscriptionsdb.");

builder.Services.AddDbContext<SubscriptionDbContext>(options =>
    options.UseSqlServer(sqlConnectionString));

// Mongo for reading new cars
builder.Services.AddCarLineMongoClient(builder.Configuration, allowLocalFallback: true);

builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddSingleton<IEmailSender, SmtpEmailSender>();
builder.Services.AddSingleton<ISubscriptionDigestTemplateBuilder, EmailDigestTemplateBuilder>();

builder.Services.AddScoped<MongoCarsRepository>();
builder.Services.AddScoped<SubscriptionProcessingService>();

builder.Services.AddHostedService<Worker>();

var app = builder.Build();

// Ensure DB exists (simple approach for demo; replace with migrations later)
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<SubscriptionDbContext>();
    await db.Database.EnsureCreatedAsync();
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();
