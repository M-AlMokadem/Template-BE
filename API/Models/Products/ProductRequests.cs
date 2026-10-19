using System.ComponentModel.DataAnnotations;

namespace API.Models.Products;

public class CreateProductRequest
{
    [Required]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;

    [StringLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "79228162514264337593543950335")]
    public decimal Price { get; set; }
}

public sealed class UpdateProductRequest : CreateProductRequest
{
    public bool IsActive { get; set; } = true;
}
