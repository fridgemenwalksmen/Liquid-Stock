namespace LiquidStock.Api.Models;

public class Order
{
    public int Id { get; set; }

    /// <summary>Null = walk-in. Needs SchemaChanges.sql (recommended part) to be applied.</summary>
    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    /// <summary>Null = placed by the customer directly. Needs SchemaChanges.sql (recommended part).</summary>
    public int? StaffId { get; set; }
    public Staff? Staff { get; set; }

    /// <summary>Set by the database clock (SYSDATETIME) when left unset.</summary>
    public DateTime OrderDate { get; set; }

    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; } = OrderStatus.Pending;

    public List<OrderItem> Items { get; set; } = new();
}
