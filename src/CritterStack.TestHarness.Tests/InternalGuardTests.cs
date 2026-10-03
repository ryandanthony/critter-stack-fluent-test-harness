// -----------------------------------------------------------------------
// <copyright file="InternalGuardTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Internal;
using CritterStack.TestHarness.Internal.Steps;
using CritterStack.TestHarness.Tests.Infrastructure;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Defensive paths the fluent API can't reach, found by code coverage: they protect against harness bugs and
/// payloads the store can't serialize, so they are driven through the internals directly.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class InternalGuardTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "HarnessAssertionException follows the standard exception constructor pattern, so callers and tools can create it.")]
    public void HarnessAssertionException_has_the_standard_constructors()
    {
        var empty = new HarnessAssertionException();
        var withMessage = new HarnessAssertionException("step 1 When: boom");
        var inner = new InvalidOperationException("inner");
        var withInner = new HarnessAssertionException("step 1 When: boom", inner);

        Assert.Null(empty.InnerException);
        Assert.Equal("step 1 When: boom", withMessage.Message);
        Assert.Null(withMessage.InnerException);
        Assert.Same(inner, withInner.InnerException);
    }

    [Fact]
    [Trait("business_value", "A payload the store can't serialize still produces a readable failure message instead of masking the real failure.")]
    public void Unserializable_payload_is_described_instead_of_throwing()
    {
        var context = new ScenarioContext(fixture.Host, new HarnessOptions(), tenantId: null);

        var json = PayloadFormatter.Json(context, typeof(string));

        Assert.StartsWith("(could not serialize: ", json, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "If the runner ever forgets to snapshot a stream before a When, the appended-event assertion says it's a harness bug rather than reporting wrong events.")]
    public async Task Appended_assertion_without_a_snapshot_reports_a_harness_bug()
    {
        var context = new ScenarioContext(fixture.Host, new HarnessOptions(), tenantId: null)
        {
            CurrentStepLabel = "step 2 ThenAppendedCount",
        };
        var step = StreamAssertionStep.ForCount(Guid.NewGuid(), StreamScope.Appended, 0);

        var failure = await Assert.ThrowsAsync<HarnessAssertionException>(() => step.ExecuteAsync(context, Ct));

        Assert.Equal("step 2 ThenAppendedCount: no stream snapshot was taken before the When; this is a harness bug", failure.Message);
    }

    [Fact]
    [Trait("business_value", "Exception matching for ThenThrows finds the type directly, inside an AggregateException, or reports no match.")]
    public void FindMatch_handles_direct_aggregate_and_unrelated_exceptions()
    {
        var direct = new TimeoutException("direct");
        var nested = new TimeoutException("nested");
        var aggregate = new AggregateException(new InvalidOperationException(), new AggregateException(nested));

        Assert.Same(direct, ExceptionClassifier.FindMatch<TimeoutException>(direct));
        Assert.Same(nested, ExceptionClassifier.FindMatch<TimeoutException>(aggregate));
        Assert.Null(ExceptionClassifier.FindMatch<TimeoutException>(new InvalidOperationException()));
        Assert.Null(ExceptionClassifier.FindMatch<ArgumentException>(aggregate));
    }

    [Fact]
    [Trait("business_value", "A handler's own early TimeoutException is never mistaken for a tracking timeout, even if it is wrapped by the tracked session.")]
    public void Early_wrapped_timeout_is_a_handler_exception_not_infrastructure()
    {
        var handler = new TimeoutException("supplier timed out");
        var wrapped = new TimeoutException("This TrackedSession timed out", handler);
        var tracking = TimeSpan.FromSeconds(10);

        Assert.Null(ExceptionClassifier.InfrastructureReason(wrapped, typeof(string), tracking, TimeSpan.FromSeconds(1)));
        Assert.NotNull(ExceptionClassifier.InfrastructureReason(wrapped, typeof(string), tracking, TimeSpan.FromSeconds(9.5)));
        Assert.Same(handler, ExceptionClassifier.HandlerException(wrapped));
        Assert.Same(handler, ExceptionClassifier.HandlerException(handler));
    }
}
