using Aggregator.Models;
using Aggregator.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Aggregator.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrderAggregatorController : ControllerBase
{
    private readonly IOrderService _orderService;
    private readonly IPaymentService _paymentService;
    private readonly IProductService _productService;
    private readonly ILogger<OrderAggregatorController> _logger;

    public OrderAggregatorController(
        IOrderService orderService,
        IPaymentService paymentService,
        IProductService productService,
        ILogger<OrderAggregatorController> logger)
    {
        _orderService = orderService;
        _paymentService = paymentService;
        _productService = productService;
        _logger = logger;
    }

    [HttpGet("orders/{orderId}")]
    public async Task<ActionResult<OrderDetailsViewModel>> GetOrderDetails(string orderId)
    {
        try
        {
            // Get order details
            var orderJson = await _orderService.GetOrderAsync(orderId);
            var order = JsonSerializer.Deserialize<Order>(orderJson.ToString());
            if (order == null)
                return NotFound();

            // Get payment details
            var paymentJson = await _paymentService.GetPaymentByOrderIdAsync(orderId);
            var payment = JsonSerializer.Deserialize<Payment>(paymentJson.ToString());

            // Get product details for each item
            var productIds = order.Items.Select(item => item.ProductId).ToList();
            var productsJson = await _productService.GetProductsAsync(productIds);
            var products = JsonSerializer.Deserialize<List<Product>>(productsJson.ToString());

            // Create view model
            var orderDetails = new OrderDetailsViewModel
            {
                OrderId = order.Id.ToString(),
                UserId = order.UserId,
                Status = order.Status,
                TotalAmount = order.TotalPrice,
                Items = order.Items.Select(item => new OrderItemViewModel
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName,
                    UnitPrice = item.UnitPrice,
                    Quantity = item.Quantity,
                    TotalPrice = item.TotalPrice,
                    ProductDescription = products?.FirstOrDefault(p => p.Id == item.ProductId)?.Description ?? string.Empty
                }).ToList()
            };

            if (payment != null)
            {
                orderDetails.Payment = new PaymentDetails
                {
                    TransactionId = payment.TransactionId,
                    Status = payment.Status,
                    PaymentDate = payment.CreatedAt,
                    PaymentMethod = payment.PaymentMethod
                };
            }

            return orderDetails;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order details for order {OrderId}", orderId);
            return StatusCode(500, new { Message = "Error getting order details" });
        }
    }

    [HttpGet("users/{userId}/orders")]
    public async Task<ActionResult<IEnumerable<OrderDetailsViewModel>>> GetUserOrders(string userId)
    {
        try
        {
            var ordersJson = await _orderService.GetOrdersByUserIdAsync(userId);
            var orders = JsonSerializer.Deserialize<List<Order>>(ordersJson.ToString());
            
            var orderDetailsTasks = orders.Select(order => GetOrderDetails(order.Id.ToString()));
            var orderDetails = await Task.WhenAll(orderDetailsTasks);
            
            return orderDetails.Where(result => result.Value != null)
                             .Select(result => result.Value!)
                             .ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            return StatusCode(500, new { Message = "Error getting user orders" });
        }
    }
}

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

public class Payment
{
    public string TransactionId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
}

public class Product
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
} 