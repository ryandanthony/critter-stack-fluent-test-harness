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
    /// <param name="extraStores">Stores to sync even when no step names them (<c>HarnessOptions.UseStore</c>).</param>
    /// <param name="cancellationToken">Cancels the run.</param>
    /// <returns>The final context, for diagnostics.</returns>
    internal static async Task<ScenarioContext> RunAsync(
        IReadOnlyList<IScenarioStep> steps,
        IHost host,
        HarnessOptions options,
        string? tenantId,
        IReadOnlyList<StoreRef> extraStores,
        CancellationToken cancellationToken)
    {
        PlanSnapshots(steps);

        var stepStores = steps.OfType<IStreamStep>().Select(s => s.Stream.Store);
        var context = new ScenarioContext(host, options, tenantId, [.. stepStores, .. extraStores]);
        CheckStreamIdentity(steps, context);
        try
        {
            foreach (var store in context.SyncStores)
            {
                var watcher = await ShardFailureWatcher.StartAsync(host, options, store, context.StoreFor(store)).ConfigureAwait(false);
                if (watcher is not null)
                {
                    context.ShardFailures[store] = watcher;
                }
            }

            await RunStepsAsync(steps, context, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            foreach (var watcher in context.ShardFailures.Values)
            {
                watcher.Dispose();
            }
        }

        return context;
    }

    private static async Task RunStepsAsync(IReadOnlyList<IScenarioStep> steps, ScenarioContext context, CancellationToken cancellationToken)
    {
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
    }

    /// <summary>
    /// Tells each <c>When</c> and <c>WhenHttp</c> which streams the appended-event assertions after it will read, so it
    /// can snapshot their versions before running, and tells each <c>WhenHttp</c> whether a <c>ThenStatusCode</c>
    /// follows it.
    /// </summary>
    private static void PlanSnapshots(IReadOnlyList<IScenarioStep> steps)
    {
        ISnapshottingStep? current = null;
        foreach (var step in steps)
        {
            if (step is ISnapshottingStep snapshotting)
            {
                snapshotting.SnapshotIds.Clear();
                if (snapshotting is WhenHttpStep http)
                {
                    http.StatusAsserted = false;
                }

                current = snapshotting;
            }
            else if (step is IActionStep)
            {
                current = null;
            }
            else if (current is not null && step is StreamAssertionStep { Scope: StreamScope.Appended } appended)
            {
                current.SnapshotIds.Add(appended.Stream);
            }
            else if (current is WhenHttpStep http && step is StatusCodeAssertionStep)
            {
                http.StatusAsserted = true;
            }
            else
            {
                // Givens and other Thens need no planning.
            }
        }
    }

    /// <summary>
    /// Fails before anything runs if a stream step's id is a Guid on a store keyed by string, or the reverse, checked
    /// against the step's own store. Marten doesn't catch every case itself (<c>FetchStreamStateAsync</c> with a
    /// string on a Guid store returns null), and its messages vary by call (ADR-0019).
    /// </summary>
    private static void CheckStreamIdentity(IReadOnlyList<IScenarioStep> steps, ScenarioContext context)
    {
        for (var i = 0; i < steps.Count; i++)
        {
            if (steps[i] is IStreamStep stream
                && stream.Stream.Key.Mismatch(context.StoreFor(stream.Stream.Store).Options.Events.StreamIdentity) is { } reason)
            {
                throw Failure.Create($"step {i + 1} {stream.Describe()}", reason);
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
