// -----------------------------------------------------------------------
// <copyright file="CancellationAndSyncFailureTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Cancellation and projection-sync failures, which code coverage showed were never exercised. Each cancellation
/// test cancels from inside a factory, so the token flips deterministically just before the step's own I/O.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class CancellationAndSyncFailureTests(NoDaemonHostFixture fixture)
{
    [Fact]
    [Trait("business_value", "Cancelling the test run stops a scenario at once, as a cancellation rather than a misleading assertion failure.")]
    public async Task Already_cancelled_token_stops_before_the_first_step()
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var built = false;

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i =>
            {
                built = true;
                return [new WidgetCreated(i, "Sprocket")];
            })
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .RunAsync(cts.Token));

        Assert.False(built);
    }

    [Fact]
    [Trait("business_value", "Cancellation while a Given is being written surfaces as a cancellation, not as 'writing the Given failed'.")]
    public async Task Cancellation_during_a_given_is_not_reported_as_a_given_failure()
    {
        using var cts = new CancellationTokenSource();

        var ex = await Record.ExceptionAsync(() => TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(Guid.NewGuid(), i =>
            {
                cts.Cancel();
                return [new WidgetCreated(i, "Sprocket")];
            })
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
            .RunAsync(cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    [Trait("business_value", "Cancellation during the projection sync before a When surfaces as a cancellation, not as a sync failure.")]
    public async Task Cancellation_during_projection_sync_is_not_reported_as_a_sync_failure()
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var ex = await Record.ExceptionAsync(() => TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() =>
            {
                cts.Cancel();
                return new RenameWidget(id, "Gear");
            })
            .RunAsync(cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    [Trait("business_value", "Cancellation during a When surfaces as a cancellation and can't be claimed as the handler's exception.")]
    public async Task Cancellation_during_a_when_is_not_claimable()
    {
        var id = Guid.NewGuid();
        using var cts = new CancellationTokenSource();

        var ex = await Record.ExceptionAsync(() => TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
            .When(() =>
            {
                cts.Cancel();
                return new RenameWidget(id, "Gear");
            })
            .ThenThrows<OperationCanceledException>()
            .RunAsync(cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    [Trait("business_value", "Cancellation while a WhenQuery handler runs surfaces as a cancellation and can't be claimed as the handler's exception.")]
    public async Task Cancellation_during_a_query_is_not_claimable()
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(200));

        var ex = await Record.ExceptionAsync(() => TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
            .WhenQuery<WidgetSummary?>(() => new SlowFindWidget(TimeSpan.FromSeconds(30)))
            .ThenThrows<OperationCanceledException>()
            .RunAsync(cts.Token));

        Assert.IsAssignableFrom<OperationCanceledException>(ex);
    }

    [Fact]
    [Trait("business_value", "A projection sync that can't finish (WaitForNonStale with no daemon) fails at the next action, naming the mode and timeout.")]
    public async Task WaitForNonStale_without_a_daemon_fails_with_the_sync_settings()
    {
        var id = Guid.NewGuid();
        var timeout = TimeSpan.FromMilliseconds(500);

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host, o =>
            {
                o.ProjectionSync = ProjectionSync.WaitForNonStale;
                o.ProjectionTimeout = timeout;
            })
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(_ => true));

        Assert.StartsWith("step 2 WhenQuery<WidgetSummary>(FindWidget): projection sync (WaitForNonStale) failed with", failure.Message, StringComparison.Ordinal);
        Assert.Contains($"(ProjectionTimeout {timeout})", failure.Message, StringComparison.Ordinal);
        Assert.NotNull(failure.InnerException);
    }
}
