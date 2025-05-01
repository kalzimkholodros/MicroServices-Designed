namespace PaymentService.Models;

public class Payment
{
    public int Id { get; set; }
    public string OrderId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Status { get; set; } = "Pending";
    public string PaymentMethod { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string TransactionId { get; set; } = string.Empty;
} 