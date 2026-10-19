using API.Exceptions;
using API.Models.Products;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Service.IServices;
using Service.Models;
using Util.Core;

namespace API.Controllers;

[ApiController]
[Authorize]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService productService;

    public ProductsController(IProductService productService)
    {
        this.productService = productService;
    }

    [HttpGet]
    public async Task<IActionResult> GetPaged(
        [FromQuery] string? searchedValue,
        [FromQuery] int pageSize = 10,
        [FromQuery] int pageNumber = 1)
    {
        var result = await productService.GetPagedAsync(new ProductListQuery
        {
            SearchValue = searchedValue,
            PageSize = pageSize,
            PageNumber = pageNumber
        });

        var response = new PagedProductResponse
        {
            Items = result.Items.Select(ToResponse).ToList(),
            TotalCount = result.TotalCount,
            PageNumber = result.PageNumber,
            PageSize = result.PageSize,
            TotalPages = result.TotalPages,
            HasNextPage = result.HasNextPage,
            HasPreviousPage = result.HasPreviousPage
        };

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, response, "Products retrieved successfully."));
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var product = await productService.GetByIdAsync(id);
        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, ToResponse(product), "Product retrieved successfully."));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        var product = await productService.CreateAsync(new CreateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price
        });

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, new DefaultResponse(
            true,
            StatusCodes.Status201Created,
            ToResponse(product),
            "Product created successfully."));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateProductRequest request)
    {
        var product = await productService.UpdateAsync(id, new UpdateProductCommand
        {
            Name = request.Name,
            Description = request.Description,
            Price = request.Price,
            IsActive = request.IsActive
        });

        if (product is null)
        {
            throw new NotFoundException("Product was not found.");
        }

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, ToResponse(product), "Product updated successfully."));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        if (!await productService.DeleteAsync(id))
        {
            throw new NotFoundException("Product was not found.");
        }

        return Ok(new DefaultResponse(true, StatusCodes.Status200OK, true, "Product deleted successfully."));
    }

    private static ProductResponse ToResponse(ProductSummary product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        IsActive = product.IsActive
    };
}
