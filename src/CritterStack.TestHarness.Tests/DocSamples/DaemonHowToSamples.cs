// -----------------------------------------------------------------------
// <copyright file="DaemonHowToSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0006 // Samples use == on strings, as readers' own tests would.

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs the docs/how-to/ samples that need a host running the async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(DaemonCollection))]
public sealed class DaemonHowToSamples(DaemonHostFixture fixture)
{
    // Sample: choose-projection-sync.md, "WaitForNonStale".
    [Fact]
    [Trait("business_value", "The WaitForNonStale sample in the projection-sync guide works on a host with a daemon.")]
    public async Task ChooseProjectionSync_wait_for_non_stale()
    {
        var id = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.WaitForNonStale)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(s => s.Name == "Sprocket");
    }
}
