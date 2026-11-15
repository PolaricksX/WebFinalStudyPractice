using LibraryApi.Contracts;
using LibraryApi.Data;
using LibraryApi.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Controllers;

[ApiController]
[Route("api/fines")]
public sealed class FinesController(LibraryDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult> List(bool unpaidOnly = false, CancellationToken token = default)
    {

        IQueryable<Fine> query = db.Fines.AsNoTracking();

        if (unpaidOnly)
            query = query.Where(query => !query.IsPaid);

        var response = query
            .OrderBy(fine => fine.Loan.Patron.FullName)
            .ThenBy(fine => fine.Id)
            .Select(fine => new FineResponse(
                fine.Id,
                fine.LoanId,
                fine.Loan.PatronId,
                fine.Loan.Patron.FullName,
                fine.Amount,
                fine.IsPaid,
                fine.Reason))
            .ToList();

        return Ok(response);
    }

    [HttpGet("totals")]
    public async Task<ActionResult> Totals(CancellationToken token)
    {
        var totals = await db.Fines
            .AsNoTracking()
            .Where(fine => !fine.IsPaid)
            .GroupBy(fine => new { fine.Loan.PatronId, fine.Loan.Patron.FullName })
            .Select(group => new FineTotalResponse(
                group.Key.PatronId,
                group.Key.FullName,
                group.Sum(fine => fine.Amount)))
            .OrderBy(total => total.PatronName)
            .ToListAsync(token);

        return Ok(totals);
    }

    [HttpPost("{id:int}/pay")]
    public async Task<ActionResult> Pay(int id, CancellationToken token)
    {
        var fine = await db.Fines.FirstOrDefaultAsync(item => item.Id == id, token);
        if (fine is null)
            return ApiProblems.Make(this, StatusCodes.Status404NotFound, "Fine not found.");

        if (fine.IsPaid)
            return ApiProblems.Make(this, StatusCodes.Status409Conflict, "Fine is already paid.");

        fine.IsPaid = true;
        await db.SaveChangesAsync(token);
        return NoContent();
    }
}
