namespace EDA.ReadModel;

/// <summary>
/// Read Model for Orders - Denormalized for fast queries
/// Updated by Projections when events occur
/// </summary>
public class OrderReadModel
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public DateTime? ShippedAt { get; set; }
    public List<OrderItemReadModel> Items { get; set; } = new();
}

public class OrderItemReadModel
{
    public Guid ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal TotalPrice { get; set; }
}

/// <summary>
/// Repository for Read Model
/// Can use different DB than Write Model (e.g., MongoDB, Redis)
/// </summary>
public interface IOrderReadModelRepository
{
    Task<OrderReadModel?> GetByIdAsync(Guid orderId);
    Task<IEnumerable<OrderReadModel>> GetByCustomerAsync(Guid customerId);
    Task<IEnumerable<OrderReadModel>> GetByStatusAsync(string status);
    Task<IEnumerable<OrderReadModel>> GetAllAsync(int skip, int take);
    Task SaveAsync(OrderReadModel model);
    Task UpdateAsync(OrderReadModel model);
    Task DeleteAsync(Guid orderId);
}

/// <summary>
/// In-Memory implementation for demo
/// </summary>
public class InMemoryOrderReadModelRepository : IOrderReadModelRepository
{
    private readonly Dictionary<Guid, OrderReadModel> _orders = new();

    public Task<OrderReadModel?> GetByIdAsync(Guid orderId)
    {
        _orders.TryGetValue(orderId, out var order);
        return Task.FromResult(order);
    }

    public Task<IEnumerable<OrderReadModel>> GetByCustomerAsync(Guid customerId)
    {
        var orders = _orders.Values.Where(o => o.CustomerId == customerId).ToList();
        return Task.FromResult<IEnumerable<OrderReadModel>>(orders);
    }

    public Task<IEnumerable<OrderReadModel>> GetByStatusAsync(string status)
    {
        var orders = _orders.Values.Where(o => o.Status == status).ToList();
        return Task.FromResult<IEnumerable<OrderReadModel>>(orders);
    }

    public Task<IEnumerable<OrderReadModel>> GetAllAsync(int skip, int take)
    {
        var orders = _orders.Values.Skip(skip).Take(take).ToList();
        return Task.FromResult<IEnumerable<OrderReadModel>>(orders);
    }

    public Task SaveAsync(OrderReadModel model)
    {
        _orders[model.Id] = model;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrderReadModel model)
    {
        _orders[model.Id] = model;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid orderId)
    {
        _orders.Remove(orderId);
        return Task.CompletedTask;
    }
}
