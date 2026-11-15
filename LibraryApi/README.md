# LibraryApi starter

Controller-based Web API for a campus library. MVC controllers use EF Core and LINQ directly. There is no authentication, repository, service, or Unit of Work. This project is the starting point for the Test Review practical in `docs/test-review/library-api-test.mdx`. The same starter is packaged as [LibraryApi-Starter.zip](../../../static/files/test-review/LibraryApi-Starter.zip).

The database has six tables: `Categories`, `Books`, `Copies`, `Patrons`, `Loans`, and `Fines`.

## Run

From this directory:

```powershell
dotnet run --project LibraryApi
```

The API listens on `http://localhost:5090`. SQLite creates `LibraryApi/library.db` on startup with `EnsureCreatedAsync` and seeds three patrons, three books, and one overdue loan. Sample requests are in `LibraryApi/LibraryApi.http`. In Development, the OpenAPI document is at `http://localhost:5090/openapi/v1.json`.

`EnsureCreated` does not change an existing file when the model changes. Delete only this local `library.db` before running again after a model edit. The practical test replaces this with migrations.

## Seeded circulation rules you can try

- Ada Lovelace (`S1001`) is active and has an overdue copy of Clean Code (`CC-002`).
- Grace Hopper (`S1002`) is active and has an unpaid overdue fine, so checkout is rejected.
- Alan Turing (`S1003`) is inactive, so checkout is rejected.
- The Hobbit (`HB-001`) is available. Checkout selects the available copy with the lowest barcode and sets the due date 14 days ahead.
- A late return adds a fine of 0.50 for each started day past the due date and sets the copy back to `Available`. An open overdue loan does not create a fine until the copy is returned.
