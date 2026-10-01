// -----------------------------------------------------------------------
// <copyright file="StepOrderingTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Captured exceptions, ThenThrows and the unclaimed-exception rule.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class StepOrderingTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "Expected handler failures can be asserted on, including their details.")]
    public async Task ThenThrows_claims_a_handler_exception()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>(e => e.Message.EndsWith("failed", StringComparison.Ordinal))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Once the exception is claimed, later assertions run normally and can prove nothing was written.")]
    public async Task ThenThrows_first_then_ThenNotAppended_passes()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .When(() => new FailWidget(id))
            .ThenThrows<WidgetException>()
            .ThenNotAppended<WidgetRenamed>(id)
            .ThenAppendedCount(id, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Expected query failures can be asserted on.")]
    public async Task WhenQuery_for_an_unknown_widget_can_be_claimed()
    {
        await TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()))
            .ThenThrows<WidgetNotFoundException>()
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An exception from a cascaded handler can still be asserted on by its own type.")]
    public async Task Cascaded_handler_exception_is_claimable_by_its_type()
    {
        await TestHarness.Setup(fixture.Host)
            .When(() => new FailWidgetLater(Guid.NewGuid()))
            .ThenThrows<WidgetException>(e => e.Message.StartsWith("cascaded", StringComparison.Ordinal))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A handler's own TimeoutException is behaviour under test and can be asserted on.")]
    public async Task Handler_timeout_exception_is_claimable()
    {
        await TestHarness.Setup(fixture.Host)
            .When(() => new TimeOutWidget(Guid.NewGuid()))
            .ThenThrows<TimeoutException>(e => e.Message.Contains("supplier", StringComparison.Ordinal))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An unexpected command failure fails the test with the real exception attached.")]
    public async Task Unclaimed_when_exception_fails_at_end_of_scenario()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(Guid.NewGuid())));

        Assert.StartsWith("end of scenario: step 1 When(FailWidget) threw WidgetException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "An unexpected query failure fails the test with the real exception attached.")]
    public async Task Unclaimed_query_exception_fails_the_scenario()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid())));

        Assert.Contains("threw WidgetNotFoundException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetNotFoundException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "When a command throws, the failure names the exception, not a misleading 'no event appended'.")]
    public async Task Unclaimed_exception_fails_at_the_first_other_then()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new FailWidget(id))
            .ThenAppended<WidgetRenamed>(id)
            .ThenThrows<WidgetException>());

        Assert.StartsWith("step 2 ThenAppended<WidgetRenamed>", failure.Message, StringComparison.Ordinal);
        Assert.Contains("When(FailWidget) threw WidgetException", failure.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("no WidgetRenamed", failure.Message, StringComparison.Ordinal);
        Assert.IsType<WidgetException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A ThenThrows that expects an exception which never happened fails clearly.")]
    public async Task ThenThrows_fails_when_nothing_was_thrown()
    {
        var id = Guid.NewGuid();
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(id, "Gear"))
            .ThenThrows<WidgetException>());

        Assert.Contains("expected WidgetException, but step 1 When(RenameWidget) threw nothing", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "ThenResult after a claimed query failure says there is no result instead of crashing on null.")]
    public async Task ThenResult_after_a_query_that_threw_fails_clearly()
    {
        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()))
            .ThenThrows<WidgetNotFoundException>()
            .ThenResult(r => r.Name.Length > 0));

        Assert.Contains("threw WidgetNotFoundException, so there is no result", failure.Message, StringComparison.Ordinal);
    }
}
