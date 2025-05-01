using BasketService.Models;

namespace BasketService.Services;

public interface IProductService
{
    Task<ProductDto?> GetProductAsync(int productId);
}

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly IRedisCacheService _cacheService;

    public ProductService(HttpClient httpClient, IRedisCacheService cacheService, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _cacheService = cacheService;
        _httpClient.BaseAddress = new Uri(configuration["ProductServiceUrl"]!);
    }

    public async Task<ProductDto?> GetProductAsync(int productId)
    {
        var cacheKey = $"product:{productId}";
        
        // Try to get from cache first
        var cachedProduct = await _cacheService.GetAsync<ProductDto>(cacheKey);
        if (cachedProduct != null)
            return cachedProduct;

        // If not in cache, get from API
        var response = await _httpClient.GetAsync($"/api/products/{productId}");
        if (!response.IsSuccessStatusCode)
            return null;

        var product = await response.Content.ReadFromJsonAsync<ProductDto>();
        if (product != null)
        {
            // Cache the product for 5 minutes
            await _cacheService.SetAsync(cacheKey, product, TimeSpan.FromMinutes(5));
        }

        return product;
    }
} 