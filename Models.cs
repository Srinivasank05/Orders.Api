namespace Orders.Api;

public enum OrderStatus { Pending, Paid, Shipped, Cancelled }

public record Order(Guid Id, string CustomerId, List<OrderLine> Lines, OrderStatus Status, DateTime CreatedUtc)
{
    public decimal Total => Lines.Sum(l => l.UnitPrice * l.Quantity);
}

public record OrderLine(string Sku, int Quantity, decimal UnitPrice);

public record CreateOrderRequest(string CustomerId, List<OrderLine> Lines);
public record UpdateStatusRequest(OrderStatus Status);
