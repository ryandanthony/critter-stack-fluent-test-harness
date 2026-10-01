// -----------------------------------------------------------------------
// <copyright file="Scenarios.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Helpers for tests that expect a scenario to fail.
/// </summary>
internal static class Scenarios
{
    /// <summary>
    /// Runs a scenario and returns the <see cref="ScenarioAssertionException"/> it must throw.
    /// </summary>
    /// <param name="scenario">The scenario.</param>
    /// <returns>The failure.</returns>
    internal static Task<ScenarioAssertionException> FailsAsync(IScenario scenario) =>
        Assert.ThrowsAsync<ScenarioAssertionException>(() => scenario.RunAsync(TestContext.Current.CancellationToken));

    /// <summary>
    /// A string comparison that analyzers accept inside predicates.
    /// </summary>
    /// <param name="actual">The actual value.</param>
    /// <param name="expected">The expected value.</param>
    /// <returns>True when equal (ordinal).</returns>
    internal static bool Is(string? actual, string expected) => string.Equals(actual, expected, StringComparison.Ordinal);
}
