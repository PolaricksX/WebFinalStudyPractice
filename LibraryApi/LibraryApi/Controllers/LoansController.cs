using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/loans")]
public sealed class LoansController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(
        int? patronId,
        bool overdueOnly = false,
        CancellationToken token = default)
    {
        var now = DateTime.UtcNow;
        IQueryable<Loan> query = db.Loans;
        if (patronId is int selectedPatron)
            query = query.Where(loan => loan.PatronId == selectedPatron);

        if (overdueOnly)
            query = query.Where(loan => loan.ReturnedAt == null && loan.DueAt < now);

        var loans = await Project(
                query.OrderByDescending(loan => loan.CheckedOutAt).ThenByDescending(loan => loan.Id),
                now)
            .ToListAsync(token);

        return Ok(loans);
    }

    [HttpGet("overdue-summary")]
    public async Task<ActionResult> OverdueSummary(CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var rows = await db.Loans
            .Where(loan => loan.ReturnedAt == null && loan.DueAt < now)
            .OrderBy(loan => loan.DueAt)
            .ThenBy(loan => loan.Id)
            .Join(db.Copies, loan => loan.CopyId, copy => copy.Id, (loan, copy) => new { loan, copy })
            .Join(db.Books, row => row.copy.BookId, book => book.Id, (row, book) => new { row.loan, row.copy, book })
            .Join(
                db.Patrons,
                row => row.loan.PatronId,
                patron => patron.Id,
                (row, patron) => new LoanResponse(
                    row.loan.Id,
                    patron.Id,
                    patron.FullName,
                    row.copy.Id,
                    row.copy.Barcode,
                    row.book.Id,
                    row.book.Title,
                    new DateTimeOffset(row.loan.CheckedOutAt, TimeSpan.Zero),
                    new DateTimeOffset(row.loan.DueAt, TimeSpan.Zero),
                    row.loan.ReturnedAt == null
                        ? null
                        : new DateTimeOffset(row.loan.ReturnedAt.Value, TimeSpan.Zero),
                    true))
            .ToListAsync(token);

        return Ok(rows);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult> Get(int id, CancellationToken token)
    {
        var now = DateTime.UtcNow;
        var loan = await Project(db.Loans.Where(item => item.Id == id), now).SingleOrDefaultAsync(token);
        return loan is null
            ? ApiProblems.Make(this, StatusCodes.Status404NotFound, "Loan not found.")
            : Ok(loan);
    }

    [HttpPost]
    public async Task<ActionResult> Checkout(CheckoutRequest request, CancellationToken token)
    {
        var patron = await db.Patrons.FirstOrDefaultAsync(item => item.Id == request.PatronId, token);
        if (patron is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Patron not found.");

        if (!patron.IsActive)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Patron is not active.");

        var hasUnpaidFine = await db.Fines.AnyAsync(
            fine => !fine.IsPaid && fine.Loan.PatronId == patron.Id,
            token);
        if (hasUnpaidFine)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Patron has unpaid fines.");

        if (!await db.Books.AnyAsync(book => book.Id == request.BookId, token))
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Book not found.");

        var copy = await db.Copies
            .Where(item => item.BookId == request.BookId && item.Status == CopyStatus.Available)
            .OrderBy(item => item.Barcode)
            .FirstOrDefaultAsync(token);
        if (copy is null)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "No available copy.");

        var now = DateTime.UtcNow;
        var loan = new Loan
        {
            Copy = copy,
            Patron = patron,
            CheckedOutAt = now,
            DueAt = now.AddDays(14)
        };
        copy.Status = CopyStatus.OnLoan;
        db.Loans.Add(loan);
        await db.SaveChangesAsync(token);

        var created = await Project(db.Loans.Where(item => item.Id == loan.Id), now).SingleAsync(token);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    [HttpPost("{id:int}/return")]
    public async Task<ActionResult> Return(int id, CancellationToken token)
    {
        var loan = await db.Loans
            .Include(item => item.Copy)
            .FirstOrDefaultAsync(item => item.Id == id, token);

        if (loan is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Loan not found.");

        if (loan.ReturnedAt is not null)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Loan is already returned.");

        var returnedAt = DateTime.UtcNow;
        loan.ReturnedAt = returnedAt;
        loan.Copy.Status = CopyStatus.Available;

        Fine? fine = null;
        if (returnedAt > loan.DueAt)
        {
            var days = (int)Math.Ceiling((returnedAt - loan.DueAt).TotalDays);
            if (days < 1)
                days = 1;

            fine = new Fine
            {
                Loan = loan,
                Amount = days * 0.50m,
                IsPaid = false,
                Reason = "Overdue"
            };
            db.Fines.Add(fine);
        }

        await db.SaveChangesAsync(token);

        var response = await Project(db.Loans.Where(item => item.Id == loan.Id), returnedAt).SingleAsync(token);
        FineResponse? fineResponse = fine is null
            ? null
            : new FineResponse(
                fine.Id,
                loan.Id,
                loan.PatronId,
                response.PatronName,
                fine.Amount,
                fine.IsPaid,
                fine.Reason);

        return Ok(new ReturnLoanResponse(response, fineResponse));
    }

    private static IQueryable<LoanResponse> Project(IQueryable<Loan> loans, DateTime now) =>
        loans.Select(loan => new LoanResponse(
            loan.Id,
            loan.PatronId,
            loan.Patron.FullName,
            loan.CopyId,
            loan.Copy.Barcode,
            loan.Copy.BookId,
            loan.Copy.Book.Title,
            new DateTimeOffset(loan.CheckedOutAt, TimeSpan.Zero),
            new DateTimeOffset(loan.DueAt, TimeSpan.Zero),
            loan.ReturnedAt == null ? null : new DateTimeOffset(loan.ReturnedAt.Value, TimeSpan.Zero),
            loan.ReturnedAt == null && loan.DueAt < now));
}
