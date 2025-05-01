namespace OrderService.Models;

public class Order
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string BasketId { get; set; } = string.Empty;
    public decimal TotalPrice { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<OrderItem> Items { get; set; } = new();
} 