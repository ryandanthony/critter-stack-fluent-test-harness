// -----------------------------------------------------------------------
// <copyright file="TestHarnessTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Core Given/When/Then behaviour on the host with no async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class TestHarnessTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private IDocumentStore Store => fixture.Host.Services.GetRequiredService<IDocumentStore>();

    [Fact]
    [Trait("business_value", "The documented happy path works end to end: Given, When, appended-event and query assertions.")]
    public async Task Renaming_widget_appends_WidgetRenamed()
    {
        var id1 = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.ProjectionTimeout = TimeSpan.FromSeconds(30))
            .GivenStream<Widget>(id1, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(id1, "Gear"))
            .ThenAppended<WidgetRenamed>(id1, e => Is(e.Name, "Gear"))
            .ThenAppendedCount(id1, 1)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id1))
            .ThenResult(r => Is(r.Name, "Gear"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A plain Given writes a stream that later steps can see.")]
    public async Task Plain_given_creates_the_stream()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream(id, i => [new WidgetCreated(i, "Plain")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamEventCount(id, 1)
            .ThenStreamHasEvent<WidgetCreated>(id, e => Is(e.Name, "Plain"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A typed Given starts the stream with the aggregate type, so aggregate-based handlers can load it.")]
    public async Task Typed_given_starts_the_stream_with_the_aggregate_type()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Typed")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .RunAsync(Ct);

        await using var session = Store.QuerySession();
        var state = await session.Events.FetchStreamStateAsync(id, Ct);
        Assert.NotNull(state);
        Assert.Equal(typeof(Widget), state.AggregateType);
    }

    [Fact]
    [Trait("business_value", "A stream's history can be built over several Givens without a stream-collision error.")]
    public async Task Two_givens_for_the_same_id_start_then_append()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "One")])
            .GivenStream<Widget>(id, i => [new WidgetRenamed(i, "Two")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Nothing touches the database until the scenario is run, and a scenario can be awaited directly.")]
    public async Task Steps_run_only_when_the_scenario_is_run_or_awaited()
    {
        var id = Guid.NewGuid();
        var scenario = TestHarness.Setup(fixture.Host)
            .GivenStream(id, i => [new WidgetCreated(i, "Deferred")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id));

        await using (var before = Store.QuerySession())
        {
            Assert.Null(await before.Events.FetchStreamStateAsync(id, Ct));
        }

        await scenario;

        await using var after = Store.QuerySession();
        Assert.NotNull(await after.Events.FetchStreamStateAsync(id, Ct));
    }

    [Fact]
    [Trait("business_value", "ForTenant runs Givens, the command handler and the Thens as that tenant.")]
    public async Task ForTenant_runs_every_step_as_the_tenant()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-a")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "A")])
            .When(() => new RecordTenant(id))
            .ThenAppended<TenantRecorded>(id, e => Is(e.TenantId, "tenant-a"))
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Tenants are isolated: one tenant's stream and read model are invisible to another.")]
    public async Task A_stream_given_for_one_tenant_is_invisible_to_another()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-a")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "A")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "A"))
            .RunAsync(Ct);

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-b")
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(r => r is null)
            .ThenStreamEventCount(id, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Appended-event assertions ignore events from Givens, so they prove what the command did.")]
    public async Task ThenAppended_ignores_given_events_while_ThenStreamHasEvent_sees_them()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenStreamHasEvent<WidgetCreated>(id)
            .ThenAppended<WidgetCreated>(id));

        Assert.StartsWith("step 4 ThenAppended<WidgetCreated>", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "After two commands, appended-event assertions only see the second command's events.")]
    public async Task After_two_whens_ThenAppended_sees_only_the_second()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "First"))
            .When(() => new RenameWidget(id, "Second"))
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "Second"))
            .ThenNotAppended<WidgetRenamed>(id, e => Is(e.Name, "First"))
            .ThenAppendedCount(id, 1)
            .ThenStreamEventCount(id, 3)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Appended-event assertions work when the command itself creates the stream.")]
    public async Task ThenAppended_works_on_a_stream_the_when_creates()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(id, "Fresh"))
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "Fresh"))
            .ThenAppendedCount(id, 1)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Negative assertions can target a specific event, not only an event type.")]
    public async Task Negative_assertions_accept_a_predicate()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Cog"))
            .ThenStreamHasNoEvent<WidgetRenamed>(id, e => Is(e.Name, "Gear"))
            .ThenNotAppended<WidgetRenamed>(id, e => Is(e.Name, "Gear"))
            .RunAsync(Ct);

        var other = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(other, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(other, "Gear"))
            .ThenNotAppended<WidgetRenamed>(other, e => Is(e.Name, "Gear")));

        Assert.Contains("found WidgetRenamed matching the predicate", failure.Message, StringComparison.Ordinal);

        var third = Guid.NewGuid();
        var whole = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(third, i => [new WidgetRenamed(i, "Gear")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(third))
            .ThenStreamHasNoEvent<WidgetRenamed>(third, e => Is(e.Name, "Gear")));

        Assert.Contains("found WidgetRenamed matching the predicate in the stream", whole.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Commands that cascade messages can be asserted on.")]
    public async Task ThenPublished_sees_the_cascaded_message()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .ThenAppended<WidgetPublished>(id)
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A scenario can go straight from Givens to a query with no command.")]
    public async Task Given_then_query_with_no_command()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => r.Id == id);
    }

    [Fact]
    [Trait("business_value", "Long scenarios that mix commands and queries keep their history and check the right step.")]
    public async Task Chained_commands_and_queries_check_the_right_result()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "A"))
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "A"))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "A"))
            .When(() => new RenameWidget(id, "B"))
            .ThenAppended<WidgetRenamed>(id, e => Is(e.Name, "B"))
            .ThenAppendedCount(id, 1)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "B"))
            .ThenStreamEventCount(id, 3)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A query whose handler returns null hands null to the predicate instead of failing.")]
    public async Task Null_query_result_is_passed_to_ThenResult()
    {
        await TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
            .ThenResult(r => r is null)
            .RunAsync(Ct);
    }
}
