using System.Text.Json;

namespace Aggregator.Services;

public class OrderService : IOrderService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<OrderService> _logger;

    public OrderService(HttpClient httpClient, ILogger<OrderService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetOrderAsync(string orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders/{orderId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order {OrderId}", orderId);
            throw;
        }
    }

    public async Task<string> GetOrdersByUserIdAsync(string userId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/orders?userId={userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting orders for user {UserId}", userId);
            throw;
        }
    }
} 