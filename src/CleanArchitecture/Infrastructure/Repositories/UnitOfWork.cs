using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Storage;

namespace CleanArchitecture.Infrastructure.Repositories;

/// <summary>
/// Unit of Work implementation
/// Manages transactions and coordinates repositories
/// Ensures atomic operations across multiple aggregates
/// Implements Dependency Inversion Principle
/// </summary>
public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _context;
    private IDbContextTransaction? _transaction;

    // Lazy initialization of repositories
    private ICustomerRepository? _customers;
    private IProductRepository? _products;
    private IOrderRepository? _orders;
    private ICategoryRepository? _categories;
    private IAddressRepository? _addresses;

    public UnitOfWork(ApplicationDbContext context)
    {
        _context = context;
    }

    public ICustomerRepository Customers =>
        _customers ??= new CustomerRepository(_context);

    public IProductRepository Products =>
        _products ??= new ProductRepository(_context);

    public IOrderRepository Orders =>
        _orders ??= new OrderRepository(_context);

    public ICategoryRepository Categories =>
        _categories ??= new CategoryRepository(_context);

    public IAddressRepository Addresses =>
        _addresses ??= new AddressRepository(_context);

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await _context.SaveChangesAsync(cancellationToken);

            if (_transaction != null)
            {
                await _transaction.CommitAsync(cancellationToken);
            }
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync(cancellationToken);
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public void Dispose()
    {
        _transaction?.Dispose();
        _context.Dispose();
    }
}
