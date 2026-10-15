namespace LiquidStock.Api.Models;

/// <summary>Maps to the Drink table.</summary>
public class Drink
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;

    public decimal CurrentPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }

    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }

    public InventoryRecord? Inventory { get; set; }

    public List<OrderItem> OrderItems { get; set; } = new();

    public List<PriceHistory> PriceHistory { get; set; } = new();
}
