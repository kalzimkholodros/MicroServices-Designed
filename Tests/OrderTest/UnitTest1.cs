using System.Text.Json;
using BasketService.Models;
using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using OrderService.Controllers;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;
using Xunit;

namespace OrderTest;

public class OrdersControllerTests
{
    private readonly OrderDbContext _context;
    private readonly Mock<IBasketService> _basketServiceMock;
    private readonly Mock<IRabbitMQService> _rabbitMQServiceMock;
    private readonly Mock<ILogger<OrdersController>> _loggerMock;
    private readonly OrdersController _controller;

    public OrdersControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<OrderDbContext>()
            .UseInMemoryDatabase(databaseName: "TestOrderDb")
            .Options;
        _context = new OrderDbContext(options);
        _context.Database.EnsureDeleted(); // Clean database before each test
        _context.Database.EnsureCreated();

        // Setup mocks
        _basketServiceMock = new Mock<IBasketService>();
        _rabbitMQServiceMock = new Mock<IRabbitMQService>();
        _loggerMock = new Mock<ILogger<OrdersController>>();

        // Create controller
        _controller = new OrdersController(
            _context,
            _basketServiceMock.Object,
            _rabbitMQServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task GetOrders_ReturnsAllOrders()
    {
        // Arrange
        var orders = new List<Order>
        {
            new Order
            {
                UserId = "user1",
                BasketId = "basket1",
                TotalPrice = 100.00m,
                Status = "Pending",
                Items = new List<OrderService.Models.OrderItem>
                {
                    new OrderService.Models.OrderItem
                    {
                        ProductId = 1,
                        ProductName = "Test Product",
                        UnitPrice = 50.00m,
                        Quantity = 2,
                        TotalPrice = 100.00m
                    }
                }
            }
        };

        _context.Orders.AddRange(orders);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetOrders();

        // Assert
        var okResult = Assert.IsType<ActionResult<IEnumerable<Order>>>(result);
        var returnedOrders = Assert.IsAssignableFrom<IEnumerable<Order>>(okResult.Value);
        Assert.Single(returnedOrders);
        Assert.Equal("user1", returnedOrders.First().UserId);
    }

    [Fact]
    public async Task GetOrder_WithExistingOrder_ReturnsOrder()
    {
        // Arrange
        var order = new Order
        {
            UserId = "user1",
            BasketId = "basket1",
            TotalPrice = 100.00m,
            Status = "Pending",
            Items = new List<OrderService.Models.OrderItem>
            {
                new OrderService.Models.OrderItem
                {
                    ProductId = 1,
                    ProductName = "Test Product",
                    UnitPrice = 50.00m,
                    Quantity = 2,
                    TotalPrice = 100.00m
                }
            }
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetOrder(order.Id);

        // Assert
        var okResult = Assert.IsType<ActionResult<Order>>(result);
        var returnedOrder = Assert.IsType<Order>(okResult.Value);
        Assert.Equal(order.Id, returnedOrder.Id);
        Assert.Equal("user1", returnedOrder.UserId);
        Assert.Single(returnedOrder.Items);
    }

    [Fact]
    public async Task GetOrder_WithNonExistingOrder_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetOrder(999);

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task CreateOrder_WithValidBasket_CreatesOrder()
    {
        // Arrange
        var basketItems = new List<BasketItemDto>
        {
            new BasketItemDto
            {
                ProductId = 1,
                ProductName = "Test Product",
                UnitPrice = 50.00m,
                Quantity = 2
            }
        };

        _basketServiceMock.Setup(x => x.GetBasketItemsAsync(It.IsAny<string>()))
            .ReturnsAsync(basketItems);

        var createOrderDto = new CreateOrderDto
        {
            UserId = "user1",
            BasketId = "basket1"
        };

        // Act
        var result = await _controller.CreateOrder(createOrderDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var order = Assert.IsType<Order>(createdResult.Value);
        Assert.Equal("user1", order.UserId);
        Assert.Equal("basket1", order.BasketId);
        Assert.Equal(100.00m, order.TotalPrice);
        Assert.Equal("Pending", order.Status);
        Assert.Single(order.Items);

        // Verify RabbitMQ message was published
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            It.IsAny<string>(),
            It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task CreateOrder_WithEmptyBasket_ReturnsBadRequest()
    {
        // Arrange
        _basketServiceMock.Setup(x => x.GetBasketItemsAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<BasketItemDto>());

        var createOrderDto = new CreateOrderDto
        {
            UserId = "user1",
            BasketId = "basket1"
        };

        // Act
        var result = await _controller.CreateOrder(createOrderDto);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
        Assert.Equal("Basket is empty", badRequestResult.Value);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithExistingOrder_UpdatesStatus()
    {
        // Arrange
        var order = new Order
        {
            UserId = "user1",
            BasketId = "basket1",
            TotalPrice = 100.00m,
            Status = "Pending"
        };

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateOrderStatusDto
        {
            Status = "Completed"
        };

        // Act
        var result = await _controller.UpdateOrderStatus(order.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var updatedOrder = await _context.Orders.FindAsync(order.Id);
        Assert.Equal("Completed", updatedOrder?.Status);
        Assert.NotNull(updatedOrder?.UpdatedAt);
    }

    [Fact]
    public async Task UpdateOrderStatus_WithNonExistingOrder_ReturnsNotFound()
    {
        // Arrange
        var updateDto = new UpdateOrderStatusDto
        {
            Status = "Completed"
        };

        // Act
        var result = await _controller.UpdateOrderStatus(999, updateDto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }
}
