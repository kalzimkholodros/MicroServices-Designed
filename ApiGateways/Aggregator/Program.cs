using Aggregator.Services;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP only
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

// Add services to the container.
builder.Services.AddControllers();

// Configure HttpClient for services
builder.Services.AddHttpClient<IOrderService, OrderService>(client =>
{
    client.BaseAddress = new Uri("http://order-service:5132");
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<IPaymentService, PaymentService>(client =>
{
    client.BaseAddress = new Uri("http://payment-service:5017");
})
.AddPolicyHandler(GetRetryPolicy());

builder.Services.AddHttpClient<IProductService, ProductService>(client =>
{
    client.BaseAddress = new Uri("http://product-service:5014");
})
.AddPolicyHandler(GetRetryPolicy());

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

await app.RunAsync();

static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .WaitAndRetryAsync(3, retryAttempt => 
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
}
