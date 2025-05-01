namespace Common.Messaging.Models;

public class StockUpdateMessage
{
    public string OrderId { get; set; } = string.Empty;
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
} 