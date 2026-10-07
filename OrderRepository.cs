using System.Collections.Concurrent;

namespace Orders.Api;

public interface IOrderRepository
{
    Task<Order> AddAsync(Order order);
    Task<Order?> GetAsync(Guid id);
    Task<IReadOnlyList<Order>> ListAsync(string? customerId);
    Task<Order?> UpdateStatusAsync(Guid id, OrderStatus status);
}

// Swap this for an EF Core / Dapper implementation in production.
public class InMemoryOrderRepository : IOrderRepository
{
    private readonly ConcurrentDictionary<Guid, Order> _orders = new();

    public Task<Order> AddAsync(Order order)
    {
        _orders[order.Id] = order;
        return Task.FromResult(order);
    }

    public Task<Order?> GetAsync(Guid id) =>
        Task.FromResult(_orders.TryGetValue(id, out var o) ? o : null);

    public Task<IReadOnlyList<Order>> ListAsync(string? customerId)
    {
        IEnumerable<Order> q = _orders.Values;
        if (!string.IsNullOrWhiteSpace(customerId))
            q = q.Where(o => o.CustomerId == customerId);
        return Task.FromResult<IReadOnlyList<Order>>(q.OrderByDescending(o => o.CreatedUtc).ToList());
    }

    public Task<Order?> UpdateStatusAsync(Guid id, OrderStatus status)
    {
        if (!_orders.TryGetValue(id, out var existing)) return Task.FromResult<Order?>(null);
        var updated = existing with { Status = status };
        _orders[id] = updated;
        return Task.FromResult<Order?>(updated);
    }
}
