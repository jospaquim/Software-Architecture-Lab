using AutoMapper;
using DDD.Sales.Domain.Aggregates.OrderAggregate;
using DDD.Sales.Application.DTOs;

namespace DDD.Sales.Application.Mapping;

public class OrderMappingProfile : Profile
{
    public OrderMappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id.Value))
            .ForMember(dest => dest.CustomerId, opt => opt.MapFrom(src => src.CustomerId.Value))
            .ForMember(dest => dest.OrderNumber, opt => opt.MapFrom(src => src.OrderNumber))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status.ToString()))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total.Amount))
            .ForMember(dest => dest.Currency, opt => opt.MapFrom(src => src.Total.Currency))
            .ForMember(dest => dest.Items, opt => opt.MapFrom(src => src.Items))
            .ForMember(dest => dest.ShippingAddress, opt => opt.MapFrom(src => src.ShippingAddress))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedDate));

        CreateMap<OrderItem, OrderItemDto>()
            .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId.Value))
            .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.ProductName))
            .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
            .ForMember(dest => dest.UnitPrice, opt => opt.MapFrom(src => src.UnitPrice.Amount))
            .ForMember(dest => dest.Total, opt => opt.MapFrom(src => src.Total.Amount));

        CreateMap<Address, AddressDto>()
            .ForMember(dest => dest.Street, opt => opt.MapFrom(src => src.Street))
            .ForMember(dest => dest.City, opt => opt.MapFrom(src => src.City))
            .ForMember(dest => dest.State, opt => opt.MapFrom(src => src.State))
            .ForMember(dest => dest.Country, opt => opt.MapFrom(src => src.Country))
            .ForMember(dest => dest.ZipCode, opt => opt.MapFrom(src => src.ZipCode));
    }
}
