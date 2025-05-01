using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BasketService.Data;
using BasketService.Models;
using BasketService.Services;

namespace BasketService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BasketController : ControllerBase
{
    private readonly BasketDbContext _context;
    private readonly IProductService _productService;
    private readonly IRedisCacheService _cacheService;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(15);

    public BasketController(
        BasketDbContext context,
        IProductService productService,
        IRedisCacheService cacheService)
    {
        _context = context;
        _productService = productService;
        _cacheService = cacheService;
    }

    [HttpGet("{basketId}")]
    public async Task<ActionResult<IEnumerable<BasketItem>>> GetBasket(string basketId)
    {
        var cacheKey = $"basket:{basketId}";
        
        // Try to get from cache first
        var cachedBasket = await _cacheService.GetAsync<List<BasketItem>>(cacheKey);
        if (cachedBasket != null)
            return Ok(cachedBasket);

        // If not in cache, get from database
        var basketItems = await _context.BasketItems
            .Where(b => b.BasketId == basketId)
            .ToListAsync();

        // Cache the basket for 15 minutes
        await _cacheService.SetAsync(cacheKey, basketItems, _cacheExpiration);

        return Ok(basketItems);
    }

    [HttpPost("{basketId}/items")]
    public async Task<ActionResult<BasketItem>> AddItemToBasket(string basketId, [FromBody] AddBasketItemDto dto)
    {
        // Get product information from ProductService
        var product = await _productService.GetProductAsync(dto.ProductId);
        if (product == null)
            return NotFound("Product not found");

        var basketItem = new BasketItem
        {
            BasketId = basketId,
            ProductId = dto.ProductId,
            ProductName = product.Name,
            UnitPrice = product.Price,
            Quantity = dto.Quantity
        };

        // Save to database
        _context.BasketItems.Add(basketItem);
        await _context.SaveChangesAsync();

        // Update cache
        var cacheKey = $"basket:{basketId}";
        var cachedBasket = await _cacheService.GetAsync<List<BasketItem>>(cacheKey);
        if (cachedBasket != null)
        {
            cachedBasket.Add(basketItem);
            await _cacheService.SetAsync(cacheKey, cachedBasket, _cacheExpiration);
        }

        return CreatedAtAction(nameof(GetBasket), new { basketId }, basketItem);
    }

    [HttpPut("{basketId}/items/{id}")]
    public async Task<IActionResult> UpdateBasketItem(string basketId, int id, [FromBody] UpdateBasketItemDto dto)
    {
        var basketItem = await _context.BasketItems.FindAsync(id);
        if (basketItem == null || basketItem.BasketId != basketId)
            return NotFound();

        basketItem.Quantity = dto.Quantity;
        basketItem.UpdatedAt = DateTime.UtcNow;

        // Update database
        await _context.SaveChangesAsync();

        // Update cache
        var cacheKey = $"basket:{basketId}";
        var cachedBasket = await _cacheService.GetAsync<List<BasketItem>>(cacheKey);
        if (cachedBasket != null)
        {
            var itemToUpdate = cachedBasket.FirstOrDefault(i => i.Id == id);
            if (itemToUpdate != null)
            {
                itemToUpdate.Quantity = dto.Quantity;
                itemToUpdate.UpdatedAt = DateTime.UtcNow;
                await _cacheService.SetAsync(cacheKey, cachedBasket, _cacheExpiration);
            }
        }

        return NoContent();
    }

    [HttpDelete("{basketId}/items/{id}")]
    public async Task<IActionResult> RemoveBasketItem(string basketId, int id)
    {
        var basketItem = await _context.BasketItems.FindAsync(id);
        if (basketItem == null || basketItem.BasketId != basketId)
            return NotFound();

        // Remove from database
        _context.BasketItems.Remove(basketItem);
        await _context.SaveChangesAsync();

        // Update cache
        var cacheKey = $"basket:{basketId}";
        var cachedBasket = await _cacheService.GetAsync<List<BasketItem>>(cacheKey);
        if (cachedBasket != null)
        {
            cachedBasket.RemoveAll(i => i.Id == id);
            await _cacheService.SetAsync(cacheKey, cachedBasket, _cacheExpiration);
        }

        return NoContent();
    }
}

public class AddBasketItemDto
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
}

public class UpdateBasketItemDto
{
    public int Quantity { get; set; }
} 