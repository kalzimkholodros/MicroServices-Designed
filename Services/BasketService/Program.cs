using BasketService.Data;
using BasketService.Services;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using Common.Messaging;
using Common.Messaging.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<BasketDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("BasketDb")));

// Add Redis with configuration
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var configuration = ConfigurationOptions.Parse(builder.Configuration.GetConnectionString("Redis")!);
    configuration.AbortOnConnectFail = false;
    configuration.ConnectRetry = 3;
    configuration.ConnectTimeout = 5000;
    return ConnectionMultiplexer.Connect(configuration);
});
builder.Services.AddScoped<IRedisCacheService, RedisCacheService>();

// Add HttpClient for ProductService
builder.Services.AddHttpClient<IProductService, ProductService>();

// Add RabbitMQ configuration and service
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();

// Ensure database is created
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    db.Database.EnsureCreated();
}

// Start listening for basket clearing messages
var rabbitMQService = app.Services.GetRequiredService<IRabbitMQService>();
rabbitMQService.ConsumeMessage<ClearBasketMessage>("clear-basket", async message =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<BasketDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        var basketItems = await db.BasketItems
            .Where(bi => bi.BasketId == message.BasketId)
            .ToListAsync();

        db.BasketItems.RemoveRange(basketItems);
        await db.SaveChangesAsync();

        logger.LogInformation("Basket cleared successfully for user {UserId}", message.UserId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error clearing basket for user {UserId}", message.UserId);
    }
});

app.Run();
