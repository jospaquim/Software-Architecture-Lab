using EDA.EventStore;
using EDA.Projections;
using EDA.ReadModel;
using EDA.WriteModel.Domain;
using Microsoft.AspNetCore.Mvc;

namespace EDA.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class OrdersController : ControllerBase
{
    private readonly IEventStore _eventStore;
    private readonly IEventBus _eventBus;
    private readonly IOrderReadModelRepository _readRepository;

    public OrdersController(
        IEventStore eventStore,
        IEventBus eventBus,
        IOrderReadModelRepository readRepository)
    {
        _eventStore = eventStore;
        _eventBus = eventBus;
        _readRepository = readRepository;
    }

    /// <summary>
    /// Create Order (Command - Write Side)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(Guid), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        // Create aggregate
        var order = OrderAggregate.CreateNew(request.CustomerId);

        // Save events to Event Store
        await _eventStore.SaveEventsAsync(
            order.Id,
            order.UncommittedEvents,
            expectedVersion: 0);

        // Publish events to Event Bus (for projections)
        foreach (var @event in order.UncommittedEvents)
        {
            await _eventBus.PublishAsync(@event);
        }

        order.MarkEventsAsCommitted();

        return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, order.Id);
    }

    /// <summary>
    /// Add Item to Order (Command - Write Side)
    /// </summary>
    [HttpPost("{id}/items")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AddItem(Guid id, [FromBody] AddItemRequest request)
    {
        try
        {
            // Load aggregate from Event Store
            var events = await _eventStore.GetEventsAsync(id);
            var order = OrderAggregate.LoadFromHistory(events);

            // Execute command
            order.AddItem(
                request.ProductId,
                request.ProductName,
                request.UnitPrice,
                request.Quantity);

            // Save new events
            await _eventStore.SaveEventsAsync(
                order.Id,
                order.UncommittedEvents,
                expectedVersion: order.Version);

            // Publish events
            foreach (var @event in order.UncommittedEvents)
            {
                await _eventBus.PublishAsync(@event);
            }

            order.MarkEventsAsCommitted();

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Confirm Order (Command - Write Side)
    /// </summary>
    [HttpPost("{id}/confirm")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> ConfirmOrder(Guid id)
    {
        try
        {
            var events = await _eventStore.GetEventsAsync(id);
            var order = OrderAggregate.LoadFromHistory(events);

            order.ConfirmOrder();

            await _eventStore.SaveEventsAsync(
                order.Id,
                order.UncommittedEvents,
                expectedVersion: order.Version);

            foreach (var @event in order.UncommittedEvents)
            {
                await _eventBus.PublishAsync(@event);
            }

            order.MarkEventsAsCommitted();

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Ship Order (Command - Write Side)
    /// </summary>
    [HttpPost("{id}/ship")]
    public async Task<IActionResult> ShipOrder(Guid id)
    {
        try
        {
            var events = await _eventStore.GetEventsAsync(id);
            var order = OrderAggregate.LoadFromHistory(events);

            order.ShipOrder();

            await _eventStore.SaveEventsAsync(
                order.Id,
                order.UncommittedEvents,
                expectedVersion: order.Version);

            foreach (var @event in order.UncommittedEvents)
            {
                await _eventBus.PublishAsync(@event);
            }

            return Ok();
        }
        catch (Exception ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Get Order (Query - Read Side)
    /// Reads from denormalized Read Model
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(OrderReadModel), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var order = await _readRepository.GetByIdAsync(id);

        if (order == null)
            return NotFound();

        return Ok(order);
    }

    /// <summary>
    /// Get Orders by Customer (Query - Read Side)
    /// </summary>
    [HttpGet("customer/{customerId}")]
    [ProducesResponseType(typeof(IEnumerable<OrderReadModel>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetCustomerOrders(Guid customerId)
    {
        var orders = await _readRepository.GetByCustomerAsync(customerId);
        return Ok(orders);
    }

    /// <summary>
    /// Get Event History (for debugging/audit)
    /// </summary>
    [HttpGet("{id}/events")]
    [ProducesResponseType(typeof(IEnumerable<IEvent>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetEventHistory(Guid id)
    {
        var events = await _eventStore.GetEventsAsync(id);
        return Ok(events);
    }
}

// Request DTOs
public record CreateOrderRequest(Guid CustomerId);

public record AddItemRequest(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity);
