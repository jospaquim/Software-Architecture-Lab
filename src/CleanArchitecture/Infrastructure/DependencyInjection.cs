using CleanArchitecture.Application.Common;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Auth;
using CleanArchitecture.Infrastructure.Messaging.Consumers;
using CleanArchitecture.Infrastructure.Persistence;
using CleanArchitecture.Infrastructure.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CleanArchitecture.Infrastructure;

/// <summary>
/// Dependency Injection configuration for Infrastructure layer
/// Registers all infrastructure services, repositories, and DbContext
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Database Context
        var databaseProvider = configuration.GetValue<string>("DatabaseProvider")
            ?? throw new InvalidOperationException("CRITICAL: DatabaseProvider is missing. Configure it in appsettings.json (SqlServer or PostgreSQL).");
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("CRITICAL: ConnectionStrings:DefaultConnection is missing. Configure it in appsettings.json.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            if (databaseProvider == "PostgreSQL")
            {
                options.UseNpgsql(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            }
            else
            {
                options.UseSqlServer(
                    connectionString,
                    b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
            }

            // Enable sensitive data logging in development
            if (configuration.GetValue<bool>("DetailedErrors"))
            {
                options.EnableSensitiveDataLogging();
                options.EnableDetailedErrors();
            }
        });

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Repositories (optional if using UnitOfWork)
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IAddressRepository, AddressRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();

        // Auth services
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        // MassTransit + RabbitMQ
        var rabbitHost = configuration["RabbitMQ:Host"]
            ?? throw new InvalidOperationException("CRITICAL: RabbitMQ:Host is missing. Configure it in appsettings.json.");
        var rabbitUser = configuration["RabbitMQ:Username"]
            ?? throw new InvalidOperationException("CRITICAL: RabbitMQ:Username is missing. Configure it in appsettings.json.");
        var rabbitPass = configuration["RabbitMQ:Password"]
            ?? throw new InvalidOperationException("CRITICAL: RabbitMQ:Password is missing. Configure it in appsettings.json.");

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<CustomerCreatedConsumer>();
            bus.AddConsumer<OrderCreatedConsumer>();
            bus.AddConsumer<OrderStatusChangedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(rabbitHost, "/", h =>
                {
                    h.Username(rabbitUser);
                    h.Password(rabbitPass);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
