using AutoMapper;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Enums;

namespace CleanArchitecture.Application.Mapping;

/// <summary>
/// AutoMapper profile for mapping between domain entities and DTOs
/// Keeps mapping logic centralized and maintainable
/// </summary>
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        // Customer mappings
        CreateMap<Customer, CustomerDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.GetFullName()))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()));

        CreateMap<Customer, CustomerDetailsDto>()
            .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => src.GetFullName()))
            .ForMember(dest => dest.Age, opt => opt.MapFrom(src => src.GetAge()))
            .ForMember(dest => dest.Addresses, opt => opt.Ignore())
            .ForMember(dest => dest.RecentOrders, opt => opt.Ignore());

        CreateMap<CreateCustomerDto, Customer>();
        CreateMap<UpdateCustomerDto, Customer>();

        // Address mappings
        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.FullAddress, opt => opt.MapFrom(src => src.GetFullAddress()));

        CreateMap<CreateAddressDto, Address>();

        // Product mappings
        CreateMap<Product, ProductDto>()
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.IsInStock()))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

        CreateMap<Product, ProductDetailsDto>()
            .ForMember(dest => dest.IsInStock, opt => opt.MapFrom(src => src.IsInStock()))
            .ForMember(dest => dest.CategoryName, opt => opt.MapFrom(src => src.Category.Name));

        CreateMap<CreateProductDto, Product>();
        CreateMap<UpdateProductDto, Product>();

        // Category mappings
        CreateMap<Category, CategoryDto>()
            .ForMember(dest => dest.ProductCount, opt => opt.MapFrom(src => src.Products.Count));

        CreateMap<CreateCategoryDto, Category>();

        // Order mappings
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.GetFullName()))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<Order, OrderDetailsDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.PaymentStatus, opt => opt.MapFrom(src => src.PaymentStatus.ToString()))
            .ForMember(dest => dest.PaymentMethod, opt => opt.MapFrom(src => src.PaymentMethod.ToString()))
            .ForMember(dest => dest.CustomerName, opt => opt.MapFrom(src => src.Customer.GetFullName()))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<Order, OrderSummaryDto>()
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.ItemCount, opt => opt.MapFrom(src => src.Items.Count));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.TotalPrice, opt => opt.MapFrom(src => src.GetTotalPrice()));

        CreateMap<CreateOrderDto, Order>();
        CreateMap<CreateOrderItemDto, OrderItem>();
    }
}
