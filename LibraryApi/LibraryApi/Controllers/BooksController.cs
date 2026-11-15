using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/books")]
public sealed class BooksController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(
        string? title,
        int? categoryId,
        int page = 1,
        int pageSize = 10,
        CancellationToken token = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize < 1 ? 10 : Math.Min(pageSize, 50);

        IQueryable<Book> query = db.Books;
        if (!string.IsNullOrWhiteSpace(title))
        {
            var term = title.Trim();
            query = query.Where(book => book.Title.Contains(term));
        }

        if (categoryId is int selectedCategory)
            query = query.Where(book => book.CategoryId == selectedCategory);

        var books = await query
            .OrderBy(book => book.Title)
            .ThenBy(book => book.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(book => new BookListItemResponse(
                book.Id,
                book.Title,
                book.Isbn,
                book.PublishedYear,
                book.CategoryId,
                book.Category.Name,
                book.Copies.Count(copy => copy.Status == CopyStatus.Available)))
            .ToListAsync(token);

        return Ok(books);
    }

    [HttpGet("by-category")]
    public async Task<ActionResult> ByCategory(CancellationToken token)
    {
        var groups = await (
            from book in db.Books
            join category in db.Categories on book.CategoryId equals category.Id
            group book by category.Name into books
            orderby books.Key
            select new BookGroupResponse(books.Key, books.Count()))
            .ToListAsync(token);

        return Ok(groups);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken token)
    {
        var book = await db.Books
            .AsNoTracking()
            .Where(item => item.Id == id)
            .Select(item => new BookListItemResponse(
             item.Id,
             item.Title,
             item.Isbn,
             item.PublishedYear,
             item.CategoryId,
             item.Category.Name,
             item.Copies.Count(copy => copy.Status == CopyStatus.Available)))
        .FirstOrDefaultAsync(token);

        return book is null
                ? ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.")
                : Ok(book);
    }

    [HttpPost]
    public async Task<ActionResult> Create(BookRequest request, CancellationToken token)
    {
        if (await ValidateAsync(request, ignoreBookId: null, token) is { } problem)
            return problem;

        var book = new Book
        {
            Title = request.Title.Trim(),
            Isbn = request.Isbn.Trim(),
            PublishedYear = request.PublishedYear,
            CategoryId = request.CategoryId
        };
        db.Books.Add(book);
        await db.SaveChangesAsync(token);

        var created = await db.Books
            .Where(item => item.Id == book.Id)
            .Select(item => new BookListItemResponse(
                item.Id,
                item.Title,
                item.Isbn,
                item.PublishedYear,
                item.CategoryId,
                item.Category.Name,
                0))
            .SingleAsync(token);

        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, BookRequest request, CancellationToken token)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Id == id, token);
        if (book is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.");

        if (await ValidateAsync(request, id, token) is { } problem)
            return problem;

        book.Title = request.Title.Trim();
        book.Isbn = request.Isbn.Trim();
        book.PublishedYear = request.PublishedYear;
        book.CategoryId = request.CategoryId;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult> Delete(int id, CancellationToken token)
    {
        var book = await db.Books.FirstOrDefaultAsync(item => item.Id == id, token);
        if (book is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.");

        if (await db.Copies.AnyAsync(copy => copy.BookId == id, token))
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Book has copies.");

        db.Books.Remove(book);
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(BookRequest request, int? ignoreBookId, CancellationToken token)
    {
        var error = RequestRules.Validate(
            ("Title", request.Title, 200),
            ("ISBN", request.Isbn, 20));
        if (error is not null)
            return ApiProblems.Make(this, StatusCodes.Status400BadRequest, error);

        if (request.PublishedYear < 1450 || request.PublishedYear > DateTime.UtcNow.Year + 1)
            return ApiProblems.Make(this, StatusCodes.Status400BadRequest, "Published year is out of range.");

        if (!await db.Categories.AnyAsync(category => category.Id == request.CategoryId, token))
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Category not found.");

        var isbn = request.Isbn.Trim();
        var duplicate = ignoreBookId is null
            ? await db.Books.AnyAsync(book => book.Isbn == isbn, token)
            : await db.Books.AnyAsync(book => book.Isbn == isbn && book.Id != ignoreBookId, token);
        if (duplicate)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "ISBN already exists.");

        return null;
    }
}
