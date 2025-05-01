using System.Net;
using System.Text;
using Aggregator.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace ProductTest;

public class ProductServiceTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<ProductService>> _loggerMock;
    private readonly ProductService _productService;

    public ProductServiceTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<ProductService>>();
        
        var httpClient = new HttpClient(_httpMessageHandlerMock.Object)
        {
            BaseAddress = new Uri("http://localhost")
        };
        _productService = new ProductService(httpClient, _loggerMock.Object);
    }

    [Fact]
    public async Task GetProductAsync_WithValidProductId_ReturnsProduct()
    {
        // Arrange
        var productId = 1;
        var expectedResponse = "{\"id\":1,\"description\":\"Test Product\"}";
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString().EndsWith($"/api/products/{productId}")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponse, Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _productService.GetProductAsync(productId);

        // Assert
        Assert.Equal(expectedResponse, result);
    }

    [Fact]
    public async Task GetProductAsync_WithInvalidProductId_ThrowsException()
    {
        // Arrange
        var productId = 999;
        
        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString().EndsWith($"/api/products/{productId}")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(() => _productService.GetProductAsync(productId));
    }

    [Fact]
    public async Task GetProductsAsync_WithValidProductIds_ReturnsProducts()
    {
        // Arrange
        var productIds = new[] { 1, 2 };
        var expectedResponses = new[]
        {
            "{\"Id\":1,\"Description\":\"Product 1\"}",
            "{\"Id\":2,\"Description\":\"Product 2\"}"
        };

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString().EndsWith("/api/products/1")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponses[0], Encoding.UTF8, "application/json")
            });

        _httpMessageHandlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req => 
                    req.Method == HttpMethod.Get && 
                    req.RequestUri.ToString().EndsWith("/api/products/2")),
                ItExpr.IsAny<CancellationToken>()
            )
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(expectedResponses[1], Encoding.UTF8, "application/json")
            });

        // Act
        var result = await _productService.GetProductsAsync(productIds);

        // Assert
        Assert.Contains("\"Id\":1", result);
        Assert.Contains("\"Id\":2", result);
    }
}
