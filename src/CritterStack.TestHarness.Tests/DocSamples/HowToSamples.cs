// -----------------------------------------------------------------------
// <copyright file="HowToSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0006 // Samples use == on strings, as readers' own tests would.

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs every C# block in the docs/how-to/ guides that runs on a host with no daemon.
/// Each test names the page and block it backs.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class HowToSamples(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    // Sample: add-to-a-service.md, "Write a first scenario".
    [Fact]
    [Trait("business_value", "The add-to-a-service guide's first scenario works against an existing Alba fixture.")]
    public async Task AddToAService_first_scenario()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppended<WidgetRenamed>(id);
    }

    // Sample: use-string-and-typed-stream-ids.md, "Strongly typed ids".
    [Fact]
    [Trait("business_value", "The stream-id guide's typed-id sample works as documented.")]
    public async Task UseStreamIds_typed_ids()
    {
        var id = new ParcelId(Guid.NewGuid());

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Parcel, ParcelId>(id, p => [new ParcelCreated(p, "box")])
            .When(() => new ShipParcel(id))
            .ThenAppended<ParcelShipped, ParcelId>(id, e => e.Id == id)
            .ThenAppendedCount(id, 1);
    }

    // Sample: choose-projection-sync.md, "CatchUp".
    [Fact]
    [Trait("business_value", "The CatchUp sample in the projection-sync guide works on a host with no daemon.")]
    public async Task ChooseProjectionSync_catch_up()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host) // ProjectionSync.CatchUp is the default
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Name == "Sprocket");
    }

    // Sample: choose-projection-sync.md, "None".
    [Fact]
    [Trait("business_value", "The None sample in the projection-sync guide behaves as documented: async read models stay stale.")]
    public async Task ChooseProjectionSync_none()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(s => s is null); // the async projection never ran
    }

    // Sample: test-expected-exceptions.md, "Claim the exception first".
    [Fact]
    [Trait("business_value", "The expected-exceptions guide's main pattern works as written.")]
    public async Task TestExpectedExceptions_claim_first()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>(e => e.Message.Contains("failed", StringComparison.Ordinal))
            .ThenAppendedCount(id, 0);
    }

    // Sample: test-expected-exceptions.md, "Exceptions from queries".
    [Fact]
    [Trait("business_value", "The expected-exceptions guide's query sample works as written.")]
    public async Task TestExpectedExceptions_query()
    {
        await TestHarness
            .Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()))
            .ThenThrows<WidgetNotFoundException>();
    }

    // Sample: test-expected-exceptions.md, "Exceptions from cascaded messages".
    [Fact]
    [Trait("business_value", "The expected-exceptions guide's cascade sample works as written.")]
    public async Task TestExpectedExceptions_cascaded()
    {
        await TestHarness
            .Setup(fixture.Host)
            .When(() => new FailWidgetLater(Guid.NewGuid()))
            .ThenThrows<WidgetException>(); // matched inside Wolverine's AggregateException
    }

    // Sample: assert-published-messages.md, "Assert a cascaded message".
    [Fact]
    [Trait("business_value", "The published-messages guide's sample works as written.")]
    public async Task AssertPublishedMessages_cascaded()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id);
    }

    // Sample: assert-published-messages.md, "Hosts with real transports".
    [Fact]
    [Trait("business_value", "The published-messages guide's options for real transports are accepted on a local-only host.")]
    public async Task AssertPublishedMessages_real_transports_options()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o =>
            {
                o.IncludeExternalMessages = true;
                o.TrackingTimeout = TimeSpan.FromSeconds(30);
            })
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .ThenPublished<WidgetPublishedNotice>();
    }

    // Sample: test-multi-tenant-handlers.md, "Run a scenario as one tenant".
    [Fact]
    [Trait("business_value", "The multi-tenancy guide's main sample works as written.")]
    public async Task TestMultiTenantHandlers_one_tenant()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .ForTenant("acme")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppended<WidgetRenamed>(id)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Name == "Gear");
    }

    // Sample: test-multi-tenant-handlers.md, "Prove tenants are isolated".
    [Fact]
    [Trait("business_value", "The multi-tenancy guide's isolation sample works as written.")]
    public async Task TestMultiTenantHandlers_isolation()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .ForTenant("acme")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Id == id);

        await TestHarness
            .Setup(fixture.Host)
            .ForTenant("globex")
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(s => s is null)
            .ThenStreamEventCount(id, 0);
    }

    // Sample: query-read-models.md, "Query after a command".
    [Fact]
    [Trait("business_value", "The read-model guide's command-then-query sample works as written.")]
    public async Task QueryReadModels_after_a_command()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Published);
    }

    // Sample: query-read-models.md, "Query without a command".
    [Fact]
    [Trait("business_value", "The read-model guide's query-only sample works as written.")]
    public async Task QueryReadModels_without_a_command()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket"), new WidgetRenamed(i, "Gear")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Name == "Gear");
    }

    // Sample: query-read-models.md, "Queries that may return null".
    [Fact]
    [Trait("business_value", "The read-model guide's nullable-result sample works as written.")]
    public async Task QueryReadModels_nullable()
    {
        await TestHarness
            .Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
            .ThenResult(s => s is null);
    }

    // Sample: debug-a-failing-scenario.md, "Read the message".
    [Fact]
    [Trait("business_value", "The debugging guide's example failure message is what the harness really produces.")]
    public async Task DebugAFailingScenario_message()
    {
        var id = Guid.NewGuid();

        var failure = await Assert.ThrowsAsync<HarnessAssertionException>(() => TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Cog"))
            .ThenAppended<WidgetRenamed>(id, e => e.Name == "Gear")
            .RunAsync(Ct));

        Assert.Matches(
            @"^step 3 ThenAppended<WidgetRenamed>\([0-9a-f-]+\): no WidgetRenamed matching the predicate appended by step 2 When\(RenameWidget\)\.\r?\nActual events:\r?\n  v2 WidgetRenamed \{.*""Cog"".*\}$",
            failure.Message);
    }

    // Sample: debug-a-failing-scenario.md, "An exception you didn't expect".
    [Fact]
    [Trait("business_value", "The debugging guide's unexpected-exception example matches the real output.")]
    public async Task DebugAFailingScenario_unexpected_exception()
    {
        var id = Guid.NewGuid();

        var failure = await Assert.ThrowsAsync<HarnessAssertionException>(() => TestHarness
            .Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .ThenAppended<WidgetRenamed>(id)
            .RunAsync(Ct));

        Assert.StartsWith(
            "step 2 ThenAppended<WidgetRenamed>(",
            failure.Message,
            StringComparison.Ordinal);
        Assert.EndsWith(
            "step 1 When(FailWidget) threw WidgetException and no ThenThrows claimed it. To expect it, put ThenThrows<...>() straight after that step.",
            failure.Message,
            StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }
}
