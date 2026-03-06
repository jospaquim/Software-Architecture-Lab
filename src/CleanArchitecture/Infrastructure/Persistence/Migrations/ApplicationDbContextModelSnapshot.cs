using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using CleanArchitecture.Infrastructure.Persistence;

namespace CleanArchitecture.Infrastructure.Persistence.Migrations
{
    [DbContext(typeof(ApplicationDbContext))]
    partial class ApplicationDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder.HasAnnotation("ProductVersion", "8.0.0");

            modelBuilder.Entity("CleanArchitecture.Domain.Entities.Customer", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd();
                b.Property<string>("FirstName").IsRequired().HasMaxLength(100);
                b.Property<string>("LastName").IsRequired().HasMaxLength(100);
                b.Property<string>("Email").IsRequired().HasMaxLength(256);
                b.Property<string>("Phone").HasMaxLength(20);
                b.Property<DateTime>("CreatedAt");
                b.Property<DateTime?>("UpdatedAt");

                b.HasKey("Id");
                b.HasIndex("Email").IsUnique();

                b.ToTable("Customers");

                b.OwnsOne("CleanArchitecture.Domain.Entities.Address", "Address", b1 =>
                {
                    b1.Property<string>("Street").HasColumnName("Address_Street").HasMaxLength(200);
                    b1.Property<string>("City").HasColumnName("Address_City").HasMaxLength(100);
                    b1.Property<string>("State").HasColumnName("Address_State").HasMaxLength(50);
                    b1.Property<string>("Country").HasColumnName("Address_Country").HasMaxLength(100);
                    b1.Property<string>("PostalCode").HasColumnName("Address_PostalCode").HasMaxLength(20);
                });
            });

            modelBuilder.Entity("CleanArchitecture.Domain.Entities.Product", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd();
                b.Property<string>("Name").IsRequired().HasMaxLength(200);
                b.Property<string>("Description").HasMaxLength(1000);
                b.Property<string>("SKU").IsRequired().HasMaxLength(50);
                b.Property<decimal>("Price").HasColumnType("decimal(18,2)");
                b.Property<int>("Stock");
                b.Property<bool>("IsActive").HasDefaultValue(true);
                b.Property<DateTime>("CreatedAt");
                b.Property<DateTime?>("UpdatedAt");

                b.HasKey("Id");
                b.HasIndex("SKU").IsUnique();

                b.ToTable("Products");
            });

            modelBuilder.Entity("CleanArchitecture.Domain.Entities.Order", b =>
            {
                b.Property<Guid>("Id").ValueGeneratedOnAdd();
                b.Property<string>("OrderNumber").IsRequired().HasMaxLength(50);
                b.Property<Guid>("CustomerId");
                b.Property<DateTime>("OrderDate");
                b.Property<int>("Status").HasDefaultValue(0);
                b.Property<decimal>("TotalAmount").HasColumnType("decimal(18,2)");
                b.Property<DateTime>("CreatedAt");
                b.Property<DateTime?>("UpdatedAt");

                b.HasKey("Id");
                b.HasIndex("CustomerId");
                b.HasIndex("OrderNumber").IsUnique();
                b.HasIndex("OrderDate");
                b.HasIndex("Status");

                b.ToTable("Orders");

                b.HasOne("CleanArchitecture.Domain.Entities.Customer", "Customer")
                    .WithMany("Orders")
                    .HasForeignKey("CustomerId")
                    .OnDelete(DeleteBehavior.Cascade)
                    .IsRequired();

                b.OwnsOne("CleanArchitecture.Domain.Entities.Address", "ShippingAddress", b1 =>
                {
                    b1.Property<string>("Street").HasColumnName("ShippingAddress_Street").HasMaxLength(200);
                    b1.Property<string>("City").HasColumnName("ShippingAddress_City").HasMaxLength(100);
                    b1.Property<string>("State").HasColumnName("ShippingAddress_State").HasMaxLength(50);
                    b1.Property<string>("Country").HasColumnName("ShippingAddress_Country").HasMaxLength(100);
                    b1.Property<string>("PostalCode").HasColumnName("ShippingAddress_PostalCode").HasMaxLength(20);
                });

                b.OwnsMany("CleanArchitecture.Domain.Entities.OrderItem", "Items", b1 =>
                {
                    b1.Property<Guid>("Id").ValueGeneratedOnAdd();
                    b1.Property<Guid>("OrderId");
                    b1.Property<Guid>("ProductId");
                    b1.Property<string>("ProductName").IsRequired().HasMaxLength(200);
                    b1.Property<int>("Quantity");
                    b1.Property<decimal>("UnitPrice").HasColumnType("decimal(18,2)");

                    b1.HasKey("Id");
                    b1.HasIndex("OrderId");
                    b1.HasIndex("ProductId");

                    b1.ToTable("OrderItems");

                    b1.WithOwner().HasForeignKey("OrderId");

                    b1.HasOne("CleanArchitecture.Domain.Entities.Product", "Product")
                        .WithMany()
                        .HasForeignKey("ProductId")
                        .OnDelete(DeleteBehavior.Restrict)
                        .IsRequired();
                });
            });
        }
    }
}
