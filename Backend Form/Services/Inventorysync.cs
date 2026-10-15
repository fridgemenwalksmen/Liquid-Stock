using LiquidStock.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace LiquidStock.Api.Services;

/// <summary>
/// The database stores stock twice (Drink.stock_quantity and Inventory.quantity).
/// Drink.stock_quantity is the source of truth; this copies it into Inventory so they never drift.
/// Call it inside the same transaction that changed the stock.
/// </summary>
public static class InventorySync
{
    public static async Task SyncAsync(LiquidStockDbContext db, int drinkId, CancellationToken ct = default)
    {
        var updated = await db.Database.ExecuteSqlInterpolatedAsync(
            $@"UPDATE Inventory
               SET quantity = (SELECT stock_quantity FROM Drink WHERE drink_ID = {drinkId}),
                   last_updated = SYSDATETIME()
               WHERE drink_ID = {drinkId}", ct);

        if (updated == 0)
        {
            // Drink has no inventory row yet (e.g. created directly in SSMS): create it.
            await db.Database.ExecuteSqlInterpolatedAsync(
                $@"INSERT INTO Inventory (drink_ID, quantity)
                   SELECT drink_ID, stock_quantity FROM Drink WHERE drink_ID = {drinkId}", ct);
        }
    }
}
