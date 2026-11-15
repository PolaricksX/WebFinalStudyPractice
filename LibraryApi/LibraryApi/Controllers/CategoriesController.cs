using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/categories")]
public sealed class CategoriesController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(CancellationToken token)
    {
        var categories = await db.Categories
            .OrderBy(category => category.Name)
            .Select(category => new CategoryResponse(category.Id, category.Name, category.Books.Count))
            .ToListAsync(token);

        return Ok(categories);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken token)
    {
        var category = await db.Categories
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new CategoryResponse(item.Id, item.Name, item.Books.Count))
            .FirstOrDefaultAsync(token);

        return category is null
            ? ApiProblems.Make(this, StatusCodes.Status404NotFound, "Category not found.")
            : Ok(category);
    }

    [HttpPost]
    public async Task<ActionResult> Create(UpsertCategoryRequest request, CancellationToken token)
    {
        if (TextProblem(request.Name) is { } problem)
            return problem;

        var name = request.Name.Trim();
        if (await db.Categories.AnyAsync(category => category.Name == name, token))
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Category already exists.");

        var category = new Category { Name = name };
        db.Categories.Add(category);
        await db.SaveChangesAsync(token);

        return CreatedAtAction(
            nameof(Get),
            new { id = category.Id },
            new CategoryResponse(category.Id, category.Name, 0));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UpsertCategoryRequest request, CancellationToken token)
    {
        if (TextProblem(request.Name) is { } problem)
            return problem;

        var category = await db.Categories.FirstOrDefaultAsync(item => item.Id == id, token);
        if (category is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Category not found.");

        var name = request.Name.Trim();
        var duplicate = await db.Categories.AnyAsync(
            item => item.Id != id && item.Name == name,
            token);
        if (duplicate)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Category already exists.");

        category.Name = name;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken token)
    {
        var category = await db.Categories.FirstOrDefaultAsync(item => item.Id == id, token);
        if (category is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Category not found.");

        if (await db.Books.AnyAsync(book => book.CategoryId == id, token))
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Category still has books.");

        db.Categories.Remove(category);
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    private ActionResult? TextProblem(string? name)
    {
        var error = RequestRules.Validate(("Name", name, 80));
        return error is null ? null : ApiProblems.Make(this, StatusCodes.Status400BadRequest, error);
    }
}
