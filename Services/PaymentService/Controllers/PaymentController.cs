using Common.Messaging;
using Common.Messaging.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly PaymentDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        PaymentDbContext context,
        IRabbitMQService rabbitMQService,
        ILogger<PaymentController> logger)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    [HttpPost]
    public async Task<IActionResult> ProcessPayment([FromBody] PaymentMessage paymentMessage)
    {
        try
        {
            // Simulate payment processing
            var payment = new Payment
            {
                OrderId = paymentMessage.OrderId,
                UserId = paymentMessage.UserId,
                Amount = paymentMessage.Amount,
                PaymentMethod = paymentMessage.PaymentMethod,
                Status = "Completed",
                TransactionId = Guid.NewGuid().ToString()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Publish payment result message
            var resultMessage = new PaymentResultMessage
            {
                OrderId = paymentMessage.OrderId,
                UserId = paymentMessage.UserId,
                Success = true,
                Message = "Payment processed successfully"
            };

            // Publish messages for stock update and basket clearing
            var stockUpdateMessage = new StockUpdateMessage
            {
                OrderId = paymentMessage.OrderId,
                Items = paymentMessage.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            };

            var clearBasketMessage = new ClearBasketMessage
            {
                BasketId = paymentMessage.BasketId,
                UserId = paymentMessage.UserId
            };

            _rabbitMQService.PublishMessage("payment-result", resultMessage);
            _rabbitMQService.PublishMessage("stock-update", stockUpdateMessage);
            _rabbitMQService.PublishMessage("clear-basket", clearBasketMessage);

            return Ok(new { Message = "Payment processed successfully", TransactionId = payment.TransactionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", paymentMessage.OrderId);

            var resultMessage = new PaymentResultMessage
            {
                OrderId = paymentMessage.OrderId,
                UserId = paymentMessage.UserId,
                Success = false,
                Message = "Payment processing failed"
            };

            _rabbitMQService.PublishMessage("payment-result", resultMessage);

            return StatusCode(500, new { Message = "Error processing payment" });
        }
    }

    [HttpGet("{orderId}")]
    public async Task<ActionResult<Payment>> GetPayment(string orderId)
    {
        var payment = await _context.Payments
            .FirstOrDefaultAsync(p => p.OrderId == orderId);

        if (payment == null)
            return NotFound();

        return payment;
    }
} 