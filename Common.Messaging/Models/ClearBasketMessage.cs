namespace Common.Messaging.Models;

public class ClearBasketMessage
{
    public string BasketId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
} 