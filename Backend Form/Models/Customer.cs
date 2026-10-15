using Backend_Form.Models;

namespace LiquidStock.Api.Models;

public class Customer
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? PhoneNumber { get; set; }

    /// <summary>Set by the database default when left unset.</summary>
    public DateOnly DateJoined { get; set; }

    public List<Order> Orders { get; set; } = new();
}
