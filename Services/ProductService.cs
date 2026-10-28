using InterviewLab.Data;
using InterviewLab.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewLab.Services;

public interface IProductService
{
    // OOP & SOLID Q90, Q92, Q93: controller depends on an interface; EF Core work stays in the service.
    Task<(IReadOnlyList<ProductDto> Items, int Total)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct);
    Task<ProductDto?> GetAsync(int id, CancellationToken ct);
    Task<ProductDto?> CreateAsync(CreateProductRequest request, CancellationToken ct);
}

public sealed class ProductService(LabDbContext db) : IProductService
{
    public async Task<(IReadOnlyList<ProductDto> Items, int Total)> SearchAsync(string? term, int page, int pageSize, CancellationToken ct)
    {
        // LINQ Q224, Q232; EF Core Q194, Q204, Q216: compose IQueryable, avoid tracking, project to DTO, then page in SQL.
        IQueryable<Product> query = db.Products.AsNoTracking().Include(p => p.Category);
        if (!string.IsNullOrWhiteSpace(term)) query = query.Where(p => p.Name.Contains(term));
        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(p => p.Id).Skip((page - 1) * pageSize).Take(pageSize)
            .Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock, p.Category!.Name)).ToListAsync(ct);
        return (items, total);
    }

    public async Task<ProductDto?> GetAsync(int id, CancellationToken ct) => await db.Products.AsNoTracking()
        .Where(p => p.Id == id).Select(p => new ProductDto(p.Id, p.Name, p.Price, p.Stock, p.Category!.Name))
        .SingleOrDefaultAsync(ct);

    public async Task<ProductDto?> CreateAsync(CreateProductRequest request, CancellationToken ct)
    {
        // Async Q299, Q301: async EF operations accept and propagate request cancellation.
        if (!await db.Categories.AnyAsync(c => c.Id == request.CategoryId, ct)) return null;
        var product = new Product { Name = request.Name, Price = request.Price, Stock = request.Stock, CategoryId = request.CategoryId };
        db.Products.Add(product);
        await db.SaveChangesAsync(ct);
        var category = await db.Categories.FindAsync([product.CategoryId], ct);
        return new(product.Id, product.Name, product.Price, product.Stock, category!.Name);
    }
}
