using StackExchange.Redis;
using System.Text.Json;
using EDA.ReadModel;

namespace EDA.Infrastructure.Redis;

/// <summary>
/// Implementación de Read Model usando Redis
/// Redis almacena datos en memoria para consultas ultra rápidas
///
/// VENTAJAS:
/// - Consultas en microsegundos
/// - 100,000+ operaciones/segundo
/// - Estructuras de datos avanzadas
/// - Persistencia opcional
///
/// ESTRUCTURA EN REDIS:
/// - order:{orderId} → JSON del OrderReadModel
/// - customer:orders:{customerId} → Set de orderIds
/// - status:orders:{status} → Set de orderIds
///
/// USO:
/// - Reemplazar InMemoryOrderReadModelRepository en Program.cs
/// - Configurar Redis en appsettings.json
/// </summary>
public class RedisOrderReadModelRepository : IOrderReadModelRepository
{
    private readonly IDatabase _redis;
    private readonly IConnectionMultiplexer _connection;
    private const string OrderPrefix = "order:";
    private const string CustomerOrdersPrefix = "customer:orders:";
    private const string StatusOrdersPrefix = "status:orders:";
    private const string AllOrdersKey = "all:orders";

    public RedisOrderReadModelRepository(IConnectionMultiplexer redis)
    {
        _connection = redis;
        _redis = redis.GetDatabase();
    }

    public async Task<OrderReadModel?> GetByIdAsync(Guid orderId)
    {
        var key = $"{OrderPrefix}{orderId}";
        var json = await _redis.StringGetAsync(key);

        if (json.IsNullOrEmpty)
        {
            Console.WriteLine($"️ Order {orderId} not found in Redis");
            return null;
        }

        var order = JsonSerializer.Deserialize<OrderReadModel>(json!);
        Console.WriteLine($" Order {orderId} retrieved from Redis");
        return order;
    }

    public async Task<IEnumerable<OrderReadModel>> GetByCustomerAsync(Guid customerId)
    {
        // Obtener lista de IDs de órdenes del cliente
        var key = $"{CustomerOrdersPrefix}{customerId}";
        var orderIds = await _redis.SetMembersAsync(key);

        if (orderIds.Length == 0)
        {
            Console.WriteLine($"️ No orders found for customer {customerId}");
            return Enumerable.Empty<OrderReadModel>();
        }

        Console.WriteLine($" Found {orderIds.Length} orders for customer {customerId}");

        // Obtener todas las órdenes en paralelo usando pipeline
        var batch = _redis.CreateBatch();
        var tasks = orderIds
            .Select(id => batch.StringGetAsync($"{OrderPrefix}{id}"))
            .ToArray();

        batch.Execute();
        await Task.WhenAll(tasks);

        var orders = tasks
            .Select(t => t.Result)
            .Where(json => !json.IsNullOrEmpty)
            .Select(json => JsonSerializer.Deserialize<OrderReadModel>(json!))
            .Where(o => o != null)
            .ToList();

        return orders!;
    }

    public async Task<IEnumerable<OrderReadModel>> GetByStatusAsync(string status)
    {
        // Para búsquedas por status, usamos un Set en Redis
        var key = $"{StatusOrdersPrefix}{status}";
        var orderIds = await _redis.SetMembersAsync(key);

        if (orderIds.Length == 0)
        {
            Console.WriteLine($"️ No orders found with status {status}");
            return Enumerable.Empty<OrderReadModel>();
        }

        Console.WriteLine($" Found {orderIds.Length} orders with status {status}");

        // Obtener todas las órdenes en paralelo
        var batch = _redis.CreateBatch();
        var tasks = orderIds
            .Select(id => batch.StringGetAsync($"{OrderPrefix}{id}"))
            .ToArray();

        batch.Execute();
        await Task.WhenAll(tasks);

        var orders = tasks
            .Select(t => t.Result)
            .Where(json => !json.IsNullOrEmpty)
            .Select(json => JsonSerializer.Deserialize<OrderReadModel>(json!))
            .Where(o => o != null)
            .ToList();

        return orders!;
    }

    public async Task<IEnumerable<OrderReadModel>> GetAllAsync(int skip, int take)
    {
        // Usar Sorted Set para paginación eficiente
        // Ordenado por fecha de creación (timestamp como score)
        var orderIds = await _redis.SortedSetRangeByScoreAsync(
            AllOrdersKey,
            skip: skip,
            take: take,
            order: Order.Descending // Más recientes primero
        );

        if (orderIds.Length == 0)
        {
            Console.WriteLine("️ No orders found");
            return Enumerable.Empty<OrderReadModel>();
        }

        Console.WriteLine($" Retrieving {orderIds.Length} orders (skip: {skip}, take: {take})");

        // Obtener órdenes en paralelo
        var batch = _redis.CreateBatch();
        var tasks = orderIds
            .Select(id => batch.StringGetAsync($"{OrderPrefix}{id}"))
            .ToArray();

        batch.Execute();
        await Task.WhenAll(tasks);

        var orders = tasks
            .Select(t => t.Result)
            .Where(json => !json.IsNullOrEmpty)
            .Select(json => JsonSerializer.Deserialize<OrderReadModel>(json!))
            .Where(o => o != null)
            .ToList();

        return orders!;
    }

