// -----------------------------------------------------------------------
// <copyright file="ArgumentValidationTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// The documented argument checks on the public API. They fail when the scenario is built, not when it runs, so a
/// mistake points at the line that made it.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class ArgumentValidationTests(NoDaemonHostFixture fixture)
{
    [Fact]
    [Trait("business_value", "Setup with a null host fails at once with ArgumentNullException naming the parameter.")]
    public void Setup_rejects_a_null_host()
    {
        var ex = Assert.Throws<ArgumentNullException>(() => TestHarness.Setup(null!));

        Assert.Equal("host", ex.ParamName);
    }

    [Fact]
    [Trait("business_value", "Setup runs the configure callback once, straight away, so options are fixed before any step is recorded.")]
    public void Setup_runs_configure_once_when_called()
    {
        var calls = 0;

        _ = TestHarness.Setup(fixture.Host, _ => calls++);

        Assert.Equal(1, calls);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [Trait("business_value", "ForTenant rejects a missing tenant id instead of silently running as the default tenant.")]
    public void ForTenant_rejects_a_blank_tenant(string? tenantId)
    {
        var ex = Assert.ThrowsAny<ArgumentException>(() => TestHarness.Setup(fixture.Host).ForTenant(tenantId!));

        Assert.Equal("tenantId", ex.ParamName);
    }

    [Fact]
    [Trait("business_value", "Null factories and predicates fail when the scenario is built, at the line with the mistake.")]
    public void Null_factories_and_predicates_fail_when_the_scenario_is_built()
    {
        var start = TestHarness.Setup(fixture.Host);

        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream(Guid.NewGuid(), null!)).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream<Widget>(Guid.NewGuid(), null!)).ParamName);
        Assert.Equal("command", Assert.Throws<ArgumentNullException>(() => start.When(null!)).ParamName);
        Assert.Equal("query", Assert.Throws<ArgumentNullException>(() => start.WhenQuery<WidgetSummary>(null!)).ParamName);

        var query = TestHarness.Setup(fixture.Host).WhenQuery<WidgetSummary>(() => new GetWidgetById(Guid.NewGuid()));
        Assert.Equal("predicate", Assert.Throws<ArgumentNullException>(() => query.ThenResult(null!)).ParamName);
    }
}
