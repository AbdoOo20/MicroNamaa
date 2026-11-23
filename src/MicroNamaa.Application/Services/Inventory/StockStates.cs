using System.Linq.Expressions;

using MicroNamaa.Domain.Entities;

using MicroNamaa.Domain.Enums;

namespace MicroNamaa.Application.Services.Inventory;

public static class StockStates
{
    public static StockState Of(int quantity, int threshold) => quantity <= 0 ? StockState.OutOfStock : quantity <= threshold ? StockState.LowStock : StockState.InStock;

    /// <summary>The product's total units over every warehouse, for use inside queries.</summary>
    public static Expression<Func<Product, int>> TotalQuantity => product => product.Stock.Sum(item => (int?)item.Quantity) ?? 0;
}
