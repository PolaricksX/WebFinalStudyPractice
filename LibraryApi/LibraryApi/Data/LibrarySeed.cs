using LibraryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Data;

public static class LibrarySeed
{
    public static async Task ApplyAsync(LibraryDbContext db)
    {
        if (await db.Categories.AnyAsync())
            return;

        var programming = new Category { Name = "Programming" };
        var fiction = new Category { Name = "Fiction" };

        var cleanCode = new Book
        {
            Title = "Clean Code",
            Isbn = "9780132350884",
            PublishedYear = 2008,
            Category = programming
        };
        var hobbit = new Book
        {
            Title = "The Hobbit",
            Isbn = "9780547928227",
            PublishedYear = 1937,
            Category = fiction
        };
        var refactoring = new Book
        {
            Title = "Refactoring",
            Isbn = "9780134757599",
            PublishedYear = 2018,
            Category = programming
        };

        var cleanCodeAvailable = new Copy { Barcode = "CC-001", Status = CopyStatus.Available, Book = cleanCode };
        var cleanCodeOnLoan = new Copy { Barcode = "CC-002", Status = CopyStatus.OnLoan, Book = cleanCode };
        var hobbitAvailable = new Copy { Barcode = "HB-001", Status = CopyStatus.Available, Book = hobbit };
        var refactoringAvailable = new Copy { Barcode = "RF-001", Status = CopyStatus.Available, Book = refactoring };

        var ada = new Patron
        {
            StudentNumber = "S1001",
            FullName = "Ada Lovelace",
            Email = "ada@students.example.edu",
            IsActive = true
        };
        var grace = new Patron
        {
            StudentNumber = "S1002",
            FullName = "Grace Hopper",
            Email = "grace@students.example.edu",
            IsActive = true
        };
        var alan = new Patron
        {
            StudentNumber = "S1003",
            FullName = "Alan Turing",
            Email = "alan@students.example.edu",
            IsActive = false
        };

        var now = DateTime.UtcNow;
        var adaLoan = new Loan
        {
            Copy = cleanCodeOnLoan,
            Patron = ada,
            CheckedOutAt = now.AddDays(-20),
            DueAt = now.AddDays(-6)
        };
        var graceLoan = new Loan
        {
            Copy = refactoringAvailable,
            Patron = grace,
            CheckedOutAt = now.AddDays(-10),
            DueAt = now.AddDays(-3),
            ReturnedAt = now.AddDays(-1)
        };
        var graceFine = new Fine
        {
            Loan = graceLoan,
            Amount = 1.00m,
            IsPaid = false,
            Reason = "Overdue"
        };

        db.AddRange(
            programming,
            fiction,
            cleanCode,
            hobbit,
            refactoring,
            cleanCodeAvailable,
            cleanCodeOnLoan,
            hobbitAvailable,
            refactoringAvailable,
            ada,
            grace,
            alan,
            adaLoan,
            graceLoan,
            graceFine);

        await db.SaveChangesAsync();
    }
}
