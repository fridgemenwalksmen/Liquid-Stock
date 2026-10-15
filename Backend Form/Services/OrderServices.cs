using LiquidStock.Api.Data;
using LiquidStock.Api.Dtos;
using LiquidStock.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace LiquidStock.Api.Services;

public class OrderService
{
    private readonly LiquidStockDbContext _db;

    public OrderService(LiquidStockDbContext db) => _db = db;

    /// <summary>
    /// Places an order in ONE database transaction:
    ///  1. take the stock with an atomic UPDATE (fails if there isn't enough, so nobody can oversell)
    ///  2. read the current price (the UPDATE holds a lock on the drink row, so the price can't move under us)
    ///  3. save the order + items, snapshotting the price into unit_price
    ///  4. mirror the new stock into the Inventory table
    /// If anything fails, everything is rolled back.
    /// </summary>
    public async Task<ServiceResult<OrderResponse>> PlaceOrderAsync(
        CreateOrderRequest request, CancellationToken ct = default)
    {
        // Merge duplicate lines (the Order_Item table has UNIQUE(order_ID, drink_ID)).
        // Sorted by drink id so two concurrent orders always lock rows in the same order (no deadlocks).
        var lines = request.Items
            .GroupBy(i => i.DrinkId)
            .Select(g => new { DrinkId = g.Key, Quantity = g.Sum(x => x.Quantity) })
            .OrderBy(l => l.DrinkId)
            .ToList();

        if (lines.Count == 0)
            return ServiceResult<OrderResponse>.Fail(400, "An order needs at least one item.");

        if (lines.Any(l => l.Quantity > 1000))
            return ServiceResult<OrderResponse>.Fail(400, "Maximum 1000 of one drink per order.");

        if (request.CustomerId is int customerId &&
            !await _db.Customers.AnyAsync(c => c.Id == customerId, ct))
            return ServiceResult<OrderResponse>.Fail(400, $"Customer {customerId} does not exist.");

        if (request.StaffId is int staffId &&
            !await _db.Staff.AnyAsync(s => s.Id == staffId && s.IsActive, ct))
            return ServiceResult<OrderResponse>.Fail(400, $"Staff member {staffId} does not exist or is not active.");

        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        // 1. Take the stock atomically.
        foreach (var line in lines)
        {
            var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
                $@"UPDATE Drink
                   SET stock_quantity = stock_quantity - {line.Quantity}
                   WHERE drink_ID = {line.DrinkId}
                     AND is_active = 1
                     AND stock_quantity >= {line.Quantity}", ct);

            if (rows == 0)
            {
                var drink = await _db.Drinks.AsNoTracking()
                    .Where(d => d.Id == line.DrinkId)
                    .Select(d => new { d.Name, d.IsActive, d.StockQuantity })
                    .FirstOrDefaultAsync(ct);

                await tx.RollbackAsync(ct);

                if (drink is null)
                    return ServiceResult<OrderResponse>.Fail(404, $"Drink {line.DrinkId} does not exist.");
                if (!drink.IsActive)
                    return ServiceResult<OrderResponse>.Fail(409, $"{drink.Name} is not available.");
                return ServiceResult<OrderResponse>.Fail(409,
                    $"Not enough stock for {drink.Name}: {drink.StockQuantity} left, {line.Quantity} requested.");
            }
        }

        // 2. Read prices (rows are locked by our UPDATEs until we commit).
        var ids = lines.Select(l => l.DrinkId).ToList();
        var drinks = await _db.Drinks.AsNoTracking()
            .Where(d => ids.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, ct);

        // 3. Save the order with the prices snapshotted.
        var order = new Order
        {
            CustomerId = request.CustomerId,
            StaffId = request.StaffId,
            Status = request.CompleteImmediately ? OrderStatus.Completed : OrderStatus.Pending,
            Items = lines.Select(l => new OrderItem
            {
                DrinkId = l.DrinkId,
                Quantity = l.Quantity,
                UnitPrice = drinks[l.DrinkId].CurrentPrice
            }).ToList()
        };
        order.TotalAmount = Math.Round(order.Items.Sum(i => i.UnitPrice * i.Quantity), 2);

        _db.Orders.Add(order);
        await _db.SaveChangesAsync(ct);

        // 4. Keep the Inventory table in step with Drink.stock_quantity.
        foreach (var line in lines)
            await InventorySync.SyncAsync(_db, line.DrinkId, ct);

        await tx.CommitAsync(ct);

        // STEP 3 (pricing engine) will be triggered here, after the commit.

        var names = drinks.ToDictionary(kv => kv.Key, kv => kv.Value.Name);
        return ServiceResult<OrderResponse>.Ok(OrderResponse.From(order, names));
    }

    /// <summary>
    /// Allowed changes: Pending -> Completed, Pending -> Cancelled, Completed -> Cancelled.
    /// Cancelling puts the drinks back into stock.
    /// </summary>
    public async Task<ServiceResult<OrderResponse>> UpdateStatusAsync(
        int orderId, OrderStatus newStatus, CancellationToken ct = default)
    {
        await using var tx = await _db.Database.BeginTransactionAsync(ct);

        var order = await _db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Drink)
            .FirstOrDefaultAsync(o => o.Id == orderId, ct);

        if (order is null)
        {
            await tx.RollbackAsync(ct);
            return ServiceResult<OrderResponse>.Fail(404, $"Order {orderId} does not exist.");
        }

        var names = order.Items.ToDictionary(i => i.DrinkId, i => i.Drink.Name);

        if (order.Status == newStatus)
        {
            await tx.RollbackAsync(ct);
            return ServiceResult<OrderResponse>.Ok(OrderResponse.From(order, names));
        }

        var allowed =
            (order.Status == OrderStatus.Pending && newStatus is OrderStatus.Completed or OrderStatus.Cancelled) ||
            (order.Status == OrderStatus.Completed && newStatus == OrderStatus.Cancelled);

        if (!allowed)
        {
            await tx.RollbackAsync(ct);
            return ServiceResult<OrderResponse>.Fail(409,
                $"An order that is {order.Status} cannot be changed to {newStatus}.");
        }

        if (newStatus == OrderStatus.Cancelled)
        {
            foreach (var item in order.Items.OrderBy(i => i.DrinkId))
            {
                await _db.Database.ExecuteSqlInterpolatedAsync(
                    $"UPDATE Drink SET stock_quantity = stock_quantity + {item.Quantity} WHERE drink_ID = {item.DrinkId}", ct);
                await InventorySync.SyncAsync(_db, item.DrinkId, ct);
            }
        }

        order.Status = newStatus;
        await _db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);

        return ServiceResult<OrderResponse>.Ok(OrderResponse.From(order, names));
    }
}
