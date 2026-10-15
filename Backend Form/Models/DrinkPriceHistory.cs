using System;
using System.Collections.Generic;
using System.Text;

namespace Backend_Form.Models
{
    // Renamed to avoid colliding with LiquidStock.Api.Models.PriceHistory
    public class DrinkPriceHistory
    {
        public int Id { get; set; }

        public int DrinkId { get; set; }

        public decimal OldPrice { get; set; }

        public decimal NewPrice { get; set; }

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;

        public string Reason { get; set; } = string.Empty;
    }
}
