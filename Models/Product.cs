using System.ComponentModel.DataAnnotations;

namespace InterviewLab.Models;

public sealed class Product
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Range(0.01, 1_000_000)] public decimal Price { get; set; }
    [Range(0, 1_000_000)] public int Stock { get; set; }
    public int CategoryId { get; set; }
    public Category? Category { get; set; }
}

public sealed class Category
{
    public int Id { get; set; }
    [Required, StringLength(80)] public string Name { get; set; } = "";
    public List<Product> Products { get; set; } = [];
}

// C# Q13, Q14, Q40: immutable record DTO; record with-expressions are demonstrated in LearningController.
public sealed record ProductDto(int Id, string Name, decimal Price, int Stock, string Category);

// .NET Core & Web API Q9, Q13, Q64: request DTO whose annotations drive model validation.
public sealed record CreateProductRequest(
    [property: Required, StringLength(100, MinimumLength = 2)] string Name,
    [property: Range(0.01, 1_000_000)] decimal Price,
    [property: Range(0, 1_000_000)] int Stock,
    [property: Range(1, int.MaxValue)] int CategoryId);
