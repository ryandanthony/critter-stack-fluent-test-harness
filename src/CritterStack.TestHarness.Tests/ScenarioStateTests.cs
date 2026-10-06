// -----------------------------------------------------------------------
// <copyright file="ScenarioStateTests.cs" company="Ryan Anthony">
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
/// How state carries, or doesn't, between actions in one scenario and between runs of one scenario. Line coverage
/// can't see these: every line already runs, but only in single-action scenarios.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class ScenarioStateTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private IDocumentStore Store => fixture.Host.Services.GetRequiredService<IDocumentStore>();

    [Fact]
    [Trait("business_value", "A claimed failure doesn't leak into the next command: its assertions see that command's own outcome.")]
    public async Task A_claimed_exception_does_not_leak_into_the_next_when()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .When(() => new PublishWidget(id))
            .ThenAppended<WidgetPublished>(id)
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Claiming one command's failure doesn't claim the next one's: a second unexpected failure still fails the test.")]
    public async Task A_claim_does_not_carry_over_to_the_next_failure()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .When(() => new FailWidget(id)));

        Assert.StartsWith("end of scenario: step 3 When(FailWidget) threw WidgetException and no ThenThrows claimed it", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "ThenPublished after a command that threw can't pass on messages an earlier command sent.")]
    public async Task ThenPublished_does_not_see_an_earlier_commands_messages()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .ThenPublished<WidgetPublishedNotice>()
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .ThenPublished<WidgetPublishedNotice>());

        Assert.StartsWith("step 6 ThenPublished<WidgetPublishedNotice>: step 4 When(FailWidget) threw, so there is no tracked session to check", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "ThenThrows after a successful command fails even if an earlier command threw, so old failures can't satisfy it.")]
    public async Task ThenThrows_only_sees_the_most_recent_action()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .When(() => new RenameWidget(id, "Gear"))
            .ThenThrows<WidgetException>());

        Assert.Contains("expected WidgetException, but step 3 When(RenameWidget) threw nothing", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A query result doesn't leak into the next query: ThenResult checks the latest query, even when it returns null.")]
    public async Task A_query_result_does_not_leak_into_the_next_query()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Sprocket"))
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
            .ThenResult(r => r is null)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An unclaimed failure stops the scenario at the next action, so a later command never runs on top of a broken one.")]
    public async Task Unclaimed_exception_stops_the_scenario_before_the_next_when()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .When(() => new RenameWidget(id, "Gear")));

        // The second When never built its command, so its label has no message type.
        Assert.StartsWith("step 2 When: step 1 When(FailWidget) threw WidgetException and no ThenThrows claimed it", failure.Message, StringComparison.Ordinal);
        await using var session = Store.QuerySession();
        Assert.Null(await session.Events.FetchStreamStateAsync(id, Ct));
    }

    [Fact]
    [Trait("business_value", "An unclaimed query failure stops the scenario at the next query, naming the query that failed.")]
    public async Task Unclaimed_query_exception_stops_the_scenario_before_the_next_query()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()))
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid())));

        Assert.StartsWith(
            "step 2 WhenQuery<WidgetSummary>: step 1 WhenQuery<WidgetSummary>(GetWidgetById) threw WidgetNotFoundException and no ThenThrows claimed it",
            failure.Message,
            StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Appended-event assertions on several streams each see only what the command did to that stream.")]
    public async Task Appended_assertions_on_two_streams_are_measured_separately()
    {
        var target = Guid.NewGuid();
        var bystander = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(target, i => [new WidgetCreated(i, "Target")])
            .GivenStream<Widget>(bystander, i => [new WidgetCreated(i, "Bystander"), new WidgetRenamed(i, "Still")])
            .When(() => new RenameWidget(target, "Gear"))
            .ThenAppendedCount(target, 1)
            .ThenAppendedCount(bystander, 0)
            .ThenNotAppended<WidgetRenamed>(bystander)
            .ThenStreamEventCount(bystander, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Whole-stream assertions after a When don't depend on a snapshot, so they work on streams no appended-event assertion mentions.")]
    public async Task Whole_stream_assertions_after_a_when_need_no_snapshot()
    {
        var id = Guid.NewGuid();
        var start = TestHarness.Setup(fixture.Host);

        await start
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenStreamHasEvent<WidgetRenamed>(id)
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);

        Assert.Empty(((Internal.ScenarioBuilder)start).LastContext!.Snapshots);
    }

    [Fact]
    [Trait("business_value", "A scenario can be run again; appended-event assertions measure from the stream's version at each run, not the first.")]
    public async Task Rerunning_a_scenario_takes_fresh_snapshots()
    {
        var id = Guid.NewGuid();
        var scenario = TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppendedCount(id, 1)
            .ThenAppended<WidgetRenamed>(id);

        await scenario.RunAsync(Ct);
        await scenario.RunAsync(Ct);

        await using var session = Store.QuerySession();
        var state = await session.Events.FetchStreamStateAsync(id, Ct);
        Assert.Equal(2, state!.Version);
    }

    [Fact]
    [Trait("business_value", "ThenThrows matches subclasses, so a test can claim a failure by a base type such as Exception.")]
    public async Task ThenThrows_matches_by_base_type()
    {
        await TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(Guid.NewGuid()))
            .ThenThrows<Exception>(e => e is WidgetException)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A query handler's own TimeoutException is behaviour under test and can be claimed, like a command's.")]
    public async Task Query_handler_timeout_exception_is_claimable()
    {
        await TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new TimeOutWidgetQuery(Guid.NewGuid()))
            .ThenThrows<TimeoutException>(e => e.Message.Contains("read model", StringComparison.Ordinal))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A null response reaches ThenResult as default(TResponse), even for a value type, instead of crashing.")]
    public async Task Null_response_is_default_for_a_value_type()
    {
        await TestHarness.Setup(fixture.Host)
            .WhenQuery<int>(() => new FindWidget(Guid.NewGuid()))
            .ThenResult(r => r == 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Queries aren't tracked, so a query handler's exception is never mistaken for a tracking timeout, whatever it looks like.")]
    public async Task Query_exception_shaped_like_a_tracking_timeout_stays_claimable()
    {
        await TestHarness.Setup(fixture.Host, o => o.TrackingTimeout = TimeSpan.FromMilliseconds(1))
            .WhenQuery<WidgetSummary>(() => new WrapperShapedTimeoutQuery(Guid.NewGuid()))
            .ThenThrows<TimeoutException>(e => e.Message.StartsWith("This TrackedSession timed out", StringComparison.Ordinal))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A cascaded failure can be claimed as the AggregateException itself, for tests that care about every inner failure.")]
    public async Task ThenThrows_can_claim_the_aggregate_itself()
    {
        await TestHarness.Setup(fixture.Host)
            .When(() => new FailWidgetLater(Guid.NewGuid()))
            .ThenThrows<AggregateException>(e => e.Flatten().InnerExceptions.Single() is WidgetException)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Under ForTenant, expected failures and published messages work the same as for the default tenant.")]
    public async Task ForTenant_supports_ThenThrows_and_ThenPublished()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-c")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "C")])
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .When(() => new PublishWidget(id))
            .ThenAppended<WidgetPublished>(id)
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == id)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => r.Published)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Under ForTenant, a query for a stream that only exists for another tenant fails as that tenant would see it.")]
    public async Task ForTenant_query_failure_is_claimable()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-d")
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "D")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "D"))
            .RunAsync(Ct);

        await TestHarness.Setup(fixture.Host)
            .ForTenant("tenant-e")
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenThrows<WidgetNotFoundException>()
            .RunAsync(Ct);
    }
}
