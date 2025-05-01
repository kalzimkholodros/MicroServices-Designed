using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using OcelotApiGateway;
using System.Net;
using Xunit;

namespace OcelotApiGatewayTest;

public class OcelotApiGatewayTests
{
    private readonly TestServer _server;
    private readonly HttpClient _client;

    public OcelotApiGatewayTests()
    {
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json")
            .AddJsonFile("ocelot.json")
            .Build();

        var builder = new WebHostBuilder()
            .UseConfiguration(configuration)
            .UseStartup<Startup>();

        _server = new TestServer(builder);
        _client = _server.CreateClient();
    }

    [Fact]
    public async Task ProductService_Route_Should_Return_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/products/1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task BasketService_Route_Should_Return_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/basket/user1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task OrderService_Route_Should_Return_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/orders/1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task PaymentService_Route_Should_Return_Success()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/payments/1");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Invalid_Route_Should_Return_NotFound()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/api/invalid/route");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Development_Environment_Should_Have_Swagger()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Get, "/swagger/index.html");

        // Act
        var response = await _client.SendAsync(request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public void Configuration_Should_Load_Correctly()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("ocelot.json")
            .Build();

        // Act
        var routes = configuration.GetSection("Routes").GetChildren();
        var globalConfig = configuration.GetSection("GlobalConfiguration");

        // Assert
        Assert.NotNull(routes);
        Assert.Equal(4, routes.Count()); // 4 servis route'u
        Assert.NotNull(globalConfig);
        Assert.Equal("https://localhost:5231", globalConfig["BaseUrl"]);
    }
} 