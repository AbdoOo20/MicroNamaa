using System.Text.Json;

using System.Text.Json.Nodes;

using Microsoft.EntityFrameworkCore;

using MicroNamaa.Application.Abstractions;

using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Catalog;

using MicroNamaa.Application.DTOs.Merchants;

using MicroNamaa.Application.DTOs.Sync;

using MicroNamaa.Application.Exceptions;

using MicroNamaa.Application.Services.Merchants;

using MicroNamaa.Domain.Entities;

namespace MicroNamaa.Application.Services.Sync;

public class SyncService(IUnitOfWork unitOfWork, IUserAppService userAppService, MerchantContext context, IMerchantSettingsService settingsService, IBranchService branchService, IServiceProvider services) : ISyncService
{
    private const string Applied = "applied";

    private const string Duplicate = "duplicate";

    private const string Failed = "failed";

    private const string Skipped = "skipped";

    public async Task<Result<SyncPullDto>> PullAsync(DateTime? since, CancellationToken cancellationToken = default)
    {
        var serverTime = DateTime.UtcNow;

        var merchantId = context.MerchantId;

        var after = since is { } value ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : (DateTime?)null;

        var settings = await settingsService.GetAsync(cancellationToken);

        var branches = await branchService.GetAllAsync(cancellationToken);

        var promotions = await unitOfWork.Repository<Promotion>().Query()
            .OrderBy(promotion => promotion.Id)
            .Select(promotion => new PromotionDto(promotion.Id, promotion.Name, promotion.Type, promotion.Value, promotion.Scope, promotion.ProductId, promotion.Product == null ? null : promotion.Product.Name, promotion.CategoryId, promotion.Category == null ? null : promotion.Category.Name, promotion.MinOrderAmount, promotion.StartsAt, promotion.EndsAt, promotion.IsActive, promotion.IsActive && (promotion.StartsAt == null || promotion.StartsAt <= serverTime) && (promotion.EndsAt == null || promotion.EndsAt > serverTime)))
            .ToListAsync(cancellationToken);

        var categories = await Changed(unitOfWork.Repository<Category>().Query(), after)
            .Select(category => new { Category = category })
            .ToListAsync(cancellationToken);

        // A product is sent again when it changed or when its stock moved.
        var stockChanged = unitOfWork.Repository<StockItem>().Query().Where(item => after == null || (item.UpdatedAt ?? item.CreatedAt) > after).Select(item => item.ProductId);

        var products = await unitOfWork.Repository<Product>().Query()
            .Where(product => after == null || (product.UpdatedAt ?? product.CreatedAt) > after || stockChanged.Contains(product.Id))
            .Select(product => new { Product = product, Stock = product.Stock.Select(item => new ProductStockDto(item.WarehouseId, item.Warehouse.Name, item.Quantity)).ToList() })
            .ToListAsync(cancellationToken);

        var customers = await Changed(unitOfWork.Repository<Customer>().Query(), after).ToListAsync(cancellationToken);

        var suppliers = userAppService.IsManager ? await Changed(unitOfWork.Repository<Supplier>().Query(), after).ToListAsync(cancellationToken) : [];

        var warehouses = await Changed(unitOfWork.Repository<Warehouse>().Query(), after).ToListAsync(cancellationToken);

        var deleted = after is { } point
            ? new SyncDeletedDto(
                await DeletedAsync<Category>(merchantId, point, cancellationToken),
                await DeletedAsync<Product>(merchantId, point, cancellationToken),
                await DeletedAsync<Customer>(merchantId, point, cancellationToken),
                userAppService.IsManager ? await DeletedAsync<Supplier>(merchantId, point, cancellationToken) : [],
                await DeletedAsync<Warehouse>(merchantId, point, cancellationToken),
                await DeletedAsync<Promotion>(merchantId, point, cancellationToken))
            : new SyncDeletedDto([], [], [], [], [], []);

        return Result<SyncPullDto>.Ok(new SyncPullDto(
            serverTime,
            after is null,
            settings.Data!,
            branches.Data!,
            promotions,
            categories.Select(row => row.Category).Select(category => new SyncCategoryDto(category.Id, category.Name, category.Description, FileUrls.Category(category.Id, category.ImagePath, category.UpdatedAt ?? category.CreatedAt), category.Status, category.SortOrder, category.UpdatedAt ?? category.CreatedAt)).ToList(),
            products.Select(row => new SyncProductDto(row.Product.Id, row.Product.Name, row.Product.Sku, row.Product.Barcode, row.Product.CategoryId, row.Product.Description, FileUrls.Product(row.Product.Id, row.Product.ImagePath, row.Product.UpdatedAt ?? row.Product.CreatedAt), row.Product.Cost, row.Product.Price, row.Product.LowStockThreshold, row.Product.Status, row.Stock, row.Product.UpdatedAt ?? row.Product.CreatedAt)).ToList(),
            customers.Select(customer => new SyncCustomerDto(customer.Id, customer.Code, customer.Name, customer.Phone, customer.Email, customer.City, customer.Address, FileUrls.CustomerAvatar(customer.Id, customer.AvatarPath, customer.UpdatedAt ?? customer.CreatedAt), customer.BranchId, customer.Balance, customer.CreditLimit, customer.LoyaltyPoints, customer.Status, customer.Notes, customer.UpdatedAt ?? customer.CreatedAt)).ToList(),
            suppliers.Select(supplier => new SyncSupplierDto(supplier.Id, supplier.Code, supplier.Name, supplier.ContactPerson, supplier.Phone, supplier.Email, supplier.City, supplier.Address, supplier.TaxNumber, FileUrls.SupplierLogo(supplier.Id, supplier.LogoPath, supplier.UpdatedAt ?? supplier.CreatedAt), supplier.Balance, supplier.Status, supplier.Notes, supplier.UpdatedAt ?? supplier.CreatedAt)).ToList(),
            warehouses.Select(warehouse => new SyncWarehouseDto(warehouse.Id, warehouse.Name, warehouse.Location, warehouse.BranchId, warehouse.Capacity, warehouse.Status, warehouse.UpdatedAt ?? warehouse.CreatedAt)).ToList(),
            deleted));
    }

