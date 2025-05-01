namespace Aggregator.Services;

public interface IProductService
{
    Task<string> GetProductAsync(int productId);
    Task<string> GetProductsAsync(IEnumerable<int> productIds);
} 