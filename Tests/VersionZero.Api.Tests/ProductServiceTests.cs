using Domain.Context;
using Domain.Models;
using Microsoft.EntityFrameworkCore;
using Service.Models;
using Service.Services;
using Testcontainers.PostgreSql;

namespace VersionZero.Api.Tests;

public sealed class ProductServiceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    private ApplicationContext context = null!;
    private ProductService productService = null!;

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        var options = new DbContextOptionsBuilder<ApplicationContext>()
            .UseNpgsql(postgres.GetConnectionString())
            .Options;

        context = new ApplicationContext(options);
        await context.Database.EnsureCreatedAsync();
        productService = new ProductService(context);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task CreateAsync_PersistsProductWithActiveStatus()
    {
        var result = await productService.CreateAsync(new CreateProductCommand
        {
            Name = " Starter Product ",
            Description = "A reusable example product.",
            Price = 19.95m
        });

        var persisted = await context.Products.SingleAsync(product => product.Id == result.Id);

        Assert.Equal("Starter Product", result.Name);
        Assert.Equal(19.95m, result.Price);
        Assert.True(result.IsActive);
        Assert.Equal(result.Id, persisted.Id);
        Assert.False(persisted.IsDeleted);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task GetPagedAsync_FiltersBySearchAndReturnsPaginationMetadata()
    {
        await productService.CreateAsync(new CreateProductCommand { Name = "Blue Notebook", Price = 5m });
        await productService.CreateAsync(new CreateProductCommand { Name = "Red Pen", Price = 2m });
        await productService.CreateAsync(new CreateProductCommand { Name = "Blue Folder", Price = 7m });

        var result = await productService.GetPagedAsync(new ProductListQuery
        {
            SearchValue = "blue",
            PageSize = 1,
            PageNumber = 1
        });

        Assert.Equal(2, result.TotalCount);
        Assert.Single(result.Items);
        Assert.Equal(1, result.PageNumber);
        Assert.Equal(1, result.PageSize);
        Assert.Equal(2, result.TotalPages);
        Assert.True(result.HasNextPage);
        Assert.False(result.HasPreviousPage);
        Assert.Contains("Blue", result.Items[0].Name);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateAsync_ChangesProductFields()
    {
        var created = await productService.CreateAsync(new CreateProductCommand
        {
            Name = "Old Name",
            Description = "Old description",
            Price = 10m
        });

        var updated = await productService.UpdateAsync(created.Id, new UpdateProductCommand
        {
            Name = "New Name",
            Description = "New description",
            Price = 12.5m,
            IsActive = false
        });

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated.Name);
        Assert.Equal("New description", updated.Description);
        Assert.Equal(12.5m, updated.Price);
        Assert.False(updated.IsActive);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task DeleteAsync_SoftDeletesProductAndHidesItFromQueries()
    {
        var created = await productService.CreateAsync(new CreateProductCommand
        {
            Name = "Disposable Product",
            Price = 1m
        });

        var deleted = await productService.DeleteAsync(created.Id);
        var result = await productService.GetByIdAsync(created.Id);
        var persisted = await context.Products
            .IgnoreQueryFilters()
            .SingleAsync(product => product.Id == created.Id);

        Assert.True(deleted);
        Assert.Null(result);
        Assert.True(persisted.IsDeleted);
        Assert.False(persisted.IsActive);
    }

    [Fact]
    [Trait("Category", "Integration")]
    public async Task UpdateAndDelete_ReturnFalseForMissingProduct()
    {
        var missingId = Guid.NewGuid();

        var updated = await productService.UpdateAsync(missingId, new UpdateProductCommand
        {
            Name = "Missing",
            Price = 1m,
            IsActive = true
        });
        var deleted = await productService.DeleteAsync(missingId);

        Assert.Null(updated);
        Assert.False(deleted);
    }

    public async Task DisposeAsync()
    {
        await context.DisposeAsync();
        await postgres.DisposeAsync();
    }
}
