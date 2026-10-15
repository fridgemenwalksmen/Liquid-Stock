using System.ComponentModel.DataAnnotations;

namespace LiquidStock.Api.Dtos;

public class DrinkResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal MinPrice { get; set; }
    public decimal MaxPrice { get; set; }
    public int StockQuantity { get; set; }
    public bool IsActive { get; set; }

    /// <summary>The price before the most recent change (null if the price never changed).</summary>
    public decimal? PreviousPrice { get; set; }

    /// <summary>"Up", "Down", "Same" or "None". Handy for showing an arrow on the screen.</summary>
    public string Trend =>
        PreviousPrice is null ? "None" :
        CurrentPrice > PreviousPrice ? "Up" :
        CurrentPrice < PreviousPrice ? "Down" : "Same";
}

public class CreateDrinkRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100000")]
    public decimal CurrentPrice { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal MinPrice { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal MaxPrice { get; set; }

    [Range(0, 1000000)]
    public int StockQuantity { get; set; }
}

public class UpdateDrinkRequest
{
    [Required, StringLength(100, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Range(typeof(decimal), "0", "100000")]
    public decimal MinPrice { get; set; }

    [Range(typeof(decimal), "0", "100000")]
    public decimal MaxPrice { get; set; }

    public bool IsActive { get; set; } = true;
}

public class SetPriceRequest
{
    [Range(typeof(decimal), "0", "100000")]
    public decimal NewPrice { get; set; }
}

public class RestockRequest
{
    [Range(1, 1000000)]
    public int Quantity { get; set; }
}

public class PriceHistoryResponse
{
    public int Id { get; set; }
    public int DrinkId { get; set; }
    public decimal OldPrice { get; set; }
    public decimal NewPrice { get; set; }
    public DateTime ChangedAt { get; set; }
    public string? Reason { get; set; }
}
