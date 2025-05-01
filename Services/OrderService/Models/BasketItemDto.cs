namespace OrderService.Models;

public class BasketItemDto
{
    public int Id { get; set; }
    public string BasketId { get; set; } = string.Empty;
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
} 