using DDD.Sales.Domain.Aggregates.Order;
using Microsoft.EntityFrameworkCore;

namespace DDD.Sales.Infrastructure.Persistence;

public class SalesDbContext : DbContext
{
    public SalesDbContext(DbContextOptions<SalesDbContext> options) : base(options)
    {
    }

    public DbSet<Order> Orders => Set<Order>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Order Aggregate Configuration
        modelBuilder.Entity<Order>(entity =>
        {
            entity.ToTable("Orders");

            entity.HasKey(o => o.Id);

            // Value Object: OrderId
            entity.Property(o => o.Id)
                .HasConversion(
                    id => id.Value,
                    value => Domain.ValueObjects.OrderId.CreateFrom(value))
                .HasColumnName("Id");

            // Value Object: CustomerId
            entity.Property(o => o.CustomerId)
                .HasConversion(
                    id => id.Value,
                    value => Domain.ValueObjects.CustomerId.CreateFrom(value))
                .IsRequired();

            entity.Property(o => o.OrderNumber)
                .IsRequired()
                .HasMaxLength(50);

            entity.HasIndex(o => o.OrderNumber)
                .IsUnique();

            entity.Property(o => o.Status)
                .HasConversion<string>()
                .IsRequired();

            // Value Object: Money (Total)
            entity.OwnsOne(o => o.Total, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("TotalAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("TotalCurrency")
                    .HasConversion<string>();
            });

            // Value Object: Money (Subtotal)
            entity.OwnsOne(o => o.Subtotal, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("SubtotalAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("SubtotalCurrency")
                    .HasConversion<string>();
            });

            // Value Object: Money (Tax)
            entity.OwnsOne(o => o.Tax, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("TaxAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("TaxCurrency")
                    .HasConversion<string>();
            });

            // Value Object: Money (Discount)
            entity.OwnsOne(o => o.Discount, money =>
            {
                money.Property(m => m.Amount)
                    .HasColumnName("DiscountAmount")
                    .HasPrecision(18, 2);

                money.Property(m => m.Currency)
                    .HasColumnName("DiscountCurrency")
                    .HasConversion<string>();
            });

            // Value Object: Address
            entity.OwnsOne(o => o.ShippingAddress, address =>
            {
                address.Property(a => a.Street)
                    .HasColumnName("ShippingStreet")
                    .HasMaxLength(200);

                address.Property(a => a.City)
                    .HasColumnName("ShippingCity")
                    .HasMaxLength(100);

                address.Property(a => a.State)
                    .HasColumnName("ShippingState")
                    .HasMaxLength(100);

                address.Property(a => a.Country)
                    .HasColumnName("ShippingCountry")
                    .HasMaxLength(100);

                address.Property(a => a.ZipCode)
                    .HasColumnName("ShippingZipCode")
                    .HasMaxLength(20);
            });

            // OrderItems collection
            entity.OwnsMany(o => o.Items, items =>
            {
                items.ToTable("OrderItems");

                items.WithOwner().HasForeignKey("OrderId");

                items.Property<Guid>("Id");
                items.HasKey("Id");

                items.Property(i => i.ProductId)
                    .HasConversion(
                        id => id.Value,
                        value => Domain.ValueObjects.ProductId.CreateFrom(value));

                items.Property(i => i.ProductName)
                    .IsRequired()
                    .HasMaxLength(200);

                items.OwnsOne(i => i.UnitPrice, money =>
                {
                    money.Property(m => m.Amount)
                        .HasColumnName("UnitPrice")
                        .HasPrecision(18, 2);

                    money.Property(m => m.Currency)
                        .HasColumnName("Currency")
                        .HasConversion<string>();
                });

                items.Property(i => i.Quantity)
                    .IsRequired();
            });

            // Ignore domain events (transient)
            entity.Ignore(o => o.DomainEvents);

            entity.Property(o => o.CreatedAt)
                .IsRequired();

            entity.Property(o => o.ConfirmedAt);
            entity.Property(o => o.ShippedAt);
        });
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Here you could dispatch domain events before saving
        return base.SaveChangesAsync(cancellationToken);
    }
}
