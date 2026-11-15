using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/patrons")]
public sealed class PatronsController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(bool activeOnly = false, CancellationToken token = default)
    {
        IQueryable<Patron> query = db.Patrons;
        if (activeOnly)
            query = query.Where(patron => patron.IsActive);

        var patrons = await query
            .OrderBy(patron => patron.FullName)
            .ThenBy(patron => patron.Id)
            .Select(patron => new PatronResponse(
                patron.Id,
                patron.StudentNumber,
                patron.FullName,
                patron.Email,
                patron.IsActive,
                patron.Loans.Count(loan => loan.ReturnedAt == null),
                patron.Loans.SelectMany(loan => loan.Fines).Where(fine => !fine.IsPaid).Sum(fine => (decimal?)fine.Amount) ?? 0m))
            .ToListAsync(token);

        return Ok(patrons);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken token)
    {
        var patron = await db.Patrons
            .Include(item => item.Loans)
            .ThenInclude(loan => loan.Fines)
            .FirstOrDefaultAsync(item => item.Id == id, token);

        if (patron is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Patron not found.");

        var openLoans = patron.Loans.Count(loan => loan.ReturnedAt is null);
        var unpaid = patron.Loans.SelectMany(loan => loan.Fines).Where(fine => !fine.IsPaid).Sum(fine => fine.Amount);
        return Ok(new PatronResponse(
            patron.Id,
            patron.StudentNumber,
            patron.FullName,
            patron.Email,
            patron.IsActive,
            openLoans,
            unpaid));
    }

    [HttpPost]
    public async Task<ActionResult> Create(UpsertPatronRequest request, CancellationToken token)
    {
        if (await ValidateAsync(request, ignorePatronId: null, token) is { } problem)
            return problem;

        var patron = new Patron
        {
            StudentNumber = request.StudentNumber.Trim(),
            FullName = request.FullName.Trim(),
            Email = request.Email.Trim(),
            IsActive = request.IsActive
        };
        db.Patrons.Add(patron);
        await db.SaveChangesAsync(token);

        var response = new PatronResponse(
            patron.Id,
            patron.StudentNumber,
            patron.FullName,
            patron.Email,
            patron.IsActive,
            0,
            0m);
        return CreatedAtAction(nameof(Get), new { id = patron.Id }, response);
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult> Update(int id, UpsertPatronRequest request, CancellationToken token)
    {
        var patron = await db.Patrons.FirstOrDefaultAsync(item => item.Id == id, token);
        if (patron is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Patron not found.");

        if (await ValidateAsync(request, id, token) is { } problem)
            return problem;

        patron.StudentNumber = request.StudentNumber.Trim();
        patron.FullName = request.FullName.Trim();
        patron.Email = request.Email.Trim();
        patron.IsActive = request.IsActive;
        await db.SaveChangesAsync(token);
        return NoContent();
    }

    private async Task<ActionResult?> ValidateAsync(
        UpsertPatronRequest request,
        int? ignorePatronId,
        CancellationToken token)
    {
        var error = RequestRules.Validate(
            ("Student number", request.StudentNumber, 16),
            ("Name", request.FullName, 120),
            ("Email", request.Email, 200));
        if (error is not null)
            return ApiProblems.Make(this, StatusCodes.Status400BadRequest, error);

        var studentNumber = request.StudentNumber.Trim();
        var email = request.Email.Trim();
        var studentNumberTaken = ignorePatronId is null
            ? await db.Patrons.AnyAsync(patron => patron.StudentNumber == studentNumber, token)
            : await db.Patrons.AnyAsync(
                patron => patron.Id != ignorePatronId && patron.StudentNumber == studentNumber,
                token);
        if (studentNumberTaken)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Student number already exists.");

        var emailTaken = ignorePatronId is null
            ? await db.Patrons.AnyAsync(patron => patron.Email == email, token)
            : await db.Patrons.AnyAsync(
                patron => patron.Id != ignorePatronId && patron.Email == email,
                token);
        if (emailTaken)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Email already exists.");

        return null;
    }
}
