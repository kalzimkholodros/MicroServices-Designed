using Microsoft.AspNetCore.Mvc;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories;

namespace ProductService.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _repository;
    private readonly ILogger<ProductsController> _logger;

    public ProductsController(IProductRepository repository, ILogger<ProductsController> logger)
    {
        _repository = repository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
    {
        var products = await _repository.GetAllProductsAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<Product>> GetProduct(int id)
    {
        var product = await _repository.GetProductByIdAsync(id);
        if (product == null)
            return NotFound();

        return product;
    }

    [HttpPost]
    public async Task<ActionResult<Product>> CreateProduct(Product product)
    {
        var createdProduct = await _repository.CreateProductAsync(product);
        return CreatedAtAction(nameof(GetProduct), new { id = createdProduct.Id }, createdProduct);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateProduct(int id, Product product)
    {
        var updatedProduct = await _repository.UpdateProductAsync(id, product);
        if (updatedProduct == null)
            return NotFound();

        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        var result = await _repository.DeleteProductAsync(id);
        if (!result)
            return NotFound();

        return NoContent();
    }

    [HttpPut("{id}/stock")]
    public async Task<IActionResult> UpdateStock(int id, [FromBody] int quantity)
    {
        var result = await _repository.UpdateStockAsync(id, quantity);
        if (!result)
            return NotFound();

        return NoContent();
    }
} 