using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;
using Mapster;

namespace CleanArchitecture.Application.Mapping;

/// <summary>
/// Mapster configuration for mapping between domain entities and DTOs
/// </summary>
public static class MappingConfig
{
    public static void ConfigureGlobalMappings()
    {
        var config = TypeAdapterConfig.GlobalSettings;

        // Customer mappings
        config.NewConfig<Customer, CustomerDto>()
            .Map(dest => dest.FullName, src => src.GetFullName())
            .Map(dest => dest.Age, src => src.GetAge());

        config.NewConfig<Customer, CustomerDetailsDto>()
            .Map(dest => dest.FullName, src => src.GetFullName())
            .Map(dest => dest.Age, src => src.GetAge())
            .Ignore(dest => dest.Addresses)
            .Ignore(dest => dest.RecentOrders);

        config.NewConfig<CreateCustomerDto, Customer>();
        config.NewConfig<UpdateCustomerDto, Customer>();

        // Address mappings
        config.NewConfig<Address, AddressDto>()
            .Map(dest => dest.FullAddress, src => src.GetFullAddress());

        config.NewConfig<CreateAddressDto, Address>();

        // Product mappings
        config.NewConfig<Product, ProductDto>()
            .Map(dest => dest.IsInStock, src => src.IsInStock())
            .Map(dest => dest.CategoryUid, src => src.Category.Uid)
            .Map(dest => dest.CategoryName, src => src.Category.Name);

        config.NewConfig<Product, ProductDetailsDto>()
            .Map(dest => dest.IsInStock, src => src.IsInStock())
            .Map(dest => dest.CategoryUid, src => src.Category.Uid)
            .Map(dest => dest.CategoryName, src => src.Category.Name);

        config.NewConfig<CreateProductDto, Product>();
        config.NewConfig<UpdateProductDto, Product>();

        // Category mappings
        config.NewConfig<Category, CategoryDto>()
            .Map(dest => dest.ProductCount, src => src.Products.Count);

        config.NewConfig<CreateCategoryDto, Category>();

        // Order mappings
        config.NewConfig<Order, OrderDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.PaymentStatus, src => src.PaymentStatus.ToString())
            .Map(dest => dest.PaymentMethod, src => src.PaymentMethod.ToString())
            .Map(dest => dest.CustomerUid, src => src.Customer.Uid)
            .Map(dest => dest.CustomerName, src => src.Customer.GetFullName())
            .Map(dest => dest.ItemCount, src => src.Items.Count);

        config.NewConfig<Order, OrderDetailsDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.PaymentStatus, src => src.PaymentStatus.ToString())
            .Map(dest => dest.PaymentMethod, src => src.PaymentMethod.ToString())
            .Map(dest => dest.CustomerUid, src => src.Customer.Uid)
            .Map(dest => dest.CustomerName, src => src.Customer.GetFullName())
            .Map(dest => dest.ItemCount, src => src.Items.Count);

        config.NewConfig<Order, OrderSummaryDto>()
            .Map(dest => dest.Status, src => src.Status.ToString())
            .Map(dest => dest.ItemCount, src => src.Items.Count);

        config.NewConfig<OrderItem, OrderItemDto>()
            .Map(dest => dest.ProductUid, src => src.Product.Uid)
            .Map(dest => dest.TotalPrice, src => src.GetTotalPrice());

        config.NewConfig<CreateOrderDto, Order>();
        config.NewConfig<CreateOrderItemDto, OrderItem>();

    }
}
