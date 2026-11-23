using MicroNamaa.Application.DTOs.Merchants;

namespace MicroNamaa.Application.DTOs.Sync;

/// <summary>
/// What the device keeps for offline work. With "since" only what changed after it (plus Deleted); without it, everything.
/// Save ServerTime and send it as "since" next time. Settings, branches and promotions always come whole.
/// </summary>
public record SyncPullDto(
    DateTime ServerTime,
    bool Full,
    MerchantSettingsDto Settings,
    IReadOnlyList<BranchDto> Branches,
    IReadOnlyList<PromotionDto> Promotions,
    IReadOnlyList<SyncCategoryDto> Categories,
    IReadOnlyList<SyncProductDto> Products,
    IReadOnlyList<SyncCustomerDto> Customers,
    IReadOnlyList<SyncSupplierDto> Suppliers,
    IReadOnlyList<SyncWarehouseDto> Warehouses,
    SyncDeletedDto Deleted);
