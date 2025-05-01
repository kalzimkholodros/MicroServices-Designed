namespace Aggregator.Services;

public interface IOrderService
{
    Task<string> GetOrderAsync(string orderId);
    Task<string> GetOrdersByUserIdAsync(string userId);
} 