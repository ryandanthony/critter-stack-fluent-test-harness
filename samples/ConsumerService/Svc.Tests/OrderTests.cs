using Alba;
using CritterStack.TestHarness;
using JasperFx.Events.Projections;
using Marten;
using Marten.Events.Aggregation;
using Microsoft.AspNetCore.Builder;
using Testcontainers.PostgreSql;
using Wolverine;
using Wolverine.Marten;

namespace Svc.Tests;

// A service's own domain, unrelated to the harness's Widget test domain.
public record OrderPlaced(Guid Id, decimal Total);
public record OrderShipped(Guid Id);
public record ShipOrder(Guid Id);
public record OrderShippedNotice(Guid Id);
public record GetOrder(Guid Id);
public class Order { public Guid Id { get; set; } }
public class OrderView { public Guid Id { get; set; } public decimal Total { get; set; } public bool Shipped { get; set; } }
public class OrderViewProjection : SingleStreamProjection<OrderView, Guid>
{
    public static OrderView Create(OrderPlaced e) => new() { Id = e.Id, Total = e.Total };
    public static void Apply(OrderShipped e, OrderView v) => v.Shipped = true;
}
public static class OrderHandlers
{
    public static OrderShippedNotice Handle(ShipOrder c, IDocumentSession s) { s.Events.Append(c.Id, new OrderShipped(c.Id)); return new(c.Id); }
    public static void Handle(OrderShippedNotice n) { }
    public static Task<OrderView?> Handle(GetOrder q, IQuerySession s) => s.LoadAsync<OrderView>(q.Id);
}

// The service's existing Alba fixture, as the how-to assumes.
public sealed class ServiceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _pg = new PostgreSqlBuilder("postgres:17-alpine").Build();
    public IAlbaHost Host { get; private set; } = null!;
    public async ValueTask InitializeAsync()
    {
        await _pg.StartAsync();
        var b = WebApplication.CreateBuilder();
        b.Services.AddMarten(o => { o.Connection(_pg.GetConnectionString()); o.Projections.Add<OrderViewProjection>(ProjectionLifecycle.Async); })
            .IntegrateWithWolverine();
        b.Services.DisableAllExternalWolverineTransports();
        b.Host.UseWolverine(o => { o.Policies.AutoApplyTransactions(); o.Discovery.IncludeType(typeof(OrderHandlers)); });
        Host = await AlbaHost.For(b, _ => { });
    }
    public async ValueTask DisposeAsync() { await Host.DisposeAsync(); await _pg.DisposeAsync(); }
}

public sealed class OrderTests(ServiceFixture fixture) : IClassFixture<ServiceFixture>
{
    [Fact]
    public async Task Shipping_an_order()
    {
        var id = Guid.NewGuid();
        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Order>(id, i => [new OrderPlaced(i, 42m)])
            .When(() => new ShipOrder(id))
            .ThenAppended<OrderShipped>(id)
            .ThenPublished<OrderShippedNotice>(n => n.Id == id)
            .WhenQuery<OrderView?>(() => new GetOrder(id))
            .ThenResult(v => v is { Shipped: true, Total: 42m });
    }

    [Fact]
    public async Task Failure_message_names_the_step()
    {
        var id = Guid.NewGuid();
        var ex = await Assert.ThrowsAsync<HarnessAssertionException>(() => TestHarness
            .Setup(fixture.Host)
            .GivenStream<Order>(id, i => [new OrderPlaced(i, 42m)])
            .When(() => new ShipOrder(id))
            .ThenAppendedCount(id, 2)
            .RunAsync(TestContext.Current.CancellationToken));
        Assert.StartsWith("step 3 ThenAppendedCount", ex.Message);
    }
}
