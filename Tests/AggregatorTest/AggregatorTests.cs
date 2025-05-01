using Aggregator.Controllers;
using Aggregator.Services;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Net;
using System.Text.Json;
using Xunit;

namespace AggregatorTest;

public class AggregatorTests
{
    private readonly Mock<IOrderService> _orderServiceMock;
    private readonly Mock<IPaymentService> _paymentServiceMock;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly OrderAggregatorController _controller;

    public AggregatorTests()
    {
        _orderServiceMock = new Mock<IOrderService>();
        _paymentServiceMock = new Mock<IPaymentService>();
        _productServiceMock = new Mock<IProductService>();

        _controller = new OrderAggregatorController(
            _orderServiceMock.Object,
            _paymentServiceMock.Object,
            _productServiceMock.Object);
    }

    [Fact]
    public async Task GetOrderDetails_WithValidOrderId_ReturnsCombinedData()
    {
        // Arrange
        var orderId = "order1";
        var orderJson = JsonSerializer.Serialize(new { Id = orderId, UserId = "user1", TotalAmount = 100.00m });
        var paymentJson = JsonSerializer.Serialize(new { OrderId = orderId, Status = "Completed", Amount = 100.00m });
        var productJson = JsonSerializer.Serialize(new { Id = 1, Name = "Product1", Price = 50.00m });

        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(orderJson);
        _paymentServiceMock.Setup(x => x.GetPaymentByOrderIdAsync(orderId))
            .ReturnsAsync(paymentJson);
        _productServiceMock.Setup(x => x.GetProductAsync("1"))
            .ReturnsAsync(productJson);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        Assert.Contains(orderId, JsonSerializer.Serialize(response));
        Assert.Contains("Completed", JsonSerializer.Serialize(response));
        Assert.Contains("Product1", JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task GetOrderDetails_WithNonExistingOrder_ReturnsNotFound()
    {
        // Arrange
        var orderId = "nonexistent";
        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync((string)null);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetOrderDetails_WhenOrderServiceFails_ReturnsServiceUnavailable()
    {
        // Arrange
        var orderId = "order1";
        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        var statusCodeResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal((int)HttpStatusCode.ServiceUnavailable, statusCodeResult.StatusCode);
    }

    [Fact]
    public async Task GetOrderDetails_WhenPaymentServiceFails_ReturnsPartialData()
    {
        // Arrange
        var orderId = "order1";
        var orderJson = JsonSerializer.Serialize(new { Id = orderId, UserId = "user1", TotalAmount = 100.00m });
        var productJson = JsonSerializer.Serialize(new { Id = 1, Name = "Product1", Price = 50.00m });

        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(orderJson);
        _paymentServiceMock.Setup(x => x.GetPaymentByOrderIdAsync(orderId))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));
        _productServiceMock.Setup(x => x.GetProductAsync("1"))
            .ReturnsAsync(productJson);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = okResult.Value;
        Assert.NotNull(response);
        Assert.Contains(orderId, JsonSerializer.Serialize(response));
        Assert.DoesNotContain("Completed", JsonSerializer.Serialize(response));
        Assert.Contains("Product1", JsonSerializer.Serialize(response));
    }

    [Fact]
    public async Task GetOrderDetails_WithInvalidJson_ReturnsBadRequest()
    {
        // Arrange
        var orderId = "order1";
        var invalidJson = "invalid json";
        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(invalidJson);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Invalid JSON", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task GetOrderDetails_WithNullServiceResponse_ReturnsNotFound()
    {
        // Arrange
        var orderId = "order1";
        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync((string)null);
        _paymentServiceMock.Setup(x => x.GetPaymentByOrderIdAsync(orderId))
            .ReturnsAsync((string)null);
        _productServiceMock.Setup(x => x.GetProductAsync("1"))
            .ReturnsAsync((string)null);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetOrderDetails_WithRetryPolicy_RetriesOnFailure()
    {
        // Arrange
        var orderId = "order1";
        var orderJson = JsonSerializer.Serialize(new { Id = orderId, UserId = "user1", TotalAmount = 100.00m });
        var paymentJson = JsonSerializer.Serialize(new { OrderId = orderId, Status = "Completed", Amount = 100.00m });
        var productJson = JsonSerializer.Serialize(new { Id = 1, Name = "Product1", Price = 50.00m });

        var callCount = 0;
        _orderServiceMock.Setup(x => x.GetOrderAsync(orderId))
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 2)
                    throw new HttpRequestException("Temporary failure");
                return orderJson;
            });
        _paymentServiceMock.Setup(x => x.GetPaymentByOrderIdAsync(orderId))
            .ReturnsAsync(paymentJson);
        _productServiceMock.Setup(x => x.GetProductAsync("1"))
            .ReturnsAsync(productJson);

        // Act
        var result = await _controller.GetOrderDetails(orderId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.Equal(3, callCount); // 2 failed attempts + 1 successful
    }
} 