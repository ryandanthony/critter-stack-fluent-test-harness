// -----------------------------------------------------------------------
// <copyright file="IScenarioStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// One recorded step of a scenario.
/// </summary>
internal interface IScenarioStep
{
    /// <summary>
    /// Gets the kind of step.
    /// </summary>
    /// <value>The step kind.</value>
    StepKind Kind { get; }

    /// <summary>
    /// Describes the step for failure messages, for example <c>ThenAppended&lt;WidgetRenamed&gt;(id)</c>.
    /// </summary>
    /// <returns>The description.</returns>
    string Describe();

    /// <summary>
    /// Runs the step.
    /// </summary>
    /// <param name="context">The shared scenario state.</param>
    /// <param name="cancellationToken">Cancels the step.</param>
    /// <returns>A task that completes when the step has run.</returns>
    Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken);
}
