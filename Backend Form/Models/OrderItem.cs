namespace LiquidStock.Api.Models;

public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public int DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Price per drink at the moment of purchase.</summary>
    public decimal UnitPrice { get; set; }

    /// <summary>Computed by the database (quantity * unit_price). Read-only here.</summary>
    public decimal Subtotal { get; private set; }
}
