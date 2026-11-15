namespace LibraryApi.Contracts;

public sealed record PatronResponse(
    int Id,
    string StudentNumber,
    string FullName,
    string Email,
    bool IsActive,
    int OpenLoanCount,
    decimal UnpaidFineTotal);

public sealed record UpsertPatronRequest
{
    public string StudentNumber { get; init; } = "";
    public string FullName { get; init; } = "";
    public string Email { get; init; } = "";
    public bool IsActive { get; init; } = true;
}

public sealed record LoanResponse(
    int Id,
    int PatronId,
    string PatronName,
    int CopyId,
    string Barcode,
    int BookId,
    string BookTitle,
    DateTimeOffset CheckedOutAt,
    DateTimeOffset DueAt,
    DateTimeOffset? ReturnedAt,
    bool IsOverdue);

public sealed record CheckoutRequest
{
    public int PatronId { get; init; }
    public int BookId { get; init; }
}

public sealed record ReturnLoanResponse(LoanResponse Loan, FineResponse? Fine);

public sealed record FineResponse(
    int Id,
    int LoanId,
    int PatronId,
    string PatronName,
    decimal Amount,
    bool IsPaid,
    string Reason);

public sealed record FineTotalResponse(int PatronId, string PatronName, decimal UnpaidTotal);
