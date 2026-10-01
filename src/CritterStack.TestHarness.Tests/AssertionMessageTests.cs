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
        Assert.Contains("writing the Given failed with ExistingStreamIdCollisionException", failure.Message, StringComparison.Ordinal);
        Assert.Contains("use a fresh id per test", failure.Message, StringComparison.Ordinal);
        Assert.IsType<ExistingStreamIdCollisionException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A bug in the test's own Given setup code is reported against that Given, with the original exception attached.")]
    public async Task Given_events_factory_that_throws_fails_at_that_step()
    {
        var boom = new InvalidOperationException("bad test data");
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(Guid.NewGuid(), _ => throw boom)
            .When(() => new UnhandledCommand()));

        Assert.StartsWith("step 1 GivenStream<Widget>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("building the events threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.Same(boom, failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A bug in the test's command factory is reported as a test bug, not a handler failure, and ThenThrows can't hide it.")]
    public async Task Command_factory_that_throws_fails_at_once_and_cannot_be_claimed()
    {
        var boom = new InvalidOperationException("bad command");
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => throw boom)
            .ThenThrows<InvalidOperationException>());

        // The factory never produced a message, so the label has no message type.
        Assert.StartsWith("step 1 When: building the command threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.Same(boom, failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A query factory that throws is reported as a test bug with the original exception attached.")]
    public async Task Query_factory_that_throws_fails_at_once()
    {
        var boom = new InvalidOperationException("bad query");
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => throw boom)
            .ThenResult(_ => true));

        Assert.StartsWith("step 1 WhenQuery<WidgetSummary>: building the query threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.Same(boom, failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A factory that returns null is caught clearly instead of surfacing as an obscure Wolverine error.")]
    public async Task Factory_that_returns_null_fails_clearly()
    {
        var command = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => null!));
        var query = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => null!));

        Assert.Contains("the command factory returned null", command.Message, StringComparison.Ordinal);
        Assert.Contains("the query factory returned null", query.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A predicate that throws (for example a null dereference) is reported as such, with the original exception attached.")]
    public async Task Predicate_that_throws_is_reported_with_its_exception()
    {
        var id = Guid.NewGuid();
        var boom = new InvalidOperationException("bad predicate");
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppended<WidgetRenamed>(id, _ => throw boom));

        Assert.StartsWith("step 3 ThenAppended<WidgetRenamed>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("the predicate threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.Same(boom, failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "ThenPublished after a claimed throw explains there is nothing to check instead of reporting an empty list.")]
    public async Task ThenPublished_after_a_throw_explains_there_is_no_session()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(Guid.NewGuid()))
            .ThenThrows<WidgetException>()
            .ThenPublished<WidgetPublishedNotice>());

        Assert.StartsWith("step 3 ThenPublished<WidgetPublishedNotice>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("When(FailWidget) threw, so there is no tracked session to check", failure.Message, StringComparison.Ordinal);
    }
}
