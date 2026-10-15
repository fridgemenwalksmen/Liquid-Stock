namespace LiquidStock.Api.Models;

/// <summary>Maps to the History table. ChangedAt and Reason need SchemaChanges.sql (required part).</summary>
public class PriceHistory
{
    public int Id { get; set; }

    public int DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    /// <summary>The History table also stores current_price; we always set it equal to NewPrice.</summary>
    public decimal CurrentPrice { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }

    /// <summary>Set by the database clock when left unset.</summary>
    public DateTime ChangedAt { get; set; }

    /// <summary>One of PriceChangeReasons.*</summary>
    public string? Reason { get; set; }
}
