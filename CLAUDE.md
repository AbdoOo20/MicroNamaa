# MicroNamaa – rules for working on this repo

Backend for Micro Tech / نماء: a POS and inventory system sold to many merchants (SaaS) in Libya. .NET 10, SQL Server,
EF Core, Identity + JWT, SignalR. Same structure and conventions as the Bako project.
Read `docs/Architecture.md` (how it works) and `docs/API.md` (endpoints) first.

## Product rules that must not break
- Every store row implements `IMerchantOwned`. `AppDbContext` filters those rows to the signed-in user's merchant, fills
  `MerchantId` on new rows and refuses to save another merchant's row. Never trust an id from the client: load referenced
  rows through the filtered repositories (a row of another store simply is not found).
- Platform admins (`PlatformAdmin`) have no merchant and see every store; they never use the store endpoints.
  Staff (sub-admins) are admins limited by `StaffPermission`; every admin endpoint needs a `[RequirePermission(area, action)]`
  (or `[OwnerOnly]`), enforced by `PermissionFilter`.
- Only the admin creates merchants (from the admin panel or by approving a `MerchantApplication` from the landing page).
  A merchant is Active or Suspended and has a subscription end date; `MerchantAccessService` locks out its users otherwise.
- Store roles: `MerchantOwner` (everything, settings, owners and managers), `MerchantManager` (the store, cashiers only),
  `Cashier` (POS of their own branch, their own invoices, customers). A store always keeps one active owner.
- Currency is LYD for everyone (`Money`, three decimals). Days and months follow Libyan time (`LibyaTime`, UTC+2); dates are stored in UTC.
- Stock changes only through `StockLedger` (writes a `StockMovement`, refuses negative stock unless the store allows it, sends low/out alerts).
  Customer and supplier balances change only through `BalanceLedger` (writes the statement line).
- A sale is priced only by `SalesPricing`: line promotions, then the order promotion and manual discount, then points, then tax.
  Drafts do not touch stock, balances or points; completing does; cancelling a completed invoice puts everything back.
- Products, warehouses, suppliers and employees with history are archived or turned off, never deleted (their documents must keep showing).
- Document numbers (INV, PO, RET, customer and supplier codes) come from `IDocumentNumberService`, per merchant.
- Offline sync (`SyncService`) runs every pushed operation through the same service as its online endpoint; a new store
  operation that should work offline must be added to `SyncOperations`. Offline sales go through `CreateOfflineAsync` only.
- Notifications are created with `NotificationFactory` and pushed over SignalR by the DbContext after saving.

## Code conventions
- Layers: `Api -> Infrastructure -> Application -> Domain`. Controllers only call a service and `Respond(result)`.
- Services return `Result` / `Result<T>`; responses keep the `{ succeeded, statusCode, message, errors, data }` shape.
- One type per file. DTOs in `Application/DTOs/<Feature>`, services in `Application/Services/<Feature>`.
- Entities use the `MicroNamaa.Domain.Entities` namespace whatever their folder.
- A blank line between statements, between properties, and between `using` directives (same style as Bako).
- File-scoped namespaces, primary constructors, no unused usings. The build must have 0 warnings (`EnforceCodeStyleInBuild`).
- API messages are English; notification titles and bodies shown to users are Arabic.
- Uploads (images only) go through `ImageUploads` / `IFileStorage`; never serve them as static files (`FilesController`).
- `[Authorize]` on a class and on an action are combined with AND: an action can narrow access, never widen it.

## Commands
- Build: `dotnet build MicroNamaa.slnx`
- Run: `dotnet run --project src/MicroNamaa.Api` (Development migrates the database and seeds test data on startup)
- New migration: `dotnet ef migrations add <Name> -p src/MicroNamaa.Infrastructure -s src/MicroNamaa.Api -o Persistence/Migrations`
  (Visual Studio: `Add-Migration <Name>` with default project `MicroNamaa.Infrastructure`, startup project `MicroNamaa.Api`)
