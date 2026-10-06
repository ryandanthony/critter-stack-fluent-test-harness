// -----------------------------------------------------------------------
// <copyright file="FailureMessageGapTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Failure paths that code coverage showed were never exercised: count mismatches, wrong or unmatched
/// <c>ThenThrows</c>, cascaded-exception descriptions, empty payloads and a missing query handler.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class FailureMessageGapTests(NoDaemonHostFixture fixture)
{
    [Fact]
    [Trait("business_value", "A failure listing several events shows one per line, oldest first, with nothing trailing after the last.")]
    public async Task Failed_stream_assertion_lists_each_event_on_its_own_line()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket"), new WidgetRenamed(i, "Gear")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamHasEvent<WidgetPublished>(id));

        var lines = failure.Message.Split(Environment.NewLine);
        Assert.Equal("Actual events:", lines[^3]);
        Assert.StartsWith("  v1 WidgetCreated {", lines[^2], StringComparison.Ordinal);
        Assert.StartsWith("  v2 WidgetRenamed {", lines[^1], StringComparison.Ordinal);
        Assert.EndsWith("}", lines[^1], StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A wrong appended count on a stream the command created says 'found 0' or the right number, never counting from before the stream existed.")]
    public async Task ThenAppendedCount_on_a_new_stream_counts_from_its_first_event()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(id, "Fresh"))
            .ThenAppendedCount(id, 2));

        Assert.Contains("expected 2 event(s) appended by step 1 When(RenameWidget), found 1", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v1 WidgetRenamed {", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A wrong whole-stream event count says what was expected, what was found, and lists the events.")]
    public async Task ThenStreamEventCount_mismatch_reports_expected_and_actual()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamEventCount(id, 2));

        Assert.StartsWith($"step 3 ThenStreamEventCount({id}, 2)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected 2 event(s) in the stream, found 1", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v1 WidgetCreated {", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A wrong appended-event count names the command and lists only what that command appended.")]
    public async Task ThenAppendedCount_mismatch_names_the_command()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new RenameWidget(id, "Gear"))
            .ThenAppendedCount(id, 2));

        Assert.StartsWith($"step 3 ThenAppendedCount({id}, 2)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected 2 event(s) appended by step 2 When(RenameWidget), found 1", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v2 WidgetRenamed {", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("WidgetCreated", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "ThenThrows for the wrong exception type fails, names the real type, and attaches the real exception.")]
    public async Task ThenThrows_with_the_wrong_type_names_the_actual_exception()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(Guid.NewGuid()))
            .ThenThrows<TimeoutException>());

        Assert.StartsWith("step 2 ThenThrows<TimeoutException>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected TimeoutException, but step 1 When(FailWidget) threw WidgetException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "ThenThrows with the right type but a non-matching predicate fails instead of passing silently.")]
    public async Task ThenThrows_with_a_non_matching_predicate_fails()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(Guid.NewGuid()))
            .ThenThrows<WidgetException>(e => e.Message.Contains("never", StringComparison.Ordinal)));

        Assert.Contains("step 1 When(FailWidget) threw WidgetException, but it did not match the predicate", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A cascaded handler's exception that nobody claims is described by its inner types, not just 'AggregateException'.")]
    public async Task Unclaimed_cascaded_exception_lists_the_inner_types()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidgetLater(Guid.NewGuid())));

        Assert.StartsWith(
            "end of scenario: step 1 When(FailWidgetLater) threw AggregateException [WidgetException]",
            failure.Message,
            StringComparison.Ordinal);
        Assert.IsType<AggregateException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "ThenThrows for the wrong type after a cascaded failure lists the inner types so the real cause is visible.")]
    public async Task ThenThrows_wrong_type_after_cascaded_failure_lists_inner_types()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidgetLater(Guid.NewGuid()))
            .ThenThrows<TimeoutException>());

        Assert.Contains("threw AggregateException [WidgetException]", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A stream assertion on a stream with no events says so plainly instead of printing an empty list.")]
    public async Task Stream_assertion_on_an_empty_stream_says_no_events()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamHasEvent<WidgetCreated>(id));

        Assert.Contains($"no WidgetCreated in the stream.{Environment.NewLine}Actual events:{Environment.NewLine}  (no events)", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "ThenPublished after a command that sent nothing says so plainly instead of printing an empty list.")]
    public async Task ThenPublished_when_nothing_was_sent_says_no_messages()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(id, "Gear"))
            .ThenPublished<WidgetPublishedNotice>());

        Assert.Contains("did not send WidgetPublishedNotice.", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("matching the predicate", failure.Message, StringComparison.Ordinal);
        Assert.EndsWith("(no messages)", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A failed ThenResult on a null response shows 'null' rather than an empty or confusing payload.")]
    public async Task Failed_ThenResult_on_a_null_response_shows_null()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => new FindWidget(Guid.NewGuid()))
            .ThenResult(r => r is not null));

        Assert.EndsWith($"Actual result:{Environment.NewLine}  null", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Generic response types are named readably in labels and failures, e.g. IReadOnlyList<String>.")]
    public async Task Generic_response_types_are_named_readably()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .WhenQuery<IReadOnlyList<string>>(() => new GetWidgetById(id)));

        Assert.StartsWith("step 2 WhenQuery<IReadOnlyList<String>>(GetWidgetById)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("expected the handler to return IReadOnlyList<String>, but it returned WidgetSummary", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A query with no handler is a setup problem: it fails at once and ThenThrows can't hide it.")]
    public async Task Missing_query_handler_fails_at_once_and_cannot_be_claimed()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new UnhandledCommand())
            .ThenThrows<Exception>());

        Assert.StartsWith(
            "step 1 WhenQuery<WidgetSummary>(UnhandledCommand): no Wolverine handler is registered for UnhandledCommand",
            failure.Message,
            StringComparison.Ordinal);
        Assert.NotNull(failure.InnerException);
    }
}
