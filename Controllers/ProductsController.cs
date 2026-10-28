using InterviewLab.Models;
using InterviewLab.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InterviewLab.Controllers;

[ApiController, Route("api/products")]
public sealed class ProductsController(IProductService products) : ControllerBase
{
    // .NET Core & Web API Q7, Q8, Q9, Q13, Q27, Q64: REST routes, binding, validation, paging, and validation errors.
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] string? q, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, CancellationToken ct = default)
    {
        if (page < 1 || pageSize is < 1 or > 100)
            return ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["pagination"] = ["page must be >= 1; pageSize must be from 1 to 100."] }));
        var (items, total) = await products.SearchAsync(q, page, pageSize, ct);
        return Ok(new { items, page, pageSize, total, totalPages = (int)Math.Ceiling(total / (double)pageSize) });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> Get([FromRoute] int id, CancellationToken ct) =>
        await products.GetAsync(id, ct) is { } item ? Ok(item) : NotFound();

    [HttpPost, Authorize(Policy = "Readers")]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request, CancellationToken ct)
    {
        var product = await products.CreateAsync(request, ct);
        return product is null ? ValidationProblem(new ValidationProblemDetails(new Dictionary<string, string[]> { ["categoryId"] = ["Category does not exist."] }))
            : CreatedAtAction(nameof(Get), new { id = product.Id }, product);
    }
}
