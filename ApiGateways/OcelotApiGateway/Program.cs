using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot;

var builder = WebApplication.CreateBuilder(args);

// Configure Kestrel to use HTTP only
builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(80);
});

// Add services to the container.
builder.Services.AddControllers();

// Add Ocelot
builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true);
builder.Services.AddOcelot(builder.Configuration);

// Configure Swagger with Ocelot
builder.Services.AddSwaggerForOcelot(builder.Configuration, (o) =>
{
    o.GenerateDocsForAggregates = true;
    o.GenerateDocsForGatewayItSelf = true;
});

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerForOcelotUI(opt =>
    {
        opt.PathToSwaggerGenerator = "/swagger/docs";
        opt.ReConfigureUpstreamSwaggerJson = AlterUpstreamSwaggerJson;
    });
}

app.UseAuthorization();

app.MapControllers();

// Use Ocelot
await app.UseOcelot();

await app.RunAsync();

static string AlterUpstreamSwaggerJson(HttpContext context, string swaggerJson)
{
    var swagger = System.Text.Json.JsonSerializer.Deserialize<System.Text.Json.JsonDocument>(swaggerJson);
    var modified = swagger.RootElement.GetRawText();
    return modified;
}
