using LiquidStock.Api.Dtos;
using LiquidStock.Api.Data;
using LiquidStock.Api.Dtos;
using LiquidStock.Api.Models;
using LiquidStock.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Runtime.InteropServices;

namespace LiquidStock.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly LiquidStockDbContext _db;
    private readonly OrderService _orders;

    public OrdersController(LiquidStockDbContext db, OrderService orders)
    {
        _db = db;
        _orders = orders;
    }

    /// <summary>Place an order. Prices are taken from the server at this moment.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Create(CreateOrderRequest request, CancellationToken ct)
    {
        var result = await _orders.PlaceOrderAsync(request, ct);
        if (!result.Succeeded)
            return Problem(detail: result.Error, statusCode: result.StatusCode);

        return CreatedAtAction(nameof(GetById), new { id = result.Value!.Id }, result.Value);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> GetById(int id, CancellationToken ct)
    {
        var order = await _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Drink)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

        if (order is null) return NotFound();

        var names = order.Items.ToDictionary(i => i.DrinkId, i => i.Drink.Name);
        return OrderResponse.From(order, names);
    }

    /// <summary>List orders, newest first. Filters: status, customerId, from, to, limit (default 100, max 500).</summary>
    [HttpGet]
    public async Task<ActionResult<List<OrderResponse>>> GetAll(
        [FromQuery] OrderStatus? status = null,
        [FromQuery] int? customerId = null,
        [FromQuery(Name = "from")] DateTime? fromDate = null,
        [FromQuery(Name = "to")] DateTime? toDate = null,
        [FromQuery] int limit = 100,
        CancellationToken ct = default)
    {
        limit = Math.Clamp(limit, 1, 500);

        var query = _db.Orders.AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Drink)
            .AsQueryable();

        if (status is not null) query = query.Where(o => o.Status == status);
        if (customerId is not null) query = query.Where(o => o.CustomerId == customerId);
        if (fromDate is not null) query = query.Where(o => o.OrderDate >= fromDate);
        if (toDate is not null) query = query.Where(o => o.OrderDate <= toDate);

        var orders = await query
            .OrderByDescending(o => o.OrderDate).ThenByDescending(o => o.Id)
            .Take(limit)
            .ToListAsync(ct);

        return orders
            .Select(o => OrderResponse.From(o, o.Items.ToDictionary(i => i.DrinkId, i => i.Drink.Name)))
            .ToList();
    }

    /// <summary>Change status (Pending -> Completed / Cancelled, Completed -> Cancelled). Cancelling restores stock.</summary>
    [HttpPatch("{id:int}/status")]
    public async Task<ActionResult<OrderResponse>> UpdateStatus(
        int id, UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await _orders.UpdateStatusAsync(id, request.Status, ct);
        if (!result.Succeeded)
            return Problem(detail: result.Error, statusCode: result.StatusCode);

        return result.Value!;
    }
}
