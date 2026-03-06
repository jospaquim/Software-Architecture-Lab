using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Customer-specific repository interface
/// Extends generic repository with customer-specific operations
/// </summary>
public interface ICustomerRepository : IRepository<Customer>
{
    Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetVipCustomersAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerWithOrdersAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Customer?> GetCustomerWithAddressesAsync(int customerId, CancellationToken cancellationToken = default);
    Task<bool> EmailExistsAsync(string email, int? excludeCustomerId = null, CancellationToken cancellationToken = default);
}

public interface IProductRepository : IRepository<Product>
{
    Task<Product?> GetBySkuAsync(string sku, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetByCategoryAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> GetLowStockProductsAsync(int threshold = 10, CancellationToken cancellationToken = default);
    Task<IEnumerable<Product>> SearchProductsAsync(string searchTerm, CancellationToken cancellationToken = default);
    Task<bool> SkuExistsAsync(string sku, int? excludeProductId = null, CancellationToken cancellationToken = default);
}

public interface IOrderRepository : IRepository<Order>
{
    Task<Order?> GetByOrderNumberAsync(string orderNumber, CancellationToken cancellationToken = default);
    Task<Order?> GetOrderWithItemsAsync(int orderId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetCustomerOrdersAsync(int customerId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByStatusAsync(Enums.OrderStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetPendingPaymentOrdersAsync(CancellationToken cancellationToken = default);
    Task<decimal> GetTotalRevenueAsync(DateTime? startDate = null, DateTime? endDate = null, CancellationToken cancellationToken = default);
}

public interface ICategoryRepository : IRepository<Category>
{
    Task<Category?> GetByNameAsync(string name, CancellationToken cancellationToken = default);
    Task<Category?> GetCategoryWithProductsAsync(int categoryId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Category>> GetActiveCategoriesAsync(CancellationToken cancellationToken = default);
}

public interface IAddressRepository : IRepository<Address>
{
    Task<IEnumerable<Address>> GetCustomerAddressesAsync(int customerId, CancellationToken cancellationToken = default);
    Task<Address?> GetDefaultAddressAsync(int customerId, CancellationToken cancellationToken = default);
}
