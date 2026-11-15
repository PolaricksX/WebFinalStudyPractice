using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/books/{bookId:int}/copies")]
public sealed class CopiesController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(int bookId, CancellationToken token)
    {
        if (!await db.Books.AnyAsync(book => book.Id == bookId, token))
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.");

        var copies = await db.Copies
            .Where(copy => copy.BookId == bookId)
            .OrderBy(copy => copy.Barcode)
            .Select(copy => new CopyResponse(copy.Id, copy.BookId, copy.Barcode, copy.Status))
            .ToListAsync(token);

        return Ok(copies);
    }

    [HttpPost]
    public async Task<ActionResult> Create(int bookId, CreateCopyRequest request, CancellationToken token)
    {
        if (!await db.Books.AnyAsync(book => book.Id == bookId, token))
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.");

        var error = RequestRules.Validate(("Barcode", request.Barcode, 32));
        if (error is not null)
            return ApiProblems.Make(this, StatusCodes.Status400BadRequest, error);

        var barcode = request.Barcode.Trim();
        if (await db.Copies.AnyAsync(copy => copy.Barcode == barcode, token))
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Barcode already exists.");

        var copy = new Copy
        {
            BookId = bookId,
            Barcode = barcode,
            Status = CopyStatus.Available
        };
        db.Copies.Add(copy);
        await db.SaveChangesAsync(token);

        var response = new CopyResponse(copy.Id, copy.BookId, copy.Barcode, copy.Status);
        return Created($"/api/books/{bookId}/copies/{copy.Id}", response);
    }

    [HttpDelete("{copyId:int}")]
    public async Task<ActionResult> Delete(int bookId, int copyId, CancellationToken token)
    {
        var copy = await db.Copies.FirstOrDefaultAsync(
            item => item.Id == copyId && item.BookId == bookId,
            token);
        if (copy is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Copy not found.");

        if (await db.Loans.AnyAsync(loan => loan.CopyId == copy.Id, token))
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Copy has loans.");

        db.Copies.Remove(copy);
        await db.SaveChangesAsync(token);
        return NoContent();
    }
}
