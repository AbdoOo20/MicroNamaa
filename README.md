# MicroNamaa – Backend

POS and inventory system for merchants in Libya (Micro Tech / نماء), sold as one platform to many stores.

- .NET 10, ASP.NET Core, SQL Server, EF Core, Identity + JWT, SignalR
- `docs/Architecture.md` – how it works · `docs/API.md` – endpoints for the frontend · `CLAUDE.md` – rules for working on the code

```bash
dotnet build MicroNamaa.slnx
dotnet run --project src/MicroNamaa.Api   # Development: migrates the database and seeds test data
```

Set `ConnectionStrings:DefaultConnection` in `src/MicroNamaa.Api/appsettings.json` (or an environment variable) before running.
Swagger: `http://localhost:5290/swagger`.

Test accounts (Development): admin `admin@micronamaa.test` / `Admin@12345`, owner `0910000001` / `Test@12345`, cashier `0910000002` / `Test@12345`.
