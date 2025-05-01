using Common.Messaging;
using Common.Messaging.Models;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentProcessor
{
    Task ProcessOrderPaymentAsync(OrderCreatedMessage orderMessage);
}

public class PaymentProcessor : IPaymentProcessor
{
    private readonly PaymentDbContext _context;
    private readonly IRabbitMQService _rabbitMQService;
    private readonly ILogger<PaymentProcessor> _logger;

    public PaymentProcessor(
        PaymentDbContext context,
        IRabbitMQService rabbitMQService,
        ILogger<PaymentProcessor> logger)
    {
        _context = context;
        _rabbitMQService = rabbitMQService;
        _logger = logger;
    }

    public async Task ProcessOrderPaymentAsync(OrderCreatedMessage orderMessage)
    {
        try
        {
            // Simulate payment processing
            var payment = new Payment
            {
                OrderId = orderMessage.OrderId,
                UserId = orderMessage.UserId,
                Amount = orderMessage.TotalAmount,
                PaymentMethod = "Credit Card", // Bu kısmı gerçek uygulamada ödeme yöntemine göre ayarlayın
                Status = "Completed",
                TransactionId = Guid.NewGuid().ToString()
            };

            _context.Payments.Add(payment);
            await _context.SaveChangesAsync();

            // Publish payment result message
            var resultMessage = new PaymentResultMessage
            {
                OrderId = orderMessage.OrderId,
                UserId = orderMessage.UserId,
                Success = true,
                Message = "Payment processed successfully"
            };

            // Publish stock update message
            var stockUpdateMessage = new StockUpdateMessage
            {
                OrderId = orderMessage.OrderId,
                Items = orderMessage.Items.Select(item => new OrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity
                }).ToList()
            };

            // Publish basket clear message
            var clearBasketMessage = new ClearBasketMessage
            {
                BasketId = orderMessage.BasketId,
                UserId = orderMessage.UserId
            };

            _rabbitMQService.PublishMessage("payment-result", resultMessage);
            _rabbitMQService.PublishMessage("stock-update", stockUpdateMessage);
            _rabbitMQService.PublishMessage("clear-basket", clearBasketMessage);

            _logger.LogInformation("Payment processed successfully for order {OrderId}", orderMessage.OrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment for order {OrderId}", orderMessage.OrderId);

            var resultMessage = new PaymentResultMessage
            {
                OrderId = orderMessage.OrderId,
                UserId = orderMessage.UserId,
                Success = false,
                Message = "Payment processing failed"
            };

            _rabbitMQService.PublishMessage("payment-result", resultMessage);
            throw;
        }
    }
} 