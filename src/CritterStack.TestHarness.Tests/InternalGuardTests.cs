// -----------------------------------------------------------------------
// <copyright file="InternalGuardTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Internal;
using CritterStack.TestHarness.Internal.Steps;
using CritterStack.TestHarness.Tests.Infrastructure;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Defensive paths the fluent API can't reach, found by code coverage: they protect against harness bugs and
/// payloads the store can't serialize, so they are driven through the internals directly.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class InternalGuardTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "HarnessAssertionException follows the standard exception constructor pattern, so callers and tools can create it.")]
    public void HarnessAssertionException_has_the_standard_constructors()
    {
        var empty = new HarnessAssertionException();
        var withMessage = new HarnessAssertionException("step 1 When: boom");
        var inner = new InvalidOperationException("inner");
        var withInner = new HarnessAssertionException("step 1 When: boom", inner);

        Assert.Null(empty.InnerException);
        Assert.Equal("step 1 When: boom", withMessage.Message);
        Assert.Null(withMessage.InnerException);
        Assert.Same(inner, withInner.InnerException);
    }

    [Fact]
    [Trait("business_value", "A payload the store can't serialize still produces a readable failure message instead of masking the real failure.")]
    public void Unserializable_payload_is_described_instead_of_throwing()
    {
        var context = new ScenarioContext(fixture.Host, new HarnessOptions(), tenantId: null);

        var json = PayloadFormatter.Json(context, typeof(string));

        Assert.StartsWith("(could not serialize: ", json, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Starting an action clears everything the previous action left behind, so no assertion can read a stale result, snapshot, session or exception.")]
    public void BeginAction_clears_the_previous_actions_state()
    {
        var context = new ScenarioContext(fixture.Host, new HarnessOptions(), tenantId: null)
        {
            ActionException = new InvalidOperationException("old"),
            ActionExceptionClaimed = true,
            QueryResult = "old result",
        };
        context.Snapshots[new StreamAddress(StoreRef.Main, StreamKey.For(Guid.NewGuid()))] = 3;

        context.BeginAction("step 4 When(RenameWidget)");

        Assert.Equal("step 4 When(RenameWidget)", context.ActionLabel);
        Assert.Null(context.ActionException);
        Assert.False(context.ActionExceptionClaimed);
        Assert.Null(context.TrackedSession);
        Assert.Null(context.QueryResult);
        Assert.Empty(context.Snapshots);
    }

    [Fact]
    [Trait("business_value", "Snapshot planning ties an appended-event check only to the When directly before it, never to a When before an intervening query.")]
    public async Task Snapshot_planning_stops_at_a_query()
    {
        var id = Guid.NewGuid();
        var command = new WhenCommandStep(() => new TestDomain.RenameWidget(id, "Gear"));

        // The public API can't put an appended check after a query; building the steps directly can, to prove the plan.
        IScenarioStep[] steps =
        [
            command,
            new WhenQueryStep<TestDomain.WidgetSummary?>(() => new TestDomain.FindWidget(id)),
            StreamAssertionStep.ForCount(new StreamAddress(StoreRef.Main, StreamKey.For(id)), StreamScope.Appended, 0),
        ];

        await Assert.ThrowsAsync<HarnessAssertionException>(
            () => ScenarioRunner.RunAsync(steps, fixture.Host, new HarnessOptions(), tenantId: null, [], Ct));

        Assert.Empty(command.SnapshotIds);
    }

    [Fact]
    [Trait("business_value", "If the runner ever forgets to snapshot a stream before a When, the appended-event assertion says it's a harness bug rather than reporting wrong events.")]
    public async Task Appended_assertion_without_a_snapshot_reports_a_harness_bug()
    {
        var context = new ScenarioContext(fixture.Host, new HarnessOptions(), tenantId: null)
        {
            CurrentStepLabel = "step 2 ThenAppendedCount",
        };
        var step = StreamAssertionStep.ForCount(new StreamAddress(StoreRef.Main, StreamKey.For(Guid.NewGuid())), StreamScope.Appended, 0);

        var failure = await Assert.ThrowsAsync<HarnessAssertionException>(() => step.ExecuteAsync(context, Ct));

        Assert.Equal("step 2 ThenAppendedCount: no stream snapshot was taken before the When; this is a harness bug", failure.Message);
    }

    [Fact]
    [Trait("business_value", "Exception matching for ThenThrows finds the type directly, inside an AggregateException, or reports no match.")]
    public void FindMatch_handles_direct_aggregate_and_unrelated_exceptions()
    {
        var direct = new TimeoutException("direct");
        var nested = new TimeoutException("nested");
        var aggregate = new AggregateException(new InvalidOperationException(), new AggregateException(nested));

        Assert.Same(direct, ExceptionClassifier.FindMatch<TimeoutException>(direct));
        Assert.Same(nested, ExceptionClassifier.FindMatch<TimeoutException>(aggregate));
        Assert.Null(ExceptionClassifier.FindMatch<TimeoutException>(new InvalidOperationException()));
        Assert.Null(ExceptionClassifier.FindMatch<ArgumentException>(aggregate));
    }

    [Fact]
    [Trait("business_value", "A handler's own early TimeoutException is never mistaken for a tracking timeout, even if it is wrapped by the tracked session.")]
    public void Early_wrapped_timeout_is_a_handler_exception_not_infrastructure()
    {
        var handler = new TimeoutException("supplier timed out");
        var wrapped = new TimeoutException("This TrackedSession timed out", handler);
        var tracking = TimeSpan.FromSeconds(10);

        Assert.Null(ExceptionClassifier.InfrastructureReason(wrapped, typeof(string), tracking, TimeSpan.FromSeconds(1)));
        Assert.NotNull(ExceptionClassifier.InfrastructureReason(wrapped, typeof(string), tracking, TimeSpan.FromSeconds(9.5)));
        Assert.Same(handler, ExceptionClassifier.HandlerException(wrapped));
        Assert.Same(handler, ExceptionClassifier.HandlerException(handler));
    }

    [Fact]
    [Trait("business_value", "Only Wolverine's own tracking wrapper counts as a tracking timeout; a late handler TimeoutException with another shape stays claimable.")]
    public void Only_the_tracking_wrapper_shape_is_a_tracking_timeout()
    {
        var tracking = TimeSpan.FromSeconds(10);
        var late = TimeSpan.FromSeconds(11);
        var handlerOwn = new TimeoutException("This TrackedSession timed out but was thrown by a handler");
        var otherMessage = new TimeoutException("supplier timed out", new TimeoutException("inner"));
        var otherInner = new TimeoutException("This TrackedSession timed out", new InvalidOperationException("inner"));

        Assert.Null(ExceptionClassifier.InfrastructureReason(handlerOwn, typeof(string), tracking, late));
        Assert.Null(ExceptionClassifier.InfrastructureReason(otherMessage, typeof(string), tracking, late));
        Assert.Null(ExceptionClassifier.InfrastructureReason(otherInner, typeof(string), tracking, late));
        Assert.Same(otherMessage, ExceptionClassifier.HandlerException(otherMessage));
        Assert.Same(otherInner, ExceptionClassifier.HandlerException(otherInner));
    }

    [Fact]
    [Trait("business_value", "Nested generic types are named readably at every level, e.g. Dictionary<String, List<Int32>>.")]
    public void TypeName_names_nested_generic_arguments()
    {
        Assert.Equal("Dictionary<String, List<Int32>>", Failure.TypeName(typeof(Dictionary<string, List<int>>)));
        Assert.Equal("Widget", Failure.TypeName(typeof(TestDomain.Widget)));
    }

    [Fact]
    [Trait("business_value", "When IncludeExternalMessages is set, the When really asks Wolverine to track external transports.")]
    public async Task IncludeExternalMessages_turns_on_external_tracking()
    {
        var on = await RunPublishAsync(includeExternal: true);
        var off = await RunPublishAsync(includeExternal: false);

        Assert.True(AlwaysTrackExternalTransports(on));
        Assert.False(AlwaysTrackExternalTransports(off));
    }

    private static bool AlwaysTrackExternalTransports(object trackedSession) =>
        (bool)trackedSession.GetType().GetProperty("AlwaysTrackExternalTransports")!.GetValue(trackedSession)!;

    private async Task<object> RunPublishAsync(bool includeExternal)
    {
        var id = Guid.NewGuid();
        var start = TestHarness.Setup(fixture.Host, o => o.IncludeExternalMessages = includeExternal);
        await start
            .When(() => new TestDomain.PublishWidget(id))
            .ThenPublished<TestDomain.WidgetPublishedNotice>()
            .RunAsync(Ct);

        return ((ScenarioBuilder)start).LastContext!.TrackedSession!;
    }
}
