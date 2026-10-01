// -----------------------------------------------------------------------
// <copyright file="AssertionMessageTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten.Exceptions;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Failure messages and infrastructure failures (which can't be claimed by ThenThrows).
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class AssertionMessageTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "A failed stream assertion shows every actual event with its version, type and JSON, so the cause is visible.")]
    public async Task Failed_stream_assertion_lists_events_with_json()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamHasEvent<WidgetRenamed>(id));

        Assert.StartsWith("step 3 ThenStreamHasEvent<WidgetRenamed>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v1 WidgetCreated {", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Sprocket", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A failed ThenResult shows the actual response as JSON.")]
    public async Task Failed_ThenResult_shows_the_response_as_json()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
            .ThenResult(r => Is(r.Name, "Gear")));

        Assert.Contains("did not match the predicate", failure.Message, StringComparison.Ordinal);
        Assert.Contains("Sprocket", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A failed ThenPublished lists what was actually sent.")]
    public async Task Failed_ThenPublished_lists_sent_messages()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new PublishWidget(id))
            .ThenPublished<WidgetPublishedNotice>(n => n.Id == Guid.Empty));

        Assert.Contains("did not send WidgetPublishedNotice matching the predicate", failure.Message, StringComparison.Ordinal);
        Assert.Contains("WidgetPublishedNotice {", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Asking a query for the wrong response type is caught, even though Wolverine silently returns null.")]
    public async Task Wrong_response_type_fails_at_once()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<string>(() => new GetWidgetById(id))
            .ThenResult(_ => true));

        Assert.StartsWith("step 2 WhenQuery<String>(GetWidgetById)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected the handler to return String, but it returned WidgetSummary", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A tracking timeout is a setup problem: it fails at once and can't be mistaken for expected behaviour.")]
    public async Task Tracking_timeout_fails_at_once_and_cannot_be_claimed()
    {
        var stall = TimeSpan.FromSeconds(2);
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host, o => o.TrackingTimeout = TimeSpan.FromMilliseconds(250))
            .When(() => new StallWidget(stall))
            .ThenThrows<TimeoutException>());

        Assert.StartsWith("step 1 When(StallWidget): Wolverine tracking timed out", failure.Message, StringComparison.Ordinal);
        Assert.IsType<TimeoutException>(failure.InnerException);

        // Let the stalled handler finish so it doesn't run into the next test's tracked session.
        await Task.Delay(stall, Ct);
    }

    [Fact]
    [Trait("business_value", "A command with no handler is a setup problem and fails at once.")]
    public async Task Missing_handler_fails_at_once_and_cannot_be_claimed()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new UnhandledCommand())
            .ThenThrows<Exception>());

        Assert.StartsWith("step 1 When(UnhandledCommand): no Wolverine handler is registered for UnhandledCommand", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Reusing a stream id across tests is caught with the real Marten error attached.")]
    public async Task Given_that_reuses_a_stream_id_fails_at_once()
    {
        var id = Guid.NewGuid();
        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "First")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .RunAsync(Ct);

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Again")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id)));

        Assert.StartsWith("step 1 GivenStream<Widget>", failure.Message, StringComparison.Ordinal);
        Assert.IsType<ExistingStreamIdCollisionException>(failure.InnerException);
    }
}