    public async Task SaveAsync(OrderReadModel model)
    {
        var key = $"{OrderPrefix}{model.Id}";
        var json = JsonSerializer.Serialize(model, new JsonSerializerOptions
        {
            WriteIndented = false // Menos espacio
        });

        // Usar transacción para garantizar atomicidad
        var transaction = _redis.CreateTransaction();

        // 1. Guardar la orden
        _ = transaction.StringSetAsync(key, json);

        // 2. Agregar a índice de customer
        _ = transaction.SetAddAsync($"{CustomerOrdersPrefix}{model.CustomerId}", model.Id.ToString());

        // 3. Agregar a índice de status
        _ = transaction.SetAddAsync($"{StatusOrdersPrefix}{model.Status}", model.Id.ToString());

        // 4. Agregar a sorted set de todas las órdenes (score = timestamp)
        var score = new DateTimeOffset(model.CreatedAt).ToUnixTimeSeconds();
        _ = transaction.SortedSetAddAsync(AllOrdersKey, model.Id.ToString(), score);

        // 5. Opcional: TTL (Time To Live) - expira en 30 días
        // Comentar esta línea si quieres persistencia permanente
        _ = transaction.KeyExpireAsync(key, TimeSpan.FromDays(30));

        // Ejecutar transacción
        var committed = await transaction.ExecuteAsync();

        if (committed)
        {
            Console.WriteLine($" Order {model.Id} saved to Redis");
        }
        else
        {
            Console.WriteLine($" Failed to save order {model.Id} to Redis");
            throw new InvalidOperationException("Failed to save order to Redis");
        }
    }

    public async Task UpdateAsync(OrderReadModel model)
    {
        // Primero, obtener la orden anterior para limpiar índices viejos
        var existingOrder = await GetByIdAsync(model.Id);

        var key = $"{OrderPrefix}{model.Id}";
        var json = JsonSerializer.Serialize(model);

        var transaction = _redis.CreateTransaction();

        // 1. Actualizar la orden
        _ = transaction.StringSetAsync(key, json);

        // 2. Si cambió el status, actualizar índices
        if (existingOrder != null && existingOrder.Status != model.Status)
        {
            // Remover del status anterior
            _ = transaction.SetRemoveAsync($"{StatusOrdersPrefix}{existingOrder.Status}", model.Id.ToString());

            // Agregar al nuevo status
            _ = transaction.SetAddAsync($"{StatusOrdersPrefix}{model.Status}", model.Id.ToString());
        }

        // 3. Actualizar score en sorted set (por si cambió la fecha)
        var score = new DateTimeOffset(model.CreatedAt).ToUnixTimeSeconds();
        _ = transaction.SortedSetAddAsync(AllOrdersKey, model.Id.ToString(), score);

        // 4. Renovar TTL
        _ = transaction.KeyExpireAsync(key, TimeSpan.FromDays(30));

        var committed = await transaction.ExecuteAsync();

        if (committed)
        {
            Console.WriteLine($" Order {model.Id} updated in Redis");
        }
        else
        {
            Console.WriteLine($" Failed to update order {model.Id} in Redis");
            throw new InvalidOperationException("Failed to update order in Redis");
        }
    }

    public async Task DeleteAsync(Guid orderId)
    {
        // Primero obtenemos la orden para limpiar índices
        var order = await GetByIdAsync(orderId);
        if (order == null)
        {
            Console.WriteLine($"️ Order {orderId} not found, nothing to delete");
            return;
        }

        var key = $"{OrderPrefix}{orderId}";

        var transaction = _redis.CreateTransaction();

        // 1. Eliminar de índices
        _ = transaction.SetRemoveAsync($"{CustomerOrdersPrefix}{order.CustomerId}", orderId.ToString());
        _ = transaction.SetRemoveAsync($"{StatusOrdersPrefix}{order.Status}", orderId.ToString());
        _ = transaction.SortedSetRemoveAsync(AllOrdersKey, orderId.ToString());

        // 2. Eliminar la orden
        _ = transaction.KeyDeleteAsync(key);

        var committed = await transaction.ExecuteAsync();

        if (committed)
        {
            Console.WriteLine($" Order {orderId} deleted from Redis");
        }
        else
        {
            Console.WriteLine($" Failed to delete order {orderId} from Redis");
            throw new InvalidOperationException("Failed to delete order from Redis");
        }
    }

    /// <summary>
    /// Método útil para debugging - obtener estadísticas de Redis
    /// </summary>
    public async Task<RedisStats> GetStatsAsync()
    {
        var server = _connection.GetServer(_connection.GetEndPoints().First());

        var info = await server.InfoAsync("stats");
        var keyspace = await server.InfoAsync("keyspace");

        var totalOrders = await _redis.SortedSetLengthAsync(AllOrdersKey);

        return new RedisStats
        {
            TotalOrders = (long)totalOrders,
            TotalConnections = info.FirstOrDefault(i => i.Key == "total_connections_received").Value,
            TotalCommands = info.FirstOrDefault(i => i.Key == "total_commands_processed").Value,
            UsedMemory = keyspace.FirstOrDefault(i => i.Key == "used_memory_human").Value
        };
    }
}

/// <summary>
/// Estadísticas de Redis para debugging
/// </summary>
public class RedisStats
{
    public long TotalOrders { get; set; }
    public string TotalConnections { get; set; } = string.Empty;
    public string TotalCommands { get; set; } = string.Empty;
    public string UsedMemory { get; set; } = string.Empty;

    public override string ToString()
    {
        return $@"
 Redis Statistics:
- Total Orders: {TotalOrders}
- Total Connections: {TotalConnections}
- Total Commands: {TotalCommands}
- Used Memory: {UsedMemory}
";
    }
}
