# MicroNamaa – Project Structure

MicroNamaa (Micro Tech / نماء) is a POS and inventory system sold to many merchants in Libya. Each merchant has branches,
warehouses, products, customers, suppliers and employees; cashiers sell from the POS, managers buy from suppliers and follow
stock and reports. The platform admin sells the system: creates merchants, manages their subscriptions and answers their tickets.

## Layers (dependencies point inwards)

`Api -> Infrastructure -> Application -> Domain`

| Layer | Responsibility |
|---|---|
| **Domain** | Entities, enums, constants. No dependency on other layers. |
| **Application** | Contracts (`Abstractions`), DTOs, service interfaces and business-logic services, settings. |
| **Infrastructure** | EF Core (`Persistence`), Identity/JWT, document numbers, file storage, seeding. |
| **Api** | Controllers, SignalR hub, authentication, rate limiting, OpenAPI, the background worker, `UserAppService`. |

## Folder map

```
MicroNamaa.Domain
  Common/            BaseEntity<TKey>, IAuditable, ISoftDeletable, ISearchable, IMerchantOwned, ArabicSearch
  Constants/         RoleNames (PlatformAdmin, MerchantOwner, MerchantManager, Cashier), RoleGroups
  Entities/<Area>/   Identity, Merchants, Catalog, Inventory, Parties, Sales, Purchases, Support, System
  Enums/             one enum per file

MicroNamaa.Application
  Abstractions/      IRepository, IUnitOfWork, IUserAppService, IMerchantAccessService, IDocumentNumberService, INotificationPublisher, IFileStorage
  Common/            Result<T>, PagedQuery/PagedResult, LibyanPhone, Money, LibyaTime, DateRange, CsvFile, FileUrls, UploadRules
  DTOs/<Feature>/    request/response records, one type per file
  Services/<Feature>/ I<Feature>Service + implementation
    Inventory/       StockLedger (the only place that changes stock), warehouses, stock, movements
    Parties/         BalanceLedger (customer and supplier statements), customers, suppliers
    Sales/           SalesPricing, POS, invoices, returns
    Purchases/       purchase orders
    Reports/         SalesFigures (shared queries), dashboard, reports
    Merchants/       MerchantContext (current merchant and branch), settings, branches, promotions
    Admin/, MerchantApplications/   the platform admin's side
  Settings/          JwtSettings, PlatformSettings, StorageSettings, SampleDataSettings, DatabaseSettings, PlatformAdminSettings

MicroNamaa.Infrastructure
  Persistence/Configurations/  IEntityTypeConfiguration<T>, one per entity (roles are seeded here)
  Persistence/Repositories/    GenericRepository, UnitOfWork
  Persistence/Migrations/      EF migrations (generated)
  Seeding/                     DatabaseMigrator, PlatformAdminSeeder, SampleDataSeeder
  Services/Auth/               AuthService (Identity + JWT), MerchantAccessService, PasswordReset
  Services/Employees/          EmployeeService (store users through UserManager)
  Services/Merchants/          MerchantProvisioningService (merchant + owner + branch + warehouse + register)
  Services/Numbers/            DocumentNumberService (per-merchant counters)
  Services/Storage/            LocalFileStorage (images, outside wwwroot)

MicroNamaa.Api
  Controllers/       thin controllers: call a service, then Respond(result)
  Authorization/     AppRoles, RequirePermission / OwnerOnly + PermissionFilter (platform staff)
  Hubs/              NotificationsHub + SignalRNotificationPublisher
  BackgroundServices/ MaintenanceWorker (every 5 minutes)
  Extensions/        auth, CORS, rate limiting, response shape, web app
  Services/          UserAppService (reads the JWT claims)
```

## Where does a new service go?

1. Interface + DTOs: `Application/Services/<Feature>` and `Application/DTOs/<Feature>`.
2. Implementation:
   - pure business logic over `IUnitOfWork` -> `Application/Services/<Feature>`
   - needs an external library (Identity, raw SQL, storage) -> `Infrastructure/Services/<Feature>`
   - needs `HttpContext` -> `Api/Services`
3. Register it in `DependencyInjection.cs` of the layer that owns the implementation.

## Request flow and responses

`Controller -> Service (Application) -> IUnitOfWork -> AppDbContext`

- Controllers inherit `ApiControllerBase`, never touch `IUnitOfWork`, and only call a service then `Respond(result)`.
- Every service returns `Result` / `Result<T>`. Every HTTP response has the same JSON shape: `{ succeeded, statusCode, message, errors, data }`.
- Unhandled exceptions, model-validation failures, and JWT 401/403 are returned in the same shape.

## Many merchants in one database (tenancy)

- Every store table carries `MerchantId` (`IMerchantOwned`). `AppDbContext` adds a query filter
  `IsDeleted == false && (CurrentMerchantId == null || MerchantId == CurrentMerchantId)` where `CurrentMerchantId` is the
  `merchant_id` claim of the token. A merchant user therefore never reads another store's rows, and an id from another
  store is simply "not found".
- On save, new rows get the user's merchant, and changing or saving a row of another merchant throws (403).
- Platform admins, anonymous requests (images, the landing-page form) and background jobs have no merchant and see every row.
- Unique values are per merchant (SKU, barcode, category, warehouse and supplier names, document numbers, customer phone).
- `MerchantAccessService` (cached 30 s) blocks every user of a suspended merchant or one whose subscription ended,
  both at login and on every request (`OnTokenValidated`).

## Users and roles

