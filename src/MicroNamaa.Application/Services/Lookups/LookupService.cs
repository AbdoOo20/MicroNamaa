using MicroNamaa.Application.Common;

using MicroNamaa.Application.DTOs.Lookups;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Lookups;

public class LookupService : ILookupService
{
    private static readonly Type[] EnumTypes =
    [
        typeof(RecordStatus),

        typeof(StockState),

        typeof(StockMovementType),

        typeof(SalesInvoiceStatus),

        typeof(PaymentMethod),

        typeof(RefundMethod),

        typeof(PurchaseOrderStatus),

        typeof(DiscountType),

        typeof(PromotionScope),

        typeof(BalanceEntryType),

        typeof(MerchantStatus),

        typeof(MerchantApplicationStatus),

        typeof(TicketStatus),

        typeof(TicketCategory),

        typeof(AdminArea),

        typeof(AdminActions),

        typeof(NotificationAudience)
    ];

    public Result<IReadOnlyDictionary<string, IReadOnlyList<EnumItemDto>>> GetEnums()
    {
        var enums = EnumTypes.ToDictionary(
            type => type.Name,
            type => (IReadOnlyList<EnumItemDto>)Enum.GetValues(type).Cast<object>().Select(value => new EnumItemDto(Convert.ToInt32(value), value.ToString()!)).ToList());

        return Result<IReadOnlyDictionary<string, IReadOnlyList<EnumItemDto>>>.Ok(enums);
    }
}
