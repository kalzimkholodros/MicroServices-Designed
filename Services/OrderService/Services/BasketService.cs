using OrderService.Models;

namespace OrderService.Services;

public interface IBasketService
{
    Task<List<BasketItemDto>> GetBasketItemsAsync(string basketId);
}

public class BasketService : IBasketService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public BasketService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _httpClient.BaseAddress = new Uri(_configuration["BasketServiceUrl"]!);
    }

    public async Task<List<BasketItemDto>> GetBasketItemsAsync(string basketId)
    {
        var response = await _httpClient.GetAsync($"/api/basket/{basketId}");
        if (!response.IsSuccessStatusCode)
            return new List<BasketItemDto>();

        var basketItems = await response.Content.ReadFromJsonAsync<List<BasketItemDto>>();
        return basketItems ?? new List<BasketItemDto>();
    }
} 