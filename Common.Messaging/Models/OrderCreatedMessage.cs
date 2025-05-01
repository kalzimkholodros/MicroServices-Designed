namespace Common.Messaging.Models;

public class OrderCreatedMessage
{
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string BasketId { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public List<OrderItemMessage> Items { get; set; } = new();
}

public class OrderItemMessage
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
} 