namespace LibraryApi.Models;

public sealed class Loan
{
    public int Id { get; set; }
    public int CopyId { get; set; }
    public Copy Copy { get; set; } = null!;
    public int PatronId { get; set; }
    public Patron Patron { get; set; } = null!;
    public DateTime CheckedOutAt { get; set; }
    public DateTime DueAt { get; set; }
    public DateTime? ReturnedAt { get; set; }
    public List<Fine> Fines { get; set; } = [];
}
