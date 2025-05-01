using System.Text.Json;

namespace Aggregator.Services;

public class ProductService : IProductService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProductService> _logger;

    public ProductService(HttpClient httpClient, ILogger<ProductService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string> GetProductAsync(int productId)
    {
        try
        {
            var response = await _httpClient.GetAsync($"/api/products/{productId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting product {ProductId}", productId);
            throw;
        }
    }

    public async Task<string> GetProductsAsync(IEnumerable<int> productIds)
    {
        try
        {
            var tasks = productIds.Select(id => GetProductAsync(id));
            var results = await Task.WhenAll(tasks);
            var products = results.Select(json => JsonSerializer.Deserialize<Product>(json)).ToList();
            return JsonSerializer.Serialize(products);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting products");
            throw;
        }
    }
}

public class Product
{
    public int Id { get; set; }
    public string Description { get; set; } = string.Empty;
} 