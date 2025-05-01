using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Add DbContext
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PaymentDb")));

// Add RabbitMQ configuration and service
builder.Services.Configure<RabbitMQSettings>(builder.Configuration.GetSection("RabbitMQ"));
builder.Services.AddSingleton<IRabbitMQService, RabbitMQService>();

// Add PaymentProcessor service
builder.Services.AddScoped<IPaymentProcessor, PaymentProcessor>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
    var db = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    db.Database.EnsureCreated();
}

// Start listening for order created messages
var rabbitMQService = app.Services.GetRequiredService<IRabbitMQService>();
rabbitMQService.ConsumeMessage<OrderCreatedMessage>("order-created", async message =>
{
    using var scope = app.Services.CreateScope();
    var paymentProcessor = scope.ServiceProvider.GetRequiredService<IPaymentProcessor>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    try
    {
        await paymentProcessor.ProcessOrderPaymentAsync(message);
        logger.LogInformation("Order payment processed: {OrderId}", message.OrderId);
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Error processing order payment: {OrderId}", message.OrderId);
    }
});

app.Run();
