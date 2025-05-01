using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Data;
using OrderService.Models;
using OrderService.Services;

namespace OrderService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly OrderDbContext _context;
    private readonly IBasketService _basketService;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<OrdersController> _logger;

    public OrdersController(
        OrderDbContext context,
        IBasketService basketService,
        IRabbitMQService rabbitMQService,
        ILogger<OrdersController> logger)
    {
        _context = context;
        _basketService = basketService;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
    {
        return await _context.Orders
            .Include(o => o.Items)
            .ToListAsync();
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Order>> GetOrder(int id)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null)
            return NotFound();

        return order;
    }

    [HttpPost]
    public async Task<ActionResult<Order>> CreateOrder([FromBody] CreateOrderDto dto)
    {
        try
        {
            // Get basket items from BasketService
            var basketItems = await _basketService.GetBasketItemsAsync(dto.BasketId);
            if (!basketItems.Any())
                return BadRequest("Basket is empty");

            // Calculate total price
            var totalPrice = basketItems.Sum(item => item.UnitPrice * item.Quantity);

            // Create order
            var order = new Order
            {
                UserId = dto.UserId,
                BasketId = dto.BasketId,
                TotalPrice = totalPrice,
                Status = "Pending",
                Items = basketItems.Select(item => new Models.OrderItem
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.UnitPrice * item.Quantity
                }).ToList()
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Publish order created message
            var orderCreatedMessage = new OrderCreatedMessage
            {
                OrderId = order.Id.ToString(),
                UserId = order.UserId,
                BasketId = order.BasketId,
                TotalAmount = order.TotalPrice,
                Items = order.Items.Select(item => new OrderItemMessage
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity
                }).ToList()
            };

            _rabbitMQService.PublishMessage("order-created", orderCreatedMessage);
            _logger.LogInformation("Order created and published to queue: {OrderId}", order.Id);

            return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating order for user {UserId}", dto.UserId);
            return StatusCode(500, new { Message = "Error creating order" });
        }
    }

    [HttpPut("{id}/status")]
    public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] UpdateOrderStatusDto dto)
    {
        var order = await _context.Orders.FindAsync(id);
        if (order == null)
            return NotFound();

        order.Status = dto.Status;
        order.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return NoContent();
    }
}

public class CreateOrderDto
{
    public string UserId { get; set; } = string.Empty;
    public string BasketId { get; set; } = string.Empty;
}

public class UpdateOrderStatusDto
{
    public string Status { get; set; } = string.Empty;
} 