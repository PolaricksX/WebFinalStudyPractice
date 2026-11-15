namespace LibraryApi.Models;

public sealed class Patron
{
    public int Id { get; set; }
    public string StudentNumber { get; set; } = "";
    public string FullName { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsActive { get; set; } = true;
    public List<Loan> Loans { get; set; } = [];
}
