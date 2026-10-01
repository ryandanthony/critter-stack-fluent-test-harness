// -----------------------------------------------------------------------
// <copyright file="ProjectionSyncTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Internal;
using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Projection sync on the host with no async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class ProjectionSyncTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "CatchUp makes async read models current on a host that runs no daemon.")]
    public async Task CatchUp_reads_the_async_read_model_without_a_daemon()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Sprocket"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A query after a command sees the read model updated by that command's events.")]
    public async Task When_then_WhenQuery_sees_the_updated_read_model()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new PublishWidget(id))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => r.Published)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A command handler that reads an async projection sees the Givens, because sync runs before the When.")]
    public async Task Sync_runs_before_a_when_whose_handler_reads_a_projection()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new ArchiveWidget(id))
            .ThenAppended<WidgetArchived>(id)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Two queries with nothing written between them sync once, keeping long scenarios fast.")]
    public async Task Back_to_back_queries_sync_once()
    {
        var id = Guid.NewGuid();
        var start = TestHarness.Setup(fixture.Host);

        await start
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Sprocket"))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Sprocket"))
            .RunAsync(Ct);

        Assert.Equal(1, ((ScenarioBuilder)start).LastContext!.SyncCount);
    }

    [Fact]
    [Trait("business_value", "ProjectionSync.None really does nothing, so services with inline-only projections pay no sync cost.")]
    public async Task None_leaves_the_async_read_model_stale()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenResult(r => r is null)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Without sync, a handler that reads a projection fails, which proves the default sync before When matters.")]
    public async Task Without_sync_a_projection_reading_handler_fails()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.None)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new ArchiveWidget(id))
            .ThenThrows<WidgetNotFoundException>()
            .RunAsync(Ct);
    }
}
