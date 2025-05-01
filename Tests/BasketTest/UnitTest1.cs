using System.Text.Json;
using BasketService.Controllers;
using BasketService.Data;
using BasketService.Models;
using BasketService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BasketTest;

public class BasketControllerTests
{
    private readonly BasketDbContext _context;
    private readonly Mock<IProductService> _productServiceMock;
    private readonly Mock<IRedisCacheService> _cacheServiceMock;
    private readonly Mock<ILogger<BasketController>> _loggerMock;
    private readonly BasketController _controller;

    public BasketControllerTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<BasketDbContext>()
            .UseInMemoryDatabase(databaseName: "TestBasketDb")
            .Options;
        _context = new BasketDbContext(options);
        _context.Database.EnsureDeleted(); // Clean database before each test
        _context.Database.EnsureCreated();

        // Setup mocks
        _productServiceMock = new Mock<IProductService>();
        _cacheServiceMock = new Mock<IRedisCacheService>();
        _loggerMock = new Mock<ILogger<BasketController>>();

        // Create controller
        _controller = new BasketController(_context, _productServiceMock.Object, _cacheServiceMock.Object);
    }

    [Fact]
    public async Task GetBasket_WithExistingBasket_ReturnsBasketItems()
    {
        // Arrange
        var basketId = "test-basket";
        var basketItems = new List<BasketItem>
        {
            new BasketItem
            {
                BasketId = basketId,
                ProductId = 1,
                ProductName = "Test Product",
                UnitPrice = 10.99m,
                Quantity = 2
            }
        };

        _context.BasketItems.AddRange(basketItems);
        await _context.SaveChangesAsync();

        _cacheServiceMock.Setup(x => x.GetAsync<List<BasketItem>>(It.IsAny<string>()))
            .ReturnsAsync((List<BasketItem>)null);

        // Act
        var result = await _controller.GetBasket(basketId);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var returnedItems = Assert.IsAssignableFrom<IEnumerable<BasketItem>>(okResult.Value);
        Assert.Single(returnedItems);
        Assert.Equal(basketId, returnedItems.First().BasketId);
    }

    [Fact]
    public async Task AddItemToBasket_WithValidProduct_ReturnsCreatedBasketItem()
    {
        // Arrange
        var basketId = "test-basket";
        var productDto = new ProductDto
        {
            Id = 1,
            Name = "Test Product",
            Price = 10.99m,
            Stock = 10
        };

        _productServiceMock.Setup(x => x.GetProductAsync(1))
            .ReturnsAsync(productDto);

        var addItemDto = new AddBasketItemDto
        {
            ProductId = 1,
            Quantity = 2
        };

        // Act
        var result = await _controller.AddItemToBasket(basketId, addItemDto);

        // Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
        var basketItem = Assert.IsType<BasketItem>(createdResult.Value);
        Assert.Equal(basketId, basketItem.BasketId);
        Assert.Equal(1, basketItem.ProductId);
        Assert.Equal("Test Product", basketItem.ProductName);
        Assert.Equal(10.99m, basketItem.UnitPrice);
        Assert.Equal(2, basketItem.Quantity);
    }

    [Fact]
    public async Task UpdateBasketItem_WithValidItem_ReturnsNoContent()
    {
        // Arrange
        var basketId = "test-basket";
        var basketItem = new BasketItem
        {
            BasketId = basketId,
            ProductId = 1,
            ProductName = "Test Product",
            UnitPrice = 10.99m,
            Quantity = 2
        };

        _context.BasketItems.Add(basketItem);
        await _context.SaveChangesAsync();

        var updateDto = new UpdateBasketItemDto
        {
            Quantity = 3
        };

        // Act
        var result = await _controller.UpdateBasketItem(basketId, basketItem.Id, updateDto);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var updatedItem = await _context.BasketItems.FindAsync(basketItem.Id);
        Assert.Equal(3, updatedItem.Quantity);
    }

    [Fact]
    public async Task RemoveBasketItem_WithValidItem_ReturnsNoContent()
    {
        // Arrange
        var basketId = "test-basket";
        var basketItem = new BasketItem
        {
            BasketId = basketId,
            ProductId = 1,
            ProductName = "Test Product",
            UnitPrice = 10.99m,
            Quantity = 2
        };

        _context.BasketItems.Add(basketItem);
        await _context.SaveChangesAsync();

        // Act
        var result = await _controller.RemoveBasketItem(basketId, basketItem.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        var deletedItem = await _context.BasketItems.FindAsync(basketItem.Id);
        Assert.Null(deletedItem);
    }
}
