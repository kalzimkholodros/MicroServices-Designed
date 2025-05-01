using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using PaymentService.Controllers;
using PaymentService.Data;
using PaymentService.Models;
using PaymentService.Services;
using Xunit;

namespace PaymentTest;

public class PaymentTests
{
    private readonly PaymentDbContext _context;
    private readonly Mock<IRabbitMQService> _rabbitMQServiceMock;
    private readonly Mock<ILogger<PaymentController>> _loggerMock;
    private readonly PaymentController _controller;
    private readonly IPaymentProcessor _paymentProcessor;

    public PaymentTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<PaymentDbContext>()
            .UseInMemoryDatabase(databaseName: "TestPaymentDb")
            .Options;
        _context = new PaymentDbContext(options);
        _context.Database.EnsureDeleted(); // Clean database before each test
        _context.Database.EnsureCreated();

        // Setup mocks
        _rabbitMQServiceMock = new Mock<IRabbitMQService>();
        _loggerMock = new Mock<ILogger<PaymentController>>();

        // Create controller and processor
        _controller = new PaymentController(_context, _rabbitMQServiceMock.Object, _loggerMock.Object);
        _paymentProcessor = new PaymentProcessor(_context, _rabbitMQServiceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task ProcessPayment_WithValidPayment_CreatesPayment()
    {
        // Arrange
        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == "order1");
        Assert.NotNull(payment);
        Assert.Equal("Completed", payment.Status);
        Assert.Equal("Credit Card", payment.PaymentMethod);
        Assert.Equal(100.00m, payment.Amount);

        // Verify RabbitMQ messages were published
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "payment-result", It.IsAny<PaymentResultMessage>()), Times.Once);
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "stock-update", It.IsAny<StockUpdateMessage>()), Times.Once);
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "clear-basket", It.IsAny<ClearBasketMessage>()), Times.Once);
    }

    [Fact]
    public async Task GetPayment_WithExistingPayment_ReturnsPayment()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = "order1",
            UserId = "user1",
            Amount = 100.00m,
            Status = "Completed",
            PaymentMethod = "Credit Card",
            TransactionId = Guid.NewGuid().ToString()
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.GetPayment("order1");

        // Assert
        var okResult = Assert.IsType<ActionResult<Payment>>(result);
        var returnedPayment = Assert.IsType<Payment>(okResult.Value);
        Assert.Equal(payment.Id, returnedPayment.Id);
        Assert.Equal("order1", returnedPayment.OrderId);
        Assert.Equal("Completed", returnedPayment.Status);
    }

    [Fact]
    public async Task GetPayment_WithNonExistingPayment_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetPayment("nonexistent");

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task ProcessOrderPayment_WithValidOrder_CreatesPayment()
    {
        // Arrange
        var orderMessage = new OrderCreatedMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            TotalAmount = 100.00m,
            Items = new List<OrderItemMessage>
            {
                new OrderItemMessage { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        await _paymentProcessor.ProcessOrderPaymentAsync(orderMessage);

        // Assert
        var payment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == "order1");
        Assert.NotNull(payment);
        Assert.Equal("Completed", payment.Status);
        Assert.Equal(100.00m, payment.Amount);

        // Verify RabbitMQ messages were published
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "payment-result", It.IsAny<PaymentResultMessage>()), Times.Once);
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "stock-update", It.IsAny<StockUpdateMessage>()), Times.Once);
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "clear-basket", It.IsAny<ClearBasketMessage>()), Times.Once);
    }

    [Fact]
    public async Task ProcessOrderPayment_WithError_PublishesFailureMessage()
    {
        // Arrange
        var orderMessage = new OrderCreatedMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            TotalAmount = 100.00m,
            Items = new List<OrderItemMessage>()
        };

        // Simulate database error
        _context.Database.EnsureDeleted();

        // Act & Assert
        await Assert.ThrowsAsync<Exception>(() => 
            _paymentProcessor.ProcessOrderPaymentAsync(orderMessage));

        // Verify failure message was published
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "payment-result", It.Is<PaymentResultMessage>(m => !m.Success)), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_WithPendingStatus_UpdatesStatusToCompleted()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = "order1",
            UserId = "user1",
            Amount = 100.00m,
            Status = "Pending",
            PaymentMethod = "Credit Card",
            TransactionId = Guid.NewGuid().ToString()
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPayment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == "order1");
        Assert.NotNull(updatedPayment);
        Assert.Equal("Completed", updatedPayment.Status);
    }

    [Fact]
    public async Task ProcessPayment_WithFailedStatus_RemainsFailed()
    {
        // Arrange
        var payment = new Payment
        {
            OrderId = "order1",
            UserId = "user1",
            Amount = 100.00m,
            Status = "Failed",
            PaymentMethod = "Credit Card",
            TransactionId = Guid.NewGuid().ToString()
        };
        _context.Payments.Add(payment);
        await _context.SaveChangesAsync();

        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var updatedPayment = await _context.Payments.FirstOrDefaultAsync(p => p.OrderId == "order1");
        Assert.NotNull(updatedPayment);
        Assert.Equal("Failed", updatedPayment.Status);
    }

    [Fact]
    public async Task ProcessPayment_PublishesCorrectPaymentResultMessage()
    {
        // Arrange
        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "payment-result",
            It.Is<PaymentResultMessage>(m => 
                m.OrderId == "order1" && 
                m.Success == true && 
                m.Amount == 100.00m
            )), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_PublishesCorrectStockUpdateMessage()
    {
        // Arrange
        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        _rabbitMQServiceMock.Verify(x => x.PublishMessage(
            "stock-update",
            It.Is<StockUpdateMessage>(m => 
                m.OrderId == "order1" && 
                m.Items.Count == 1 && 
                m.Items[0].ProductId == 1 && 
                m.Items[0].Quantity == 2
            )), Times.Once);
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidAmount_ReturnsBadRequest()
    {
        // Arrange
        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "user1",
            BasketId = "basket1",
            Amount = -100.00m, // Invalid amount
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("Amount", badRequestResult.Value.ToString());
    }

    [Fact]
    public async Task ProcessPayment_WithInvalidUserId_ReturnsBadRequest()
    {
        // Arrange
        var paymentMessage = new PaymentMessage
        {
            OrderId = "order1",
            UserId = "", // Invalid user ID
            BasketId = "basket1",
            Amount = 100.00m,
            PaymentMethod = "Credit Card",
            Items = new List<OrderItem>
            {
                new OrderItem { ProductId = 1, Quantity = 2 }
            }
        };

        // Act
        var result = await _controller.ProcessPayment(paymentMessage);

        // Assert
        var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
        Assert.Contains("UserId", badRequestResult.Value.ToString());
    }
}