    public async Task<Result<SyncPushResultDto>> PushAsync(SyncPushRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Operations.Any(operation => operation.OperationId == Guid.Empty))
        {
            return Result<SyncPushResultDto>.BadRequest("Every operation needs an operationId (a new GUID).");
        }

        if (request.Operations.GroupBy(operation => operation.OperationId).Any(group => group.Count() > 1))
        {
            return Result<SyncPushResultDto>.BadRequest("An operationId appears twice in the push.");
        }

        var merchantId = context.MerchantId;

        var results = new List<SyncOperationResultDto>();

        var keyMap = new Dictionary<string, long>(StringComparer.Ordinal);

        var failedKeys = new HashSet<string>(StringComparer.Ordinal);

        foreach (var operation in request.Operations)
        {
            var result = await ApplyAsync(merchantId, operation, keyMap, failedKeys, cancellationToken);

            results.Add(result);

            if (operation.ClientKey is { } key)
            {
                if (result.Id is { } id && result.Status is Applied or Duplicate)
                {
                    keyMap[key] = id;
                }

                else if (result.Status is Failed or Skipped)
                {
                    failedKeys.Add(key);
                }
            }
        }

        var summary = new SyncPushResultDto(
            results.Count(result => result.Status == Applied),
            results.Count(result => result.Status == Duplicate),
            results.Count(result => result.Status is Failed or Skipped),
            results,
            keyMap,
            DateTime.UtcNow);

        var message = summary.Failed == 0 ? "Everything was synced." : $"{summary.Failed} of {results.Count} operations were not synced.";

