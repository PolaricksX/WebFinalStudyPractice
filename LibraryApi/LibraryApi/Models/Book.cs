namespace LibraryApi.Models;

public sealed class Book
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Isbn { get; set; } = "";
    public int PublishedYear { get; set; }
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public List<Copy> Copies { get; set; } = [];
}
