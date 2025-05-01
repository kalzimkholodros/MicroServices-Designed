using Common.Messaging.Models;

namespace PaymentService.Models;

public class PaymentMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string BasketId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class PaymentResultMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
} 