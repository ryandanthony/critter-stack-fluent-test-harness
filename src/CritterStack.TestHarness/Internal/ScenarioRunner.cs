// -----------------------------------------------------------------------
// <copyright file="ScenarioRunner.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Internal.Steps;
using Microsoft.Extensions.Hosting;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Runs recorded steps in order and enforces the unclaimed-exception rule.
/// </summary>
internal static class ScenarioRunner
{
    /// <summary>
    /// Runs a scenario.
    /// </summary>
    /// <param name="steps">The recorded steps.</param>
    /// <param name="host">The host under test.</param>
    /// <param name="options">The harness options.</param>
    /// <param name="tenantId">The scenario's tenant, or null.</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The final context, for diagnostics.</returns>
    internal static async Task<ScenarioContext> RunAsync(
        IReadOnlyList<IScenarioStep> steps,
        IHost host,
        HarnessOptions options,
        string? tenantId,
        CancellationToken cancellationToken)
    {
        PlanSnapshots(steps);

        var context = new ScenarioContext(host, options, tenantId);
        for (var i = 0; i < steps.Count; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var step = steps[i];
            var label = $"step {i + 1} {step.Describe()}";
            if (step.Kind != StepKind.ThenThrows)
            {
                FailIfUnclaimed(context, label);
            }

            context.CurrentStepNumber = i + 1;
            context.CurrentStepLabel = label;
            await step.ExecuteAsync(context, cancellationToken).ConfigureAwait(false);
        }

        FailIfUnclaimed(context, "end of scenario");
        return context;
    }

    /// <summary>
    /// Tells each <c>When</c> which streams the appended-event assertions after it will read, so it can snapshot
    /// their versions before invoking the command.
    /// </summary>
    private static void PlanSnapshots(IReadOnlyList<IScenarioStep> steps)
    {
        WhenCommandStep? current = null;
        foreach (var step in steps)
        {
            if (step is WhenCommandStep command)
            {
                command.SnapshotIds.Clear();
                current = command;
            }
            else if (step is IActionStep)
            {
                current = null;
            }
            else if (current is not null && step is StreamAssertionStep { Scope: StreamScope.Appended } appended)
            {
                current.SnapshotIds.Add(appended.StreamId);
            }
            else
            {
                // Givens and other Thens need no snapshot.
            }
        }
    }

    private static void FailIfUnclaimed(ScenarioContext context, string label)
    {
        if (context.HasUnclaimedException)
        {
            var exception = context.ActionException!;
            var type = ExceptionClassifier.Describe(exception);
            throw Failure.Create(
                label,
                $"{context.ActionLabel} threw {type} and no ThenThrows claimed it. To expect it, put ThenThrows<...>() straight after that step.",
                exception);
        }
    }
}
