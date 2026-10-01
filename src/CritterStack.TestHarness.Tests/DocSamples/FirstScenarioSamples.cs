// -----------------------------------------------------------------------
// <copyright file="FirstScenarioSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0006 // Samples use == on strings, as readers' own tests would.

using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs every C# block in docs/tutorials/first-scenario.md.
/// </summary>
/// <param name="fixture">The tutorial fixture.</param>
public sealed class FirstScenarioSamples(WidgetServiceFixture fixture) : IClassFixture<WidgetServiceFixture>
{
    // Sample: first-scenario.md, step 3.
    [Fact]
    [Trait("business_value", "The tutorial's first scenario passes exactly as written.")]
    public async Task Renaming_a_widget_records_the_new_name()
    {
        var widgetId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(widgetId, "Gear"))
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Gear");
    }

    // Sample: first-scenario.md, step 5.
    [Fact]
    [Trait("business_value", "The tutorial's query step reads the async read model as written.")]
    public async Task Renaming_a_widget_updates_its_summary()
    {
        var widgetId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(widgetId, "Gear"))
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Gear")
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(widgetId))
            .ThenResult(summary => summary.Name == "Gear");
    }

    // Sample: first-scenario.md, step 4 (the deliberately broken version and the message it produces).
    [Fact]
    [Trait("business_value", "The tutorial's 'break it on purpose' step fails with the message the tutorial shows.")]
    public async Task Broken_scenario_shows_the_actual_events()
    {
        var widgetId = Guid.NewGuid();

        var failure = await Assert.ThrowsAsync<ScenarioAssertionException>(() => TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, id => [new WidgetCreated(id, "Sprocket")])
            .When(() => new RenameWidget(widgetId, "Gear"))
            .ThenAppended<WidgetRenamed>(widgetId, e => e.Name == "Cog")
            .RunAsync(TestContext.Current.CancellationToken));

        Assert.StartsWith("step 3 ThenAppended<WidgetRenamed>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("no WidgetRenamed matching the predicate appended by step 2 When(RenameWidget)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v2 WidgetRenamed {", failure.Message, StringComparison.Ordinal);
        Assert.Contains("\"Gear\"", failure.Message, StringComparison.Ordinal);
    }
}
