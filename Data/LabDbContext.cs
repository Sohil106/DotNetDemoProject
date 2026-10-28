using InterviewLab.Models;
using Microsoft.EntityFrameworkCore;

namespace InterviewLab.Data;

public sealed class LabDbContext(DbContextOptions<LabDbContext> options) : DbContext(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // EF Core Q184, Q199, Q206: relationship, Fluent API mapping, index/precision configuration, and seed data.
        modelBuilder.Entity<Product>().HasIndex(p => p.Name);
        modelBuilder.Entity<Product>().Property(p => p.Price).HasPrecision(10, 2);
        modelBuilder.Entity<Product>().HasOne(p => p.Category).WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId).OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Category>().HasData(
            new Category { Id = 1, Name = "Books" },
            new Category { Id = 2, Name = "Hardware" });
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = 1, Name = "C# in Depth", Price = 42.50m, CategoryId = 1, Stock = 12 },
            new Product { Id = 2, Name = "Mechanical Keyboard", Price = 89.99m, CategoryId = 2, Stock = 8 },
            new Product { Id = 3, Name = "ASP.NET Core Guide", Price = 35m, CategoryId = 1, Stock = 20 });
    }
}

public static class DemoSeed
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        // DI Q317: create a scope when resolving a scoped DbContext during app startup.
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LabDbContext>();
        await db.Database.EnsureCreatedAsync();
    }
}
