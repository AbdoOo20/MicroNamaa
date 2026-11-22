# MicroNamaa API – Endpoints for the frontend

Base URL: `http://localhost:5290` (dev). Swagger UI: `/swagger`.

Every response uses the same envelope:

```json
{ "succeeded": true, "statusCode": 200, "message": null, "errors": [], "data": { } }
```

Failures use the same shape with `succeeded: false`, an HTTP status (400, 401, 403, 404, 409, 429, 500) and `errors`/`message`.
Authenticated endpoints need `Authorization: Bearer <accessToken>`.
Paged endpoints return `data = { items, page, pageSize, totalCount, totalPages, hasNext, hasPrevious }` and accept `?page=1&pageSize=20&search=`.
Enums are numbers; use `GET /api/lookups/enums` for names and values.
Money is LYD (3 decimals). Dates are UTC; `from`/`to` filters are Libyan dates (`yyyy-MM-dd`, both included).
Phones are Libyan mobiles (`0912345678`, `+218912345678` and Arabic digits are accepted, stored as `09xxxxxxxx`).
Images: DTOs return ready URLs (`imageUrl`, `avatarUrl`, `logoUrl`) served by `GET /api/files/...`; upload with
`multipart/form-data`, field `file`, JPG/PNG/WEBP up to 5 MB.
List screens have a `/summary` endpoint for the cards and the tab counts (`tabs: [{ key, count }]`) and an `/export` endpoint (CSV).

Roles (`role` in the login response): `PlatformAdmin`, `MerchantOwner`, `MerchantManager`, `Cashier`.
"Managers" below = owner or manager; "Store" = owner, manager or cashier.

## Real-time notifications (SignalR)

Connect to `/hubs/notifications` with `accessTokenFactory` returning the JWT and listen to `notification`:
`{ id, type, title, body, relatedType, relatedId, isRead, createdAt }`. `relatedType` is Product, SalesInvoice, SalesReturn,
PurchaseOrder, Ticket, Merchant or MerchantApplication. Types: LowStock, OutOfStock, PurchaseOrderCreated, PurchaseOrderDelivered,
SalesInvoiceCancelled, SalesReturnCreated, TicketCreated, TicketReply, MerchantApplication, MerchantStatusChanged,
SubscriptionUpdated, SubscriptionEnding. Store alerts go to owners and managers only.

`GET /api/notifications?unreadOnly=` · `GET /api/notifications/unread-count` · `POST /api/notifications/{id}/read` · `POST /api/notifications/read-all`

## Auth and profile (screens: Login, Profile)
| Method | Route | Notes |
|---|---|---|
| POST | `/api/auth/login` | `login` (mobile **or** email) + `password` → `{ accessToken, expiresAtUtc, userId, merchantId, branchId, fullName, role }`. 403 when the store is suspended or the subscription ended |
| GET | `/api/auth/me` | user + `merchantName, merchantLogoUrl, subscriptionEndsAt, branchName, jobTitle`; admins get `isStaff, permissions` |
| PUT | `/api/auth/profile` | `fullName, email?, phone?` |
| POST/DELETE | `/api/auth/profile/avatar` | multipart `file` / remove |
| POST | `/api/auth/change-password` | `currentPassword, newPassword` |

There is no sign-up: the admin creates stores, the store creates its employees.

