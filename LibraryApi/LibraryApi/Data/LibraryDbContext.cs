using LibraryApi.Models;
using Microsoft.EntityFrameworkCore;

namespace LibraryApi.Data;

public sealed class LibraryDbContext(DbContextOptions<LibraryDbContext> options) : DbContext(options)
{
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Book> Books => Set<Book>();
    public DbSet<Copy> Copies => Set<Copy>();
    public DbSet<Patron> Patrons => Set<Patron>();
    public DbSet<Loan> Loans => Set<Loan>();
    public DbSet<Fine> Fines => Set<Fine>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Category>().Property(category => category.Name).HasMaxLength(80);
        modelBuilder.Entity<Category>().HasIndex(category => category.Name).IsUnique();

        modelBuilder.Entity<Book>().Property(book => book.Title).HasMaxLength(200);
        modelBuilder.Entity<Book>().Property(book => book.Isbn).HasMaxLength(20);
        modelBuilder.Entity<Book>().HasIndex(book => book.Isbn).IsUnique();

        modelBuilder.Entity<Copy>().Property(copy => copy.Barcode).HasMaxLength(32);
        modelBuilder.Entity<Copy>().Property(copy => copy.Status).HasMaxLength(20);
        modelBuilder.Entity<Copy>().HasIndex(copy => copy.Barcode).IsUnique();

        modelBuilder.Entity<Patron>().Property(patron => patron.StudentNumber).HasMaxLength(16);
        modelBuilder.Entity<Patron>().Property(patron => patron.FullName).HasMaxLength(120);
        modelBuilder.Entity<Patron>().Property(patron => patron.Email).HasMaxLength(200);
        modelBuilder.Entity<Patron>().HasIndex(patron => patron.StudentNumber).IsUnique();
        modelBuilder.Entity<Patron>().HasIndex(patron => patron.Email).IsUnique();

        modelBuilder.Entity<Fine>().Property(fine => fine.Reason).HasMaxLength(200);
        modelBuilder.Entity<Fine>().Property(fine => fine.Amount).HasPrecision(10, 2);

        modelBuilder.Entity<Book>()
            .HasOne(book => book.Category)
            .WithMany(category => category.Books)
            .HasForeignKey(book => book.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Copy>()
            .HasOne(copy => copy.Book)
            .WithMany(book => book.Copies)
            .HasForeignKey(copy => copy.BookId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Loan>()
            .HasOne(loan => loan.Copy)
            .WithMany(copy => copy.Loans)
            .HasForeignKey(loan => loan.CopyId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Loan>()
            .HasOne(loan => loan.Patron)
            .WithMany(patron => patron.Loans)
            .HasForeignKey(loan => loan.PatronId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Fine>()
            .HasOne(fine => fine.Loan)
            .WithMany(loan => loan.Fines)
            .HasForeignKey(fine => fine.LoanId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
