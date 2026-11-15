using LibraryApi.Contracts;
using LibraryApi.Data;
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


        var fines = await db.Fines
            .Include(fine => fine.Loan)
            .ThenInclude(loan => loan.Patron)
            .ToListAsync(token);

        if (unpaidOnly)
            fines = fines.Where(fine => !fine.IsPaid).ToList();

        var response = fines
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
        var fines = await db.Fines
            .Include(fine => fine.Loan)
            .ThenInclude(loan => loan.Patron)
            .ToListAsync(token);

        var totals = fines
            .Where(fine => !fine.IsPaid)
            .GroupBy(fine => new { fine.Loan.PatronId, fine.Loan.Patron.FullName })
            .Select(group => new FineTotalResponse(
                group.Key.PatronId,
                group.Key.FullName,
                group.Sum(fine => fine.Amount)))
            .OrderBy(total => total.PatronName)
            .ToList();

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
