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
public class DrinksController : ControllerBase
{
    private readonly LiquidStockDbContext _db;

    public DrinksController(LiquidStockDbContext db) => _db = db;

    // Shared query: drink + the price it had before its latest change.
    private static IQueryable<DrinkResponse> Project(IQueryable<Drink> query) =>
        query.Select(d => new DrinkResponse
        {
            Id = d.Id,
            Name = d.Name,
            Category = d.Category,
            CurrentPrice = d.CurrentPrice,
            MinPrice = d.MinPrice,
            MaxPrice = d.MaxPrice,
            StockQuantity = d.StockQuantity,
            IsActive = d.IsActive,
            PreviousPrice = d.PriceHistory
                .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
                .Select(h => (decimal?)h.OldPrice)
                .FirstOrDefault()
        });

    /// <summary>All drinks with current prices. Active drinks only unless includeInactive=true.</summary>
    [HttpGet]
    public async Task<ActionResult<List<DrinkResponse>>> GetAll(
        [FromQuery] bool includeInactive = false,
        [FromQuery] string? category = null)
    {
        var query = _db.Drinks.AsNoTracking().AsQueryable();
        if (!includeInactive) query = query.Where(d => d.IsActive);
        if (!string.IsNullOrWhiteSpace(category)) query = query.Where(d => d.Category == category);

        return await Project(query.OrderBy(d => d.Category).ThenBy(d => d.Name)).ToListAsync();
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<DrinkResponse>> GetById(int id)
    {
        var drink = await Project(_db.Drinks.AsNoTracking().Where(d => d.Id == id)).FirstOrDefaultAsync();
        if (drink is null) return NotFound();
        return drink;
    }

    /// <summary>Price changes over time, oldest first (ready for a chart). Defaults to the latest 200.</summary>
    [HttpGet("{id:int}/price-history")]
    public async Task<ActionResult<List<PriceHistoryResponse>>> GetPriceHistory(
        int id,
        [FromQuery(Name = "from")] DateTime? fromDate = null,
        [FromQuery(Name = "to")] DateTime? toDate = null,
        [FromQuery] int limit = 200)
    {
        if (!await _db.Drinks.AnyAsync(d => d.Id == id)) return NotFound();
        limit = Math.Clamp(limit, 1, 2000);

        var query = _db.PriceHistories.AsNoTracking().Where(h => h.DrinkId == id);
        if (fromDate is not null) query = query.Where(h => h.ChangedAt >= fromDate);
        if (toDate is not null) query = query.Where(h => h.ChangedAt <= toDate);

        var rows = await query
            .OrderByDescending(h => h.ChangedAt).ThenByDescending(h => h.Id)
            .Take(limit)
            .Select(h => new PriceHistoryResponse
            {
                Id = h.Id,
                DrinkId = h.DrinkId,
                OldPrice = h.OldPrice,
                NewPrice = h.NewPrice,
                ChangedAt = h.ChangedAt,
                Reason = h.Reason
            })
            .ToListAsync();

        rows.Reverse(); // oldest first
        return rows;
    }

    [HttpPost]
    public async Task<ActionResult<DrinkResponse>> Create(CreateDrinkRequest req)
    {
        var name = req.Name.Trim();
        var error = ValidateRange(req.MinPrice, req.MaxPrice, req.CurrentPrice);
        if (error is not null) return Problem(detail: error, statusCode: 400);

        if (await _db.Drinks.AnyAsync(d => d.Name == name))
            return Problem(detail: $"A drink called '{name}' already exists.", statusCode: 409);

        var drink = new Drink
        {
            Name = name,
            Category = req.Category.Trim(),
            CurrentPrice = Math.Round(req.CurrentPrice, 2),
            MinPrice = Math.Round(req.MinPrice, 2),
            MaxPrice = Math.Round(req.MaxPrice, 2),
            StockQuantity = req.StockQuantity,
            IsActive = true,
            Inventory = new InventoryRecord { Quantity = req.StockQuantity }
        };

        _db.Drinks.Add(drink);
        await _db.SaveChangesAsync();

        var created = await Project(_db.Drinks.AsNoTracking().Where(d => d.Id == drink.Id)).FirstAsync();
        return CreatedAtAction(nameof(GetById), new { id = drink.Id }, created);
    }

    /// <summary>Edit name, category, price limits or active flag. Stock changes go through /restock.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<DrinkResponse>> Update(int id, UpdateDrinkRequest req)
    {
        var drink = await _db.Drinks.FirstOrDefaultAsync(d => d.Id == id);
        if (drink is null) return NotFound();

        var name = req.Name.Trim();
        var min = Math.Round(req.MinPrice, 2);
        var max = Math.Round(req.MaxPrice, 2);

        var error = ValidateRange(min, max);
        if (error is not null) return Problem(detail: error, statusCode: 400);

        if (await _db.Drinks.AnyAsync(d => d.Name == name && d.Id != id))
            return Problem(detail: $"A drink called '{name}' already exists.", statusCode: 409);

        drink.Name = name;
        drink.Category = req.Category.Trim();
        drink.MinPrice = min;
        drink.MaxPrice = max;
        drink.IsActive = req.IsActive;

        // If the new limits leave the current price outside them, pull it back in and log it.
        var clamped = Math.Clamp(drink.CurrentPrice, min, max);
        if (clamped != drink.CurrentPrice)
        {
            _db.PriceHistories.Add(new PriceHistory
            {
                DrinkId = id,
                OldPrice = drink.CurrentPrice,
                NewPrice = clamped,
                CurrentPrice = clamped,
                Reason = PriceChangeReasons.ManualOverride
            });
            drink.CurrentPrice = clamped;
        }

        await _db.SaveChangesAsync();
        return await GetById(id);
    }

    /// <summary>Manually set the price (must stay between the drink's min and max).</summary>
    [HttpPut("{id:int}/price")]
    public async Task<ActionResult<DrinkResponse>> SetPrice(int id, SetPriceRequest req)
    {
        var drink = await _db.Drinks.FirstOrDefaultAsync(d => d.Id == id);
        if (drink is null) return NotFound();

        var price = Math.Round(req.NewPrice, 2);
        if (price < drink.MinPrice || price > drink.MaxPrice)
            return Problem(
                detail: $"Price must be between {drink.MinPrice} and {drink.MaxPrice} for {drink.Name}.",
                statusCode: 400);

        if (price != drink.CurrentPrice)
        {
            _db.PriceHistories.Add(new PriceHistory
            {
                DrinkId = id,
                OldPrice = drink.CurrentPrice,
                NewPrice = price,
                CurrentPrice = price,
                Reason = PriceChangeReasons.ManualOverride
            });
            drink.CurrentPrice = price;
            await _db.SaveChangesAsync();
        }

        return await GetById(id);
    }

    /// <summary>Add stock. Safe to call while orders are being placed.</summary>
    [HttpPost("{id:int}/restock")]
    public async Task<ActionResult<DrinkResponse>> Restock(int id, RestockRequest req)
    {
        await using var tx = await _db.Database.BeginTransactionAsync();

        var rows = await _db.Database.ExecuteSqlInterpolatedAsync(
            $"UPDATE Drink SET stock_quantity = stock_quantity + {req.Quantity} WHERE drink_ID = {id}");

        if (rows == 0)
        {
            await tx.RollbackAsync();
            return NotFound();
        }

        await InventorySync.SyncAsync(_db, id);
        await tx.CommitAsync();

        return await GetById(id);
    }

    private static string? ValidateRange(decimal min, decimal max, decimal? current = null)
    {
        if (min > max) return "MinPrice cannot be higher than MaxPrice.";
        if (current is not null && (current < min || current > max))
            return "CurrentPrice must be between MinPrice and MaxPrice.";
        return null;
    }
}