        return Result<SyncPushResultDto>.Ok(summary, message);
    }

    public Result<IReadOnlyList<SyncOperationTypeDto>> GetOperationTypes() =>
        Result<IReadOnlyList<SyncOperationTypeDto>>.Ok(SyncOperations.All.Values.Select(definition => new SyncOperationTypeDto(definition.Type, definition.Access.ToString(), definition.EntityType, definition.NeedsTargetId, definition.Body)).ToList());

    private async Task<SyncOperationResultDto> ApplyAsync(int merchantId, SyncOperationRequest operation, Dictionary<string, long> keyMap, HashSet<string> failedKeys, CancellationToken cancellationToken)
    {
        var done = await unitOfWork.Repository<SyncOperation>().Query().FirstOrDefaultAsync(item => item.OperationId == operation.OperationId, cancellationToken);

        if (done is not null)
        {
            return new SyncOperationResultDto(operation.OperationId, done.Type, done.ClientKey, Duplicate, done.StatusCode, done.Message, [], done.EntityType, done.EntityId, done.Code);
        }

        if (!SyncOperations.All.TryGetValue(operation.Type, out var definition))
        {
            return Fail(operation, 400, $"Unknown operation type \"{operation.Type}\".");
        }

        if (!Allowed(definition.Access))
        {
            return Fail(operation, 403, "Your role cannot do this operation.");
        }

        try
        {
            var payload = operation.Payload is { } json ? await ResolveAsync(json, keyMap, failedKeys, cancellationToken) : (JsonElement?)null;

            long? targetId = operation.TargetId is { } target ? await ResolveIdAsync(target, keyMap, failedKeys, cancellationToken) : null;

            if (definition.NeedsTargetId && targetId is null)
            {
                return Fail(operation, 400, "targetId is required for this operation.");
            }

            var result = await definition.Run(services, new SyncOperationContext(payload, targetId, operation.OccurredAt, operation.ClientKey), cancellationToken);

            unitOfWork.ClearChanges();

            if (!result.Succeeded)
            {
                return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Failed, result.StatusCode, result.Message, result.Errors, definition.EntityType, null, null);
            }

            var (id, code) = ReadIdentity(result);

            id ??= targetId;

            await unitOfWork.Repository<SyncOperation>().AddAsync(new SyncOperation
            {
                MerchantId = merchantId,

                OperationId = operation.OperationId,

                Type = definition.Type,

                ClientKey = operation.ClientKey,

                EntityType = definition.EntityType,

                EntityId = id,

                Code = code,

                StatusCode = result.StatusCode,

                Message = result.Message,

                OccurredAt = operation.OccurredAt
            }, cancellationToken);

            await unitOfWork.SaveChangesAsync(cancellationToken);

            unitOfWork.ClearChanges();

            return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Applied, result.StatusCode, result.Message, [], definition.EntityType, id, code);
        }

        catch (SyncDependencyException exception)
        {
            await ResetAsync(cancellationToken);

            return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Skipped, 424, exception.Message, [], definition.EntityType, null, null);
        }

        catch (SyncInputException exception)
        {
            await ResetAsync(cancellationToken);

            return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Failed, 400, "The operation is not valid.", exception.Errors, definition.EntityType, null, null);
        }

        catch (AppException exception)
        {
            await ResetAsync(cancellationToken);

            return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Failed, (int)exception.Status, exception.Message, [], definition.EntityType, null, null);
        }

        catch (DbUpdateException)
        {
            await ResetAsync(cancellationToken);

            return new SyncOperationResultDto(operation.OperationId, definition.Type, operation.ClientKey, Failed, 409, "The data changed at the same time. Push again.", [], definition.EntityType, null, null);
        }
    }

    private bool Allowed(SyncAccess access) => access switch
    {
        SyncAccess.Owner => userAppService.IsOwner,

        SyncAccess.Managers => userAppService.IsManager,

        _ => userAppService.MerchantId is not null
    };

    private async Task ResetAsync(CancellationToken cancellationToken)
    {
        await unitOfWork.RollbackTransactionAsync(cancellationToken);

        unitOfWork.ClearChanges();
    }

    /// <summary>Replaces every "@key" (and "@key#n" for invoice lines) in the payload by the server id.</summary>
    private async Task<JsonElement> ResolveAsync(JsonElement payload, Dictionary<string, long> keyMap, HashSet<string> failedKeys, CancellationToken cancellationToken)
    {
        var node = JsonNode.Parse(payload.GetRawText());

        await ResolveNodeAsync(node, keyMap, failedKeys, cancellationToken);

        return JsonSerializer.SerializeToElement(node);
    }

    private async Task ResolveNodeAsync(JsonNode? node, Dictionary<string, long> keyMap, HashSet<string> failedKeys, CancellationToken cancellationToken)
    {
        switch (node)
        {
            case JsonObject jsonObject:
                foreach (var (name, child) in jsonObject.ToList())
                {
                    if (child is JsonValue value && value.TryGetValue<string>(out var text) && text.StartsWith('@'))
                    {
                        jsonObject[name] = await ResolveKeyAsync(text[1..], keyMap, failedKeys, cancellationToken);
                    }

                    else
                    {
                        await ResolveNodeAsync(child, keyMap, failedKeys, cancellationToken);
                    }
                }

                break;

            case JsonArray array:
                foreach (var child in array)
                {
                    await ResolveNodeAsync(child, keyMap, failedKeys, cancellationToken);
                }

                break;
        }
    }

    private async Task<long?> ResolveIdAsync(JsonElement target, Dictionary<string, long> keyMap, HashSet<string> failedKeys, CancellationToken cancellationToken) => target.ValueKind switch
    {
        JsonValueKind.Number => target.GetInt64(),

        JsonValueKind.String when target.GetString() is { } text && text.StartsWith('@') => await ResolveKeyAsync(text[1..], keyMap, failedKeys, cancellationToken),

        JsonValueKind.String when long.TryParse(target.GetString(), out var number) => number,

        JsonValueKind.Null => null,

        _ => throw new SyncInputException(["targetId must be a number or \"@clientKey\"."])
    };

    private async Task<long> ResolveKeyAsync(string reference, Dictionary<string, long> keyMap, HashSet<string> failedKeys, CancellationToken cancellationToken)
    {
        var parts = reference.Split('#', 2);

        var key = parts[0];

        if (failedKeys.Contains(key))
        {
            throw new SyncDependencyException(key);
        }

        if (!keyMap.TryGetValue(key, out var id))
        {
            var known = await unitOfWork.Repository<SyncOperation>().Query()
                .Where(item => item.ClientKey == key && item.EntityId != null)
                .OrderBy(item => item.Id)
                .Select(item => item.EntityId)
                .FirstOrDefaultAsync(cancellationToken);

            id = known ?? throw new SyncDependencyException(key);

            keyMap[key] = id;
        }

        if (parts.Length == 1)
        {
            return id;
        }

        if (!int.TryParse(parts[1], out var line) || line < 0)
        {
            throw new SyncInputException([$"\"@{reference}\" is not a valid invoice line reference (use @key#0, @key#1...)."]);
        }

        var lineIds = await unitOfWork.Repository<SalesInvoiceItem>().Query().Where(item => item.SalesInvoiceId == id).OrderBy(item => item.Id).Select(item => item.Id).ToListAsync(cancellationToken);

        return line < lineIds.Count ? lineIds[line] : throw new SyncInputException([$"The invoice of \"@{key}\" has no line {line}."]);
    }

    private static (long? Id, string? Code) ReadIdentity(Result result)
    {
        var data = result.GetType().GetProperty("Data")?.GetValue(result);

        if (data is null)
        {
            return (null, null);
        }

        var json = JsonSerializer.SerializeToElement(data, SyncOperationContext.JsonOptions);

        if (json.ValueKind != JsonValueKind.Object)
        {
            return (null, null);
        }

        long? id = json.TryGetProperty("id", out var idValue) && idValue.ValueKind == JsonValueKind.Number ? idValue.GetInt64() : null;

        string? code = json.TryGetProperty("code", out var codeValue) && codeValue.ValueKind == JsonValueKind.String ? codeValue.GetString() : null;

        return (id, code);
    }

    private static SyncOperationResultDto Fail(SyncOperationRequest operation, int statusCode, string message) =>
        new(operation.OperationId, operation.Type, operation.ClientKey, Failed, statusCode, message, [], null, null, null);

    private static IQueryable<T> Changed<T>(IQueryable<T> rows, DateTime? after) where T : Domain.Common.BaseEntity<int> =>
        after is { } point ? rows.Where(row => (row.UpdatedAt ?? row.CreatedAt) > point) : rows;

    private async Task<IReadOnlyList<int>> DeletedAsync<T>(int merchantId, DateTime after, CancellationToken cancellationToken) where T : Domain.Common.BaseEntity<int>, Domain.Common.IMerchantOwned =>
        await unitOfWork.Repository<T>().Query()
            .IgnoreQueryFilters()
            .Where(row => row.MerchantId == merchantId && row.IsDeleted && row.UpdatedAt > after)
            .Select(row => row.Id)
            .ToListAsync(cancellationToken);
}
