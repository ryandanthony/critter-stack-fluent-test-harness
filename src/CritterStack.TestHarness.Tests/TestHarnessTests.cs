// -----------------------------------------------------------------------
// <copyright file="TestHarnessTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Core Given/When/Then behaviour on the host with no async daemon.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class TestHarnessTests(NoDaemonHostFixture fixture)
{
    /// <summary>
    /// The Target API sample from the design runs green.
    /// </summary>
    /// <returns>A task.</returns>
    [Fact]
    [Trait("business_value", "The documented happy path works end to end: Given, When, appended-event and query assertions.")]
    public async Task Renaming_widget_appends_WidgetRenamed()
    {
        var id1 = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.ProjectionTimeout = TimeSpan.FromSeconds(30))
            .GivenStream<Widget>(id1, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(id1, "Gear"))
            .ThenAppended<WidgetRenamed>(id1, e => string.Equals(e.Name, "Gear", StringComparison.Ordinal))
            .ThenAppendedCount(id1, 1)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id1))
            .ThenResult(r => string.Equals(r.Name, "Gear", StringComparison.Ordinal))
            .RunAsync(TestContext.Current.CancellationToken);
    }
}
