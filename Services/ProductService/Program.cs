using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add DbContext
builder.Services.AddDbContext<ProductDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("ProductDb")));

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
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    db.Database.EnsureCreated();
}

// Start listening for stock update messages
var rabbitMQService = app.Services.GetRequiredService<IRabbitMQService>();
rabbitMQService.ConsumeMessage<StockUpdateMessage>("stock-update", async message =>
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        foreach (var item in message.Items)
        {
            var product = await db.Products.FindAsync(item.ProductId);
            if (product != null)
            {
                product.StockQuantity -= item.Quantity;
                product.UpdatedAt = DateTime.UtcNow;
            }
        }

        await db.SaveChangesAsync();
        logger.LogInformation("Stock updated successfully for order {OrderId}", message.OrderId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error updating stock for order {OrderId}", message.OrderId);
    }
});

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
