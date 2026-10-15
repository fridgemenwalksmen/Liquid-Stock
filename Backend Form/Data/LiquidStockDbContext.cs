using Backend_Form.Models;
using LiquidStock.Api.Models;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Emit;

namespace LiquidStock.Api.Data;

/// <summary>
/// Database-first: the tables are created by SQL scripts (see /Database), NOT by EF migrations.
/// This class only maps our C# models onto the existing tables and columns.
/// </summary>
public class LiquidStockDbContext : DbContext
{
    public LiquidStockDbContext(DbContextOptions<LiquidStockDbContext> options) : base(options) { }

    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Staff> Staff => Set<Staff>();
    public DbSet<Drink> Drinks => Set<Drink>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<InventoryRecord> InventoryRecords => Set<InventoryRecord>();
    public DbSet<PriceHistory> PriceHistories => Set<PriceHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ---------- Customer ----------
        modelBuilder.Entity<Customer>(e =>
        {
            e.ToTable("Customer");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("customer_ID");
            e.Property(x => x.FullName).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Email).HasColumnName("email").HasMaxLength(255);
            e.Property(x => x.PhoneNumber).HasColumnName("phone_number").HasMaxLength(30);
            e.Property(x => x.DateJoined).HasColumnName("date_joined")
                .HasColumnType("date")
                .HasDefaultValueSql("CAST(GETDATE() AS DATE)");
        });

        // ---------- Staff ----------
        modelBuilder.Entity<Staff>(e =>
        {
            e.ToTable("Staff");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("staff_ID");
            e.Property(x => x.FullName).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Role).HasColumnName("role").HasMaxLength(50);
            e.Property(x => x.Username).HasColumnName("username").HasMaxLength(100);
            e.Property(x => x.PasswordHash).HasColumnName("password_hash").HasMaxLength(255);
            e.Property(x => x.IsActive).HasColumnName("is_active");
        });

        // ---------- Drink ----------
        modelBuilder.Entity<Drink>(e =>
        {
            e.ToTable("Drink");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("drink_ID");
            e.Property(x => x.Name).HasColumnName("name").HasMaxLength(100);
            e.Property(x => x.Category).HasColumnName("category").HasMaxLength(100);
            e.Property(x => x.CurrentPrice).HasColumnName("current_price").HasPrecision(10, 2);
            e.Property(x => x.MinPrice).HasColumnName("min_price").HasPrecision(10, 2);
            e.Property(x => x.MaxPrice).HasColumnName("max_price").HasPrecision(10, 2);
            e.Property(x => x.StockQuantity).HasColumnName("stock_quantity");
            e.Property(x => x.IsActive).HasColumnName("is_active");
        });

        // ---------- Order ----------
        modelBuilder.Entity<Order>(e =>
        {
            e.ToTable("Order");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("order_ID");
            e.Property(x => x.CustomerId).HasColumnName("customer_ID");
            e.Property(x => x.StaffId).HasColumnName("staff_ID");
            e.Property(x => x.OrderDate).HasColumnName("order_date")
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.TotalAmount).HasColumnName("total_amount").HasPrecision(12, 2);
            e.Property(x => x.Status).HasColumnName("status")
                .HasConversion<string>().HasMaxLength(30);

            e.HasOne(x => x.Customer).WithMany(c => c.Orders)
                .HasForeignKey(x => x.CustomerId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Staff).WithMany(s => s.Orders)
                .HasForeignKey(x => x.StaffId).IsRequired(false)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Order_Item ----------
        modelBuilder.Entity<OrderItem>(e =>
        {
            e.ToTable("Order_Item");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("order_item_ID");
            e.Property(x => x.OrderId).HasColumnName("order_ID");
            e.Property(x => x.DrinkId).HasColumnName("drink_ID");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.UnitPrice).HasColumnName("unit_price").HasPrecision(10, 2);

            // subtotal is a PERSISTED computed column in SQL Server: EF reads it, never writes it.
            e.Property(x => x.Subtotal).HasColumnName("subtotal").HasPrecision(12, 2)
                .ValueGeneratedOnAddOrUpdate();

            e.HasOne(x => x.Order).WithMany(o => o.Items)
                .HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);

            e.HasOne(x => x.Drink).WithMany(d => d.OrderItems)
                .HasForeignKey(x => x.DrinkId).OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- Inventory ----------
        modelBuilder.Entity<InventoryRecord>(e =>
        {
            e.ToTable("Inventory");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("inventory_ID");
            e.Property(x => x.DrinkId).HasColumnName("drink_ID");
            e.Property(x => x.Quantity).HasColumnName("quantity");
            e.Property(x => x.LastUpdated).HasColumnName("last_updated")
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");

            // drink_ID is UNIQUE in the database, so this is one-to-one.
            e.HasOne(x => x.Drink).WithOne(d => d.Inventory)
                .HasForeignKey<InventoryRecord>(x => x.DrinkId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // ---------- History (price history) ----------
        modelBuilder.Entity<PriceHistory>(e =>
        {
            e.ToTable("History");
            e.HasKey(x => x.Id);
            e.Property(x => x.Id).HasColumnName("history_ID");
            e.Property(x => x.DrinkId).HasColumnName("drink_ID");
            e.Property(x => x.CurrentPrice).HasColumnName("current_price").HasPrecision(10, 2);
            e.Property(x => x.OldPrice).HasColumnName("old_price").HasPrecision(10, 2);
            e.Property(x => x.NewPrice).HasColumnName("new_price").HasPrecision(10, 2);

            // Added by SchemaChanges.sql
            e.Property(x => x.ChangedAt).HasColumnName("changed_at")
                .HasColumnType("datetime2")
                .HasDefaultValueSql("SYSDATETIME()");
            e.Property(x => x.Reason).HasColumnName("reason").HasMaxLength(30);

            e.HasOne(x => x.Drink).WithMany(d => d.PriceHistory)
                .HasForeignKey(x => x.DrinkId).OnDelete(DeleteBehavior.Restrict);
        });
    }
}
