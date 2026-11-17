// -----------------------------------------------------------------------
// <copyright file="ProjectionErrorTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Daemon;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// A projection that throws while catching up, on a host that runs the async daemon.
/// </summary>
/// <remarks>
/// Keep this to one test per failing projection. Once the daemon reports a projection's error, it stops that
/// projection, and later catch-ups on the same host skip it without reporting anything. A second test on the same
/// projection would depend on test order. The two tests here use different stores (the main store's
/// <see cref="PoisonProjection"/>, and <see cref="IFaultyDaemonStore"/>'s projection), and the first never syncs the faulty
/// store, so they don't interfere.
/// Don't register <see cref="PoisonProjection"/> on another host in the same test run either: with two hosts
/// catching up a failing projection in one process, the error was reported on only one of them, and this test
/// failed about one run in five. A plain Marten catch-up, with no harness code, behaved the same way.
/// </remarks>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(PoisonDaemonCollection))]
public sealed class ProjectionErrorTests(PoisonDaemonHostFixture fixture)
{
    [Fact]
    [Trait("business_value", "A projection that throws during CatchUp fails the next action with Marten's error attached, instead of a misleading assertion later.")]
    public async Task CatchUp_projection_error_fails_the_next_action_with_the_apply_error()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host, o => o.ProjectionTimeout = TimeSpan.FromSeconds(10))
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket"), new WidgetPoisoned(i)])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(_ => true));

        Assert.StartsWith(
            "step 2 WhenQuery<WidgetSummary>(FindWidget): projection sync (CatchUp) failed with AggregateException [ApplyEventException] (ProjectionTimeout 00:00:10)",
            failure.Message,
            StringComparison.Ordinal);
        var reported = Assert.IsType<AggregateException>(failure.InnerException);
        Assert.StartsWith("Marten reported projection errors while catching up.", reported.Message, StringComparison.Ordinal);
        Assert.IsType<ApplyEventException>(Assert.Single(reported.Flatten().InnerExceptions));
    }

    [Fact]
    [Trait("business_value", "A projection that throws in an ancillary store with its own daemon fails the next action, naming the store, like one in the main store.")]
    public async Task CatchUp_projection_error_in_an_ancillary_store_fails_the_next_action()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host, o => o.ProjectionTimeout = TimeSpan.FromSeconds(10))
            .InStore<IFaultyDaemonStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme"), new LedgerFaulted(i)])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(_ => true));

        Assert.True(
            failure.Message.StartsWith(
                "step 2 WhenQuery<WidgetSummary>(FindWidget): projection sync (CatchUp) of IFaultyDaemonStore failed with AggregateException [ApplyEventException]",
                StringComparison.Ordinal),
            failure.ToString());
        var reported = Assert.IsType<AggregateException>(failure.InnerException);
        Assert.IsType<ApplyEventException>(Assert.Single(reported.Flatten().InnerExceptions));
    }
}
