using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using CleanArchitecture.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<Customer?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(c => c.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetVipCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsVip && c.IsActive)
            .OrderBy(c => c.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetActiveCustomersAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Where(c => c.IsActive)
            .OrderBy(c => c.LastName)
            .ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetCustomerWithOrdersAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Orders)
            .ThenInclude(o => o.Items)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    public async Task<Customer?> GetCustomerWithAddressesAsync(int customerId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
            .Include(c => c.Addresses)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
    }

    public async Task<bool> EmailExistsAsync(string email, int? excludeCustomerId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(c => c.Email == email);

        if (excludeCustomerId.HasValue)
        {
            query = query.Where(c => c.Id != excludeCustomerId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
