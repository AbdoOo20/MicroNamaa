using Microsoft.AspNetCore.Identity;

using Microsoft.EntityFrameworkCore;

using Microsoft.Extensions.DependencyInjection;

using Microsoft.Extensions.Logging;

using Microsoft.Extensions.Options;

using MicroNamaa.Application.DTOs.Admin;

using MicroNamaa.Application.Services.Admin;

using MicroNamaa.Application.Settings;

using MicroNamaa.Domain.Constants;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

using MicroNamaa.Infrastructure.Persistence;

namespace MicroNamaa.Infrastructure.Seeding;

/// <summary>
/// Development data: one store (owner + cashier) with categories, products and stock, customers, suppliers and promotions,
/// shaped like the Figma screens. Runs once, when <c>SampleData:Enabled</c> is true and the owner's number is not registered yet.
/// </summary>
public static class SampleDataSeeder
{
    public static async Task SeedSampleDataAsync(this IServiceProvider services, CancellationToken cancellationToken = default)
    {
        using var scope = services.CreateScope();

        var settings = scope.ServiceProvider.GetRequiredService<IOptions<SampleDataSettings>>().Value;

        if (!settings.Enabled)
        {
            return;
        }

        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("SampleDataSeeder");

        try
        {
            var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            if (await context.Users.AnyAsync(user => user.PhoneNumber == settings.OwnerPhone, cancellationToken))
            {
                return;
            }

            var provisioning = scope.ServiceProvider.GetRequiredService<IMerchantProvisioningService>();

            var created = await provisioning.CreateAsync(new CreateMerchantRequest
            {
                Name = "سوبرماركت النماء",

                OwnerName = "أحمد ناصر",

                Phone = settings.OwnerPhone,

                Email = "owner@micronamaa.test",

                OwnerPassword = settings.Password,

                City = "طرابلس",

                Address = "طرابلس، شارع الجمهورية",

                BranchName = "Tripoli Central #01",

                TaxRate = 5,

                SubscriptionEndsOn = DateOnly.FromDateTime(DateTime.UtcNow).AddYears(1)
            }, cancellationToken);

            if (!created.Succeeded)
            {
                logger.LogWarning("Sample merchant was not created: {Errors}", string.Join("; ", created.Errors.Append(created.Message ?? string.Empty)));

                return;
            }

            var merchantId = created.Data;

            var merchant = await context.Merchants.FirstAsync(item => item.Id == merchantId, cancellationToken);

            merchant.LoyaltyEnabled = true;

            var branch = await context.Branches.FirstAsync(item => item.MerchantId == merchantId, cancellationToken);

            var mainWarehouse = await context.Warehouses.FirstAsync(item => item.MerchantId == merchantId, cancellationToken);

            mainWarehouse.Capacity = 5000;

            var benghazi = new Branch { MerchantId = merchantId, Name = "Benghazi #02", City = "بنغازي", Address = "بنغازي، الشارع الرئيسي" };

            context.Branches.Add(benghazi);

            await context.SaveChangesAsync(cancellationToken);

            var second = new Warehouse { MerchantId = merchantId, BranchId = benghazi.Id, Name = "مخزن بنغازي", Location = "City Of Benghazi", Capacity = 3000 };

            context.Warehouses.Add(second);

            context.Registers.Add(new Register { MerchantId = merchantId, BranchId = branch.Id, Name = "Reg 02" });

            context.Registers.Add(new Register { MerchantId = merchantId, BranchId = benghazi.Id, Name = "Reg 01" });

            await context.SaveChangesAsync(cancellationToken);

            benghazi.DefaultWarehouseId = second.Id;

            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<User>>();

            var cashier = new User { UserName = settings.CashierPhone, PhoneNumber = settings.CashierPhone, FullName = "سارة الورفلي", MerchantId = merchantId, BranchId = branch.Id, JobTitle = "Senior Sales Rep", MonthlyTarget = 12000 };

            if ((await userManager.CreateAsync(cashier, settings.Password)).Succeeded)
            {
                await userManager.AddToRoleAsync(cashier, RoleNames.Cashier);
            }

            var categories = new[] { "Beverages", "Snacks & Pantry", "Dairy & Fresh", "Office", "Household" }
                .Select((name, index) => new Category { MerchantId = merchantId, Name = name, SortOrder = index + 1 })
                .ToList();

            context.Categories.AddRange(categories);

            await context.SaveChangesAsync(cancellationToken);

            Category Of(string name) => categories.First(category => category.Name == name);

            var products = new List<(Product Product, int Quantity)>
            {
                (new Product { Name = "Al-Waha Mineral Water 500ml (Pack of 12)", Sku = "BEV-WAT-001", Barcode = "6281000000011", Category = Of("Beverages"), Cost = 11, Price = 18 }, 142),

                (new Product { Name = "Illy Espresso Ground Coffee 250g", Sku = "BEV-COF-014", Barcode = "8003753900438", Category = Of("Beverages"), Cost = 32, Price = 45 }, 38),

                (new Product { Name = "Tetley Premium Black Tea 100 Bags", Sku = "BEV-TEA-003", Barcode = "5011546498367", Category = Of("Beverages"), Cost = 9.5m, Price = 14.5m }, 64),

                (new Product { Name = "Lipton Ice Tea Peach 330ml Can", Sku = "BEV-CAN-088", Barcode = "5410013136149", Category = Of("Beverages"), Cost = 2.2m, Price = 3.5m }, 210),

                (new Product { Name = "Nutella Hazelnut Spread 750g", Sku = "FDN-NUT-009", Barcode = "8000500167113", Category = Of("Snacks & Pantry"), Cost = 19, Price = 28 }, 19),

                (new Product { Name = "Barilla Spaghetti No. 5 500g", Sku = "FDN-PAS-022", Barcode = "8076802085738", Category = Of("Snacks & Pantry"), Cost = 4.2m, Price = 6.5m }, 85),

                (new Product { Name = "Kinder Bueno White Chocolate", Sku = "SNK-CHO-017", Barcode = "8000500247563", Category = Of("Snacks & Pantry"), Cost = 55, Price = 75 }, 0),

                (new Product { Name = "Double A A4 Copy Paper 80gsm (Box)", Sku = "OFC-PPR-102", Barcode = "8858734600014", Category = Of("Office"), Cost = 90, Price = 120 }, 12),

                (new Product { Name = "Ariel Automatic Detergent Powder 3kg", Sku = "HOU-DET-051", Barcode = "8001090621771", Category = Of("Household"), Cost = 44, Price = 62 }, 15),

                (new Product { Name = "Almarai Fresh Milk 1L", Sku = "DRY-MLK-004", Barcode = "6281007032513", Category = Of("Dairy & Fresh"), Cost = 3.4m, Price = 5 }, 6)
            };

            foreach (var (product, _) in products)
            {
                product.MerchantId = merchantId;
            }

            context.Products.AddRange(products.Select(row => row.Product));

            await context.SaveChangesAsync(cancellationToken);

            foreach (var (product, quantity) in products.Where(row => row.Quantity > 0))
            {
                context.StockItems.Add(new StockItem { MerchantId = merchantId, WarehouseId = mainWarehouse.Id, ProductId = product.Id, Quantity = quantity });

                context.StockMovements.Add(new StockMovement { MerchantId = merchantId, WarehouseId = mainWarehouse.Id, ProductId = product.Id, Type = StockMovementType.Opening, Quantity = quantity, BalanceAfter = quantity, UnitCost = product.Cost, ReferenceType = "Product", ReferenceId = product.Id, ReferenceCode = product.Sku, Note = "Opening stock" });
            }

            var customers = new[]
            {
                ("Michael Chen", "0912345671", 120),
                ("محمد الورفلي", "0922345672", 40),
                ("Elena Rodriguez", "0942345673", 0),
                ("فاطمة الزوي", "0912345674", 260)
            };

            var number = 0;

            foreach (var (name, phone, points) in customers)
            {
                context.Customers.Add(new Customer { MerchantId = merchantId, Number = ++number, Name = name, Phone = phone, BranchId = branch.Id, LoyaltyPoints = points, CreditLimit = 2000 });
            }

            var suppliers = new[]
            {
                ("Sama Al-Libya", "سما الليبية"),
                ("Libyan Group", "المجموعة الليبية"),
                ("H A For Industrial Zones", "هـ ع للمناطق الصناعية"),
                ("Libyana", "ليبيانا"),
                ("Al-Raed Al-Ammar", "الرائد المعماري")
            };

            number = 0;

            foreach (var (name, contact) in suppliers)
            {
                context.Suppliers.Add(new Supplier { MerchantId = merchantId, Number = ++number, Name = name, ContactPerson = contact, City = "طرابلس" });
            }

            context.DocumentCounters.Add(new DocumentCounter { MerchantId = merchantId, Type = DocumentType.Customer, LastNumber = customers.Length });

            context.DocumentCounters.Add(new DocumentCounter { MerchantId = merchantId, Type = DocumentType.Supplier, LastNumber = suppliers.Length });

            context.Promotions.Add(new Promotion { MerchantId = merchantId, Name = "VIP Promo", Type = DiscountType.FixedAmount, Value = 10, Scope = PromotionScope.Order, MinOrderAmount = 100 });

            context.Promotions.Add(new Promotion { MerchantId = merchantId, Name = "-5% Promo", Type = DiscountType.Percentage, Value = 5, Scope = PromotionScope.Product, ProductId = products[1].Product.Id });

            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation("Sample merchant {Name} was created (owner {Owner}, cashier {Cashier}).", merchant.Name, settings.OwnerPhone, settings.CashierPhone);
        }

        catch (Exception exception)
        {
            logger.LogWarning(exception, "Sample data seeding was skipped. Make sure the database is migrated.");
        }
    }
}