| Role | Who | Can |
|---|---|---|
| `PlatformAdmin` | the system owner and its staff | merchants, subscriptions, sign-up requests, tickets, audit log. Staff are limited by `StaffPermission`. |
| `MerchantOwner` | the merchant | everything in the store, settings, branches, owners and managers, the store's audit log |
| `MerchantManager` | store manager | products, stock, purchases, customers, suppliers, reports; adds and changes cashiers only |
| `Cashier` | sales person | the POS of their own branch, their own invoices, returns in their branch, customers and their payments |

Users sign in with their Libyan mobile (`09xxxxxxxx`, any format) or email + password.

## The sale (POS)

```
cart ──(preview)──> totals
  │
  ├─ save (complete=false) ──> Draft  ──(update)──> Draft ──(complete + payment)──┐
  │                                                                               v
  └─ save (complete=true + payment) ─────────────────────────────────────────> Completed ──(owner/manager)──> Cancelled
                                                                                  │
                                                                                  └──> SalesReturn(s) (partial or full)
```

- Pricing (`SalesPricing`): each line gets its best running product or category promotion; then the chosen order
  promotion (min amount) and the cashier's manual discount; then loyalty points (`PointValue` LYD each); then the tax on what is
  left. `Total = Subtotal - ItemsDiscount - OrderDiscount - PointsDiscount + TaxAmount`. Points earned = floor(Total × PointsPerCurrencyUnit).
- Payment: Cash (tendered ≥ total, change returned), Card, BankTransfer, or Deferred (customer required; the unpaid part goes to
  the customer's balance, checked against their credit limit).
- Completing writes the invoice number, takes the stock from the branch's selling warehouse (`Branch.DefaultWarehouseId`),
  records the cost of goods (product's average cost), changes the customer's points and balance — all in one transaction.
  Document numbers are taken with a locked counter, so parallel registers never clash.
- Cancelling (no returns yet) puts the stock, balance and points back. A return refunds what the customer really paid per unit
  (all discounts and the tax are shared over the lines), puts the goods back unless damaged, reverses part of the earned points,
  and refunds in cash, by card or to the customer's balance.

## Purchases

`Draft -> Pending -> InTransit -> Delivered` (or `Cancelled`). Only delivery changes anything: the goods enter the warehouse,
each product's cost becomes the moving average of what was on the shelf and the new units (discount and shipping shared over the
units, tax left out), and the supplier's balance gets the total minus what was paid on delivery.

## Stock

- `StockItem` holds the quantity of a product in a warehouse; a product's stock is the sum over warehouses.
- `StockLedger` is the only place that changes it and writes a `StockMovement` (type, quantity, balance after, unit cost, reference).
- Stock state is worked out, not stored: Out (≤ 0), Low (≤ product threshold or the store's default), In.
- When a product crosses into Low or Out, the owners and managers get a notification (also shown in the dashboard activity).
- Stock counts (`/inventory/adjustments`) set the counted quantity; transfers move stock between warehouses.

## Money and balances

- LYD with three decimals (`Money.Round`). Customer balance = what the customer owes; supplier balance = what the store owes.
- `BalanceLedger` is the only place that changes a balance and writes the statement line with the balance after it.

## Reports

- A sale counts on the day it was completed, a return on the day it was taken; revenue = completed totals − returns.
- Gross profit = sales without tax − cost of goods sold, corrected by the returns. Days and months follow Libyan time.
- `SalesFigures` holds the shared queries used by the dashboard and the reports.

## Notifications (SignalR)

- Services add `Notification` rows through `NotificationFactory` (`ForUser`, `ForMerchant`, `ForAdmins`).
- `AppDbContext.SaveChangesAsync` pushes every new notification over SignalR after the save succeeds.
- Hub: `/hubs/notifications?access_token=<jwt>`, event `notification`. Groups: `user:{id}`, `merchant:{id}` (owners and managers), `admins`.

## Offline sync (desktop app)

- The device pulls the store's data (`/api/sync/pull`, then only changes with `since`) and works on it offline.
- What it does offline is queued as operations and pushed later (`/api/sync/push`). `SyncService` runs each one through the
  same service as its online endpoint (`SyncOperations` maps type → service call and role), so every rule still applies.
- `SyncOperation` rows remember applied operations: the same `OperationId` is never applied twice, and `ClientKey` → server id
  lets later operations reference offline-created records as `"@key"` (`"@key#n"` = line n of an offline invoice).
- Each operation succeeds or fails alone; the change tracker is cleared between operations.
- An offline sale (`sales.invoice.create` with `occurredAt`) goes through `SalesInvoiceService.CreateOfflineAsync`: the device's
  prices, the promotions of that moment, `CompletedAt` = when it happened, and no refusal for stock, credit limit or points.
  `SalesInvoice.ClientKey` keeps the device's key (unique per merchant).
- CORS: `Cors:AllowAnyOrigin` lets the desktop app call from `file://`, `app://` or `tauri://` origins (bearer tokens, no cookies).

## Background worker

`MaintenanceWorker` runs `MaintenanceService` every 5 minutes: reminds a merchant (and the admins) once,
`Platform:SubscriptionReminderDays` days before the subscription ends.

## Audit

`AppDbContext` writes an `AuditLog` row (who, when, old and new values, merchant) for every change to merchants, branches,
registers, categories, products, warehouses, customers, suppliers, promotions, invoices, returns, purchase orders,
balance entries, users and tickets. Passwords are redacted. Owners see their store's log; admins see all.
