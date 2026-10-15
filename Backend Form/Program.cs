using System.Text.Json.Serialization;
using LiquidStock.Api.Data;
using LiquidStock.Api.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Database (connection string comes from user-secrets, see README)
builder.Services.AddDbContext<LiquidStockDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("LiquidStock")));

builder.Services.AddScoped<OrderService>();

// Controllers (enums are sent/received as strings, e.g. "Completed" instead of 1)
builder.Services.AddControllers()
    .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

// Swagger UI so you can test endpoints in the browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

// TEMPORARY: open /api/ping-db to prove the connection and every table/column mapping works.
// Delete once you are happy everything works.
app.MapGet("/api/ping-db", async (LiquidStockDbContext db) =>
{
    await db.Customers.AsNoTracking().Take(1).ToListAsync();
    await db.Staff.AsNoTracking().Take(1).ToListAsync();
    await db.Drinks.AsNoTracking().Take(1).ToListAsync();
    await db.Orders.AsNoTracking().Take(1).ToListAsync();
    await db.OrderItems.AsNoTracking().Take(1).ToListAsync();
    await db.InventoryRecords.AsNoTracking().Take(1).ToListAsync();
    await db.PriceHistories.AsNoTracking().Take(1).ToListAsync();

    return Results.Ok(new
    {
        status = "connected, all 7 tables map correctly",
        customers = await db.Customers.CountAsync(),
        staff = await db.Staff.CountAsync(),
        drinks = await db.Drinks.CountAsync(),
        orders = await db.Orders.CountAsync(),
        orderItems = await db.OrderItems.CountAsync(),
        inventoryRows = await db.InventoryRecords.CountAsync(),
        priceHistoryRows = await db.PriceHistories.CountAsync()
    });
});

app.Run();
