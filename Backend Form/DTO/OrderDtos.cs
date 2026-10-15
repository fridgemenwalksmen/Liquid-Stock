using System.ComponentModel.DataAnnotations;
using LiquidStock.Api.Models;

namespace LiquidStock.Api.Dtos;

public class OrderLineRequest
{
    [Range(1, int.MaxValue)]
    public int DrinkId { get; set; }

    [Range(1, 1000)]
    public int Quantity { get; set; }
}

public class CreateOrderRequest
{
    /// <summary>Leave out for a walk-in customer.</summary>
    public int? CustomerId { get; set; }

    /// <summary>The staff member taking the order. Leave out if the customer orders directly.</summary>
    public int? StaffId { get; set; }

    /// <summary>true = order is marked Completed straight away. false = it starts as Pending.</summary>
    public bool CompleteImmediately { get; set; }

    [Required, MinLength(1)]
    public List<OrderLineRequest> Items { get; set; } = new();
}

public class UpdateOrderStatusRequest
{
    public OrderStatus Status { get; set; }
}

public class OrderItemResponse
{
    public int DrinkId { get; set; }
    public string DrinkName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Subtotal { get; set; }
}

public class OrderResponse
{
    public int Id { get; set; }
    public int? CustomerId { get; set; }
    public int? StaffId { get; set; }
    public DateTime OrderDate { get; set; }
    public decimal TotalAmount { get; set; }
    public OrderStatus Status { get; set; }
    public List<OrderItemResponse> Items { get; set; } = new();

    public static OrderResponse From(Order o, IReadOnlyDictionary<int, string> drinkNames) => new()
    {
        Id = o.Id,
        CustomerId = o.CustomerId,
        StaffId = o.StaffId,
        OrderDate = o.OrderDate,
        TotalAmount = o.TotalAmount,
        Status = o.Status,
        Items = o.Items.Select(i => new OrderItemResponse
        {
            DrinkId = i.DrinkId,
            DrinkName = drinkNames.TryGetValue(i.DrinkId, out var n) ? n : string.Empty,
            Quantity = i.Quantity,
            UnitPrice = i.UnitPrice,
            Subtotal = i.UnitPrice * i.Quantity
        }).ToList()
    };
}
