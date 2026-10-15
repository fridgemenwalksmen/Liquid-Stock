namespace LiquidStock.Api.Models;

/// <summary>Maps to the Inventory table (one row per drink).</summary>
public class InventoryRecord
{
    public int Id { get; set; }

    public int DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    public int Quantity { get; set; }

    public DateTime LastUpdated { get; set; }
}
