using Orders.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSingleton<IOrderRepository, InMemoryOrderRepository>();
builder.Services.AddHealthChecks();
builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.MapHealthChecks("/health");

var orders = app.MapGroup("/orders");

orders.MapPost("/", async (CreateOrderRequest req, IOrderRepository repo, ILogger<Program> log) =>
{
    var errors = Validate(req);
    if (errors.Count > 0) return Results.ValidationProblem(errors);

    var order = new Order(Guid.NewGuid(), req.CustomerId, req.Lines, OrderStatus.Pending, DateTime.UtcNow);
    await repo.AddAsync(order);
    log.LogInformation("Order {OrderId} created for {CustomerId}", order.Id, order.CustomerId);
    // Here you would publish an OrderCreated event to your message broker.
    return Results.Created($"/orders/{order.Id}", order);
});

orders.MapGet("/", async (string? customerId, IOrderRepository repo) =>
    Results.Ok(await repo.ListAsync(customerId)));

orders.MapGet("/{id:guid}", async (Guid id, IOrderRepository repo) =>
    await repo.GetAsync(id) is { } o ? Results.Ok(o) : Results.NotFound());

orders.MapPut("/{id:guid}/status", async (Guid id, UpdateStatusRequest req, IOrderRepository repo) =>
    await repo.UpdateStatusAsync(id, req.Status) is { } o ? Results.Ok(o) : Results.NotFound());

app.Run();

static Dictionary<string, string[]> Validate(CreateOrderRequest req)
{
    var errors = new Dictionary<string, string[]>();
    if (string.IsNullOrWhiteSpace(req.CustomerId))
        errors["customerId"] = ["CustomerId is required."];
    if (req.Lines is null || req.Lines.Count == 0)
        errors["lines"] = ["At least one line is required."];
    else if (req.Lines.Any(l => string.IsNullOrWhiteSpace(l.Sku) || l.Quantity <= 0 || l.UnitPrice < 0))
        errors["lines"] = ["Each line needs a SKU, quantity > 0 and a non-negative price."];
    return errors;
}

public partial class Program;
