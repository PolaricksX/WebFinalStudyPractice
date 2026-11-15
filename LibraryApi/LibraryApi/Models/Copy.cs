namespace LibraryApi.Models;

public sealed class Copy
{
    public int Id { get; set; }
    public string Barcode { get; set; } = "";
    public string Status { get; set; } = CopyStatus.Available;
    public int BookId { get; set; }
    public Book Book { get; set; } = null!;
    public List<Loan> Loans { get; set; } = [];
}
