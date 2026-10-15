namespace LiquidStock.Api.Models;

/// <summary>Values match the CHECK constraint on [Order].status exactly.</summary>
public enum OrderStatus
{
    Pending,
    Completed,
    Cancelled
}

/// <summary>Staff.role is free text in the database; these are the values the API cares about.</summary>
public static class StaffRoles
{
    public const string Manager = "Manager";
}

/// <summary>Values stored in History.reason.</summary>
public static class PriceChangeReasons
{
    public const string DemandIncrease = "DemandIncrease";
    public const string IdleDecay = "IdleDecay";
    public const string ManualOverride = "ManualOverride";
}