## POS – Home screen (Store)
| Method | Route | Notes |
|---|---|---|
| GET | `/api/pos/context?branchId=` | merchant name/logo, `branch`, `branches` (managers can switch), `registers`, `warehouse`, `cashier`, `taxRate`, `currency`, `loyalty { enabled, pointsPerCurrencyUnit, pointValue, minPointsToRedeem }`, category chips with counts (first = All Items), running `orderPromotions`, `receiptFooter` |
| GET | `/api/pos/products` | `?branchId=&categoryId=&inStockOnly=&search=` → cards `{ id, name, sku, barcode, imageUrl, price, available, stockState, promotionName, promotionPrice }` (stock of the branch's selling warehouse) |
| GET | `/api/pos/products/lookup?code=` | barcode scan or exact SKU |
| GET | `/api/customers?search=` | customer picker (name, phone, code) with `balance`, `loyaltyPoints` |

## Sales invoices (Store; cashiers see their own)
| Method | Route | Notes |
|---|---|---|
| POST | `/api/sales/invoices/preview` | the cart → totals as the server will save them, `pointsEarned`, stock and credit `warnings` |
| POST | `/api/sales/invoices` | the cart. `complete: false` = Save As Draft / Hold; `complete: true` needs `payment` |
| PUT | `/api/sales/invoices/{id}` | change a draft (same body; can complete it) |
| POST | `/api/sales/invoices/{id}/complete` | `{ payment }` – completes a draft with today's prices |
| POST | `/api/sales/invoices/{id}/cancel` | Managers. `reason`. Only completed invoices without returns; stock, balance and points go back |
| DELETE | `/api/sales/invoices/{id}` | delete a draft |
| GET | `/api/sales/invoices` | `?status=&branchId=&customerId=&cashierId=&paymentMethod=&unpaidOnly=&from=&to=&search=` (number or customer). Drafts screen: `?status=1` |
| GET | `/api/sales/invoices/summary` | same filters → invoices, net sales, returns, unpaid, average + status tabs |
| GET | `/api/sales/invoices/{id}` | full invoice with items, `canCancel`, `canReturn`; items carry `returnedQuantity` |
| GET | `/api/sales/invoices/export` | Managers, CSV |

Cart body:
```json
{
  "branchId": null, "registerId": 2, "customerId": 5,
  "items": [ { "productId": 10, "quantity": 2 } ],
  "promotionId": 1, "discountType": 1, "discountValue": 5,
  "pointsToRedeem": 100, "note": null,
  "complete": true,
  "payment": { "method": 1, "tenderedAmount": 300, "paidAmount": null }
}
```
`payment.method`: 1 Cash (`tenderedAmount` ≥ total → `changeAmount`), 2 Card, 3 Deferred (customer required, `paidAmount`
now, the rest goes to the customer's balance within their credit limit), 4 BankTransfer. `discountType`: 1 Percentage, 2 FixedAmount.
Totals: `total = subtotal - itemsDiscount - orderDiscount - pointsDiscount + taxAmount` (tax after discounts).
Statuses: 1 Draft, 2 Completed, 3 Cancelled.

## Returns (Store; cashiers for their branch)
`GET /api/sales/returns?branchId=&customerId=&salesInvoiceId=&refundMethod=&from=&to=&search=` · `GET /api/sales/returns/{id}` ·
`POST /api/sales/returns` `{ salesInvoiceId, items: [{ salesInvoiceItemId, quantity, restock }], refundMethod, reason? }` ·
`GET /api/sales/returns/export`. `refundMethod`: 1 Cash, 2 Card, 3 CustomerBalance. Each unit refunds what was really paid
(discounts and tax shared over the lines); `restock: false` for damaged goods.

## Products (list: Store; the rest: Managers)
| Method | Route | Notes |
|---|---|---|
| GET | `/api/products` | `?status=&stockState=&categoryId=&warehouseId=&sort=name\|price\|quantity\|newest&search=` |
| GET | `/api/products/summary` | `totalProducts, totalCost (stock at cost), totalRetailValue, lowStock, outOfStock`, tabs all/draft/active/archived/lowStock/outOfStock |
| GET | `/api/products/{id}` | details + stock per warehouse + `performance { unitsSoldThisMonth, revenueThisMonth, monthlyDemandPercent, grossMarginPercent, grossProfitThisMonth, trend (6 months), demandShare (3 months) }` |
| POST | `/api/products` | `name, sku?, barcode?, categoryId?, description?, cost, price, lowStockThreshold?, status, openingQuantity, warehouseId?` (empty SKU → `SKU-00012`) |
| PUT | `/api/products/{id}` | same without opening stock (stock changes through inventory) |
| DELETE | `/api/products/{id}` | 409 when it has sales or purchases → archive it (`status: 3`) |
| POST/DELETE | `/api/products/{id}/image` | multipart `file` / remove |
| GET | `/api/products/export` | CSV |

Stock states: 1 InStock, 2 LowStock, 3 OutOfStock. Record status (products, categories, warehouses, customers, suppliers): 1 Draft, 2 Active, 3 Archived.

## Categories (list: Store; the rest: Managers)
`GET /api/categories?status=` (with `productsCount, stockUnits, stockValue`) · `GET /api/categories/summary` (`totalCategories, totalProducts, totalCost`) ·
`GET/PUT/DELETE /api/categories/{id}` · `POST /api/categories` (`name, description?, status, sortOrder`) · `POST/DELETE /api/categories/{id}/image` · `GET /api/categories/export`

## Warehouses and inventory (Managers)
| Area | Routes |
|---|---|
| Warehouses | `GET /api/warehouses?status=&branchId=` (`productsCount, totalUnits, capacityPercent, stockValue`) · `GET /api/warehouses/summary` (`activeWarehouses, totalProducts, averageCapacity`) · `GET/PUT/DELETE /api/warehouses/{id}` · `POST /api/warehouses` (`name, location?, branchId?, capacity?, status`) · `GET /api/warehouses/export` |
| Stock | `GET /api/inventory/stock?warehouseId=&productId=&stockState=&search=` |
| Movements | `GET /api/inventory/movements?productId=&warehouseId=&type=&from=&to=&search=` (type 1 Opening, 2 Purchase, 3 Sale, 4 SaleReturn, 5 SaleCancelled, 6 Adjustment, 7 TransferIn, 8 TransferOut) |
| Stock count | `POST /api/inventory/adjustments` `{ warehouseId, items: [{ productId, countedQuantity }], note? }` |
| Transfer | `POST /api/inventory/transfers` `{ fromWarehouseId, toWarehouseId, items: [{ productId, quantity }], note? }` |

## Customers (Store; delete, adjustments, summary and export: Managers)
`GET /api/customers?status=&branchId=&owingOnly=` (`code, ordersCount, lifetimeValue, balance, loyaltyPoints, lastPurchaseAt`) ·
`GET /api/customers/summary` (`totalCustomers, totalSales, outstandingBalances`) · `GET/PUT/DELETE /api/customers/{id}` ·
`POST /api/customers` (`name, phone?, email?, city?, address?, branchId?, creditLimit?, status, notes?`) · `POST/DELETE /api/customers/{id}/avatar` ·
`GET /api/customers/{id}/statement?type=&from=&to=` · `POST /api/customers/{id}/payments` (`amount, method, reference?, note?`) ·
`POST /api/customers/{id}/adjustments` (`amount ±, note`) · `GET /api/customers/export`

## Suppliers (Managers)
`GET /api/suppliers?status=&owedOnly=` (`code SUP-001, contactPerson, balance, purchasesCount, lastPurchaseAt, logoUrl`) ·
`GET /api/suppliers/summary` (`totalPayments, numberOfSuppliers, totalInvoices, totalOwed`) · `GET/PUT/DELETE /api/suppliers/{id}` ·
`POST /api/suppliers` (`name, contactPerson?, phone?, email?, city?, address?, taxNumber?, status, notes?`) · `POST/DELETE /api/suppliers/{id}/logo` ·
`GET /api/suppliers/{id}/statement` · `POST /api/suppliers/{id}/payments` · `POST /api/suppliers/{id}/adjustments` · `GET /api/suppliers/export`

## Purchases (Managers)
| Method | Route | Notes |
|---|---|---|
| GET | `/api/purchases` | `?status=&supplierId=&warehouseId=&from=&to=&search=` |
| GET | `/api/purchases/summary` | `totalCost, totalSuppliers, totalInvoices, awaitingDelivery` + status tabs |
| GET | `/api/purchases/{id}` | with items |
| POST/PUT | `/api/purchases` · `/{id}` | `supplierId, warehouseId, orderDate?, expectedAt?, paymentTerms?, dueDate?, items: [{ productId, quantity, unitCost }], discount, taxRate?, shippingCost, note?, status` (1 Draft, 2 Pending, 3 InTransit) |
| POST | `/api/purchases/{id}/status` | `status` 2 Pending or 3 InTransit |
| POST | `/api/purchases/{id}/deliver` | `paidAmount, paymentMethod?, deliveredAt?` → stock in, moving-average cost, supplier balance (status 4 Delivered) |
| POST | `/api/purchases/{id}/cancel` | `reason` (status 5) |
| DELETE | `/api/purchases/{id}` | drafts only |
| GET | `/api/purchases/export` | CSV |

## Employees (Managers; managers handle cashiers only)
`GET /api/employees?role=&branchId=&isActive=` (`role, jobTitle, branchName, monthlyTarget, salesThisMonth, performancePercent, isActive`) ·
`GET /api/employees/summary` (`totalEmployees, activeAccounts, averagePerformance`) · `GET/PUT/DELETE /api/employees/{id}` ·
`POST /api/employees` (`fullName, email?, phone?, password, role, jobTitle?, branchId? (required for cashiers), monthlyTarget?`) ·
`POST /api/employees/{id}/password` (`newPassword`) · `POST/DELETE /api/employees/{id}/avatar` · `GET /api/employees/export`.
`role`: `MerchantOwner`, `MerchantManager`, `Cashier`. Employees with invoices are turned off (`isActive: false`), not deleted.

## Dashboard and reports (Managers)
| Route | Notes |
|---|---|
| `GET /api/dashboard/summary` | `salesThisMonth, salesGrowthPercent (vs same days of last month), ordersThisMonth, totalProducts, productsAddedThisMonth, customers, warehouses, outOfStock, lowStock, awaitingDelivery` |
| `GET /api/dashboard/revenue?range=7d\|1m\|1y` | bars `{ period, label, revenue, orders }` |
| `GET /api/dashboard/top-products?limit=4` | this month |
| `GET /api/dashboard/activity?limit=10` | `{ type, title, reference, badge, relatedType, relatedId, at }` |
| `GET /api/reports/overview` | `revenue, orders, averageOrderValue, itemsSold, returns, discounts, tax, grossProfit, grossMarginPercent, unpaidAmount, newCustomers` + growth vs the previous period |
| `GET /api/reports/sales-trend?groupBy=day\|month` | net revenue per Libyan day or month |
| `GET /api/reports/top-products?limit=` · `/categories` · `/employees` · `/payment-methods` · `/branches` | |
| `GET /api/reports/warehouses` | stock units and value per warehouse now |
| `GET /api/reports/export` | daily CSV |

All reports accept `?from=&to=&branchId=` (last 30 days by default).

## Store setup
| Area | Routes |
|---|---|
| Settings | `GET /api/settings` (Store) · `PUT /api/settings` (Owner: profile, `taxRate`, `allowNegativeStock`, `defaultLowStockThreshold`, loyalty, `receiptFooter`) · `POST/DELETE /api/settings/logo` (Owner) |
| Branches | `GET /api/branches` (Store) · `POST` (Owner: `name, city?, address?, phone?, defaultWarehouseId?, createWarehouse, isActive`) · `PUT /{id}` (Managers) · `DELETE /{id}` (Owner) · registers `POST /{id}/registers`, `PUT/DELETE /{id}/registers/{registerId}` |
| Promotions | `GET /api/promotions?scope=&runningOnly=` (Store) · `POST/PUT/DELETE` (Managers): `name, type, value, scope (1 Order, 2 Product, 3 Category), productId?, categoryId?, minOrderAmount?, startsAt?, endsAt?, isActive` |
| Audit log | `GET /api/audit-logs?entityName=&entityId=&userId=&from=&to=` (Owner) |
| Help center | `GET/POST /api/tickets` · `GET /api/tickets/{id}` · `POST /api/tickets/{id}/messages` · `POST /api/tickets/{id}/close` (owners and managers; admins answer) |

## Landing page (anonymous)
`POST /api/merchant-applications` (`businessName, ownerName, phone, email?, city?, businessType?, branchesCount?, notes?`). The admins get a notification.

## Platform admin (role PlatformAdmin)
| Area | Routes |
|---|---|
| Overview | `GET /api/admin/overview` → merchants, active, suspended, expiring soon, expired, pending requests, open tickets, platform sales (30 days), top merchants |
| Merchants | `GET /api/admin/merchants?status=&expiringWithinDays=&search=` · `GET /api/admin/merchants/{id}` · `POST` (`name, ownerName, phone, email?, ownerPassword, city?, address?, taxNumber?, subscriptionEndsOn?, maxBranches?, maxUsers?, taxRate, branchName?, notes?`) · `PUT /{id}` · `POST /{id}/status` (`status` 1 Active, 2 Suspended, `note?`) · `POST /{id}/subscription` (`endsOn` or `extendDays` or `unlimited`) · `POST /{id}/users/{userId}/password` |
| Requests | `GET /api/admin/merchant-applications?status=` · `GET /{id}` · `POST /{id}/approve` (`ownerPassword, email?, subscriptionEndsOn?, maxBranches?, maxUsers?, adminNote?`, creates the merchant) · `POST /{id}/reject` (`adminNote`) |
| Tickets | `/api/tickets` (all stores, `?merchantId=`) |
| Audit | `GET /api/admin/audit-logs?merchantId=` |
| Staff (owner only) | `GET/POST /api/admin/staff` · `PUT /api/admin/staff/{id}` · `POST /api/admin/staff/{id}/password` |

Staff permissions: `area` 1 Overview, 2 Merchants, 3 MerchantApplications, 4 Tickets, 5 Settings, 6 AuditLog;
`actions` bit flags 1 View, 2 Create, 4 Update, 8 Delete.

## Test accounts (Development, `SampleData:Enabled`)
| Role | Login | Password |
|---|---|---|
| Platform admin | `admin@micronamaa.test` | `Admin@12345` |
| Store owner (سوبرماركت النماء) | `0910000001` | `Test@12345` |
| Cashier (Tripoli Central #01) | `0910000002` | `Test@12345` |
