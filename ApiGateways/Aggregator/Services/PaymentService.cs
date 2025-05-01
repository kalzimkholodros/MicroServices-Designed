using System.Text.Json;

namespace Aggregator.Services;

public class PaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<PaymentService> _logger;

    public PaymentService(HttpClient httpClient, ILogger<PaymentService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetPaymentByOrderIdAsync(string orderId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/payments/order/{orderId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting payment for order {OrderId}", orderId);
            throw;
        }
    }
} 