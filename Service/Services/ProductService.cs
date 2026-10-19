using Domain.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Service.IServices;
using Service.Models;

namespace Service.Services;

public sealed class ProductService : IProductService
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 200;
    private readonly ApplicationContext applicationContext;

    public ProductService(ApplicationContext applicationContext)
    {
        this.applicationContext = applicationContext;
    }

    public async Task<PagedProducts> GetPagedAsync(ProductListQuery query)
    {
        var productsQuery = applicationContext.Set<Product>()
            .AsNoTracking()
            .Where(product => !product.IsDeleted);

        var search = query.SearchValue?.Trim();
        if (!string.IsNullOrWhiteSpace(search))
        {
            productsQuery = productsQuery.Where(product =>
                EF.Functions.ILike(product.Name, $"%{search}%") ||
                EF.Functions.ILike(product.Description, $"%{search}%"));
        }

        var totalCount = await productsQuery.CountAsync();
        var pageSize = query.PageSize <= 0 ? DefaultPageSize : Math.Min(query.PageSize, MaxPageSize);
        var pageNumber = query.PageNumber <= 0 ? 1 : query.PageNumber;
        var totalPages = Math.Max((int)Math.Ceiling(totalCount / (double)pageSize), 1);
        pageNumber = Math.Min(pageNumber, totalPages);

        var items = await productsQuery
            .OrderBy(product => product.Name)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(product => new ProductSummary
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                IsActive = product.IsActive
            })
            .ToListAsync();

        return new PagedProducts
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };
    }

    public async Task<ProductSummary?> GetByIdAsync(Guid id)
    {
        return await applicationContext.Set<Product>()
            .AsNoTracking()
            .Where(product => product.Id == id)
            .Select(ToSummaryExpression())
            .SingleOrDefaultAsync();
    }

    public async Task<ProductSummary> CreateAsync(CreateProductCommand command)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = command.Name.Trim(),
            Description = command.Description.Trim(),
            Price = command.Price,
            IsActive = true
        };

        applicationContext.Add(product);
        await applicationContext.SaveChangesAsync();
        return ToSummary(product);
    }

    public async Task<ProductSummary?> UpdateAsync(Guid id, UpdateProductCommand command)
    {
        var product = await applicationContext.Set<Product>().SingleOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return null;
        }

        product.Name = command.Name.Trim();
        product.Description = command.Description.Trim();
        product.Price = command.Price;
        product.IsActive = command.IsActive;
        await applicationContext.SaveChangesAsync();
        return ToSummary(product);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var product = await applicationContext.Set<Product>().SingleOrDefaultAsync(item => item.Id == id);
        if (product is null)
        {
            return false;
        }

        product.IsDeleted = true;
        product.IsActive = false;
        await applicationContext.SaveChangesAsync();
        return true;
    }

    private static ProductSummary ToSummary(Product product) => new()
    {
        Id = product.Id,
        Name = product.Name,
        Description = product.Description,
        Price = product.Price,
        IsActive = product.IsActive
    };

    private static System.Linq.Expressions.Expression<Func<Product, ProductSummary>> ToSummaryExpression() =>
        product => new ProductSummary
        {
            Id = product.Id,
            Name = product.Name,
            Description = product.Description,
            Price = product.Price,
            IsActive = product.IsActive
        };
}
