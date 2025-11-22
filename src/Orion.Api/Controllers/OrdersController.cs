using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.Metrics;

namespace Orion.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class OrdersController : ControllerBase
{
    private static readonly Meter Meter = new("Orion.Orders");
    private static readonly Counter<long> OrdersCreated = Meter.CreateCounter<long>("orders_created_total");
    private static readonly Histogram<double> OrderValue = Meter.CreateHistogram<double>("order_value_amount");

    private readonly ILogger<OrdersController> _logger;
    private static readonly Dictionary<string, (string customer, double amount)> Db = new();

    public OrdersController(ILogger<OrdersController> logger)
    {
        _logger = logger;
    }

    [HttpPost]
    public IActionResult Create([FromBody] OrderRequest req)
    {
        var id = Guid.NewGuid().ToString("N");
        Db[id] = (req.Customer, req.Amount);

        OrdersCreated.Add(1);
        OrderValue.Record(req.Amount);

        _logger.LogInformation("Created order {OrderId} for {Customer} with amount {Amount}", id, req.Customer, req.Amount);

        return Ok(new { id, req.Customer, req.Amount });
    }

    [HttpGet("{id}")]
    public IActionResult Get(string id)
    {
        if (!Db.TryGetValue(id, out var row))
        {
            _logger.LogWarning("Order {OrderId} not found", id);
            return NotFound();
        }

        return Ok(new { id, row.customer, row.amount });
    }
}

public record OrderRequest(string Customer, double Amount);
