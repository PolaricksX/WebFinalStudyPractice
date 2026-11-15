namespace LibraryApi.Contracts;

public sealed record CategoryResponse(int Id, string Name, int BookCount);

public sealed record UpsertCategoryRequest
{
    public string Name { get; init; } = "";
}

public sealed record BookListItemResponse(
    int Id,
    string Title,
    string Isbn,
    int PublishedYear,
    int CategoryId,
    string CategoryName,
    int AvailableCopies);

public sealed record BookGroupResponse(string CategoryName, int BookCount);

public sealed record BookRequest
{
    public string Title { get; init; } = "";
    public string Isbn { get; init; } = "";
    public int PublishedYear { get; init; }
    public int CategoryId { get; init; }
}

public sealed record CopyResponse(int Id, int BookId, string Barcode, string Status);

public sealed record CreateCopyRequest
{
    public string Barcode { get; init; } = "";
}
