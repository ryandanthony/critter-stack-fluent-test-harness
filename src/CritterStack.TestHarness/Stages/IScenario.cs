// -----------------------------------------------------------------------
// <copyright file="IScenario.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Runtime.CompilerServices;

namespace CritterStack.TestHarness;

/// <summary>
/// A scenario that has at least one <c>When</c> or <c>WhenQuery</c> and can therefore be run.
/// Nothing runs until <see cref="RunAsync"/> is called or the scenario is awaited.
/// </summary>
public interface IScenario
{
    /// <summary>
    /// Runs every recorded step in order.
    /// </summary>
    /// <param name="cancellationToken">Cancels the run between steps and inside Marten and Wolverine calls.</param>
    /// <returns>A task that completes when every step has passed.</returns>
    /// <exception cref="ScenarioAssertionException">A step failed. The message names the step.</exception>
    Task RunAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Lets a scenario be awaited directly. Equivalent to awaiting <see cref="RunAsync"/> with no cancellation token.
    /// </summary>
    /// <returns>An awaiter for the scenario run.</returns>
    TaskAwaiter GetAwaiter();
}
