namespace CleanArchitecture.Domain.Interfaces;

/// <summary>
/// Unit of Work pattern
/// Manages transactions and coordinates the work of multiple repositories
/// Ensures atomic operations across multiple aggregates
/// </summary>
public interface IUnitOfWork : IDisposable
{
    // Repositories
    ICustomerRepository Customers { get; }
    IProductRepository Products { get; }
    IOrderRepository Orders { get; }
    ICategoryRepository Categories { get; }
    IAddressRepository Addresses { get; }

    // Transaction management
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}
