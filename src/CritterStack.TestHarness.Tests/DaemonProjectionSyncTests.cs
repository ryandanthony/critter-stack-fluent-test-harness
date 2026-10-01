// -----------------------------------------------------------------------
// <copyright file="DaemonProjectionSyncTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Projection sync on the host that runs an async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(DaemonCollection))]
public sealed class DaemonProjectionSyncTests(DaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "WaitForNonStale works for services that already run the async daemon.")]
    public async Task WaitForNonStale_works_with_a_daemon()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.WaitForNonStale)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Gear"))
            .RunAsync(Ct);
    }

    /// <remarks>
    /// On a host with a daemon, building a second daemon fails with ProgressionProgressOutOfOrderException (verified
    /// in verify-apis). So this passing proves CatchUp took the ForceAllMartenDaemonActivityToCatchUpAsync path.
    /// </remarks>
    /// <returns>A task.</returns>
    [Fact]
    [Trait("business_value", "The default CatchUp also works on hosts that run a daemon, so services don't have to change modes.")]
    public async Task CatchUp_works_with_a_daemon()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Gear"))
            .When(() => new RenameWidget(id, "Cog"))
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Cog"))
            .RunAsync(Ct);
    }
}
