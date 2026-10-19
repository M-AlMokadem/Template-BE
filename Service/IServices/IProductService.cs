using Service.Models;

namespace Service.IServices;

public interface IProductService
{
    Task<PagedProducts> GetPagedAsync(ProductListQuery query);
    Task<ProductSummary?> GetByIdAsync(Guid id);
    Task<ProductSummary> CreateAsync(CreateProductCommand command);
    Task<ProductSummary?> UpdateAsync(Guid id, UpdateProductCommand command);
    Task<bool> DeleteAsync(Guid id);
}
