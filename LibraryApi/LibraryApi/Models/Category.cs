namespace LibraryApi.Models;

public sealed class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public List<Book> Books { get; set; } = [];
}
