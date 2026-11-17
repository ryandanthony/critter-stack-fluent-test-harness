// -----------------------------------------------------------------------
// <copyright file="ProjectionSyncRunner.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Runs the configured <see cref="ProjectionSync"/> before a <c>When</c> or <c>WhenQuery</c>, but only when a Given or
/// <c>When</c> has written something since the last sync. Every store in the scenario's sync set is brought up to
/// date, main store first, each by the rule of ADR-0007 decided for that store (ADR-0020).
/// </summary>
internal static class ProjectionSyncRunner
{
    /// <summary>
    /// Syncs projections if anything was written since the last sync.
    /// </summary>
    /// <param name="context">The scenario context.</param>
    /// <param name="cancellationToken">Cancels the sync.</param>
    /// <returns>A task that completes when projections are current.</returns>
    /// <exception cref="HarnessAssertionException">The sync failed or timed out.</exception>
    internal static async Task SyncIfDirtyAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        if (!context.ProjectionsDirty)
        {
            return;
        }

        context.ProjectionsDirty = false;
        var mode = context.Options.ProjectionSync;
        if (mode == ProjectionSync.None)
        {
            return;
        }

        context.SyncCount++;
        foreach (var store in context.SyncStores)
        {
            try
            {
                await SyncAsync(context, store, mode, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                var which = store.IsMain ? string.Empty : $" of {store.Name}";
                throw Failure.Create(
                    context.CurrentStepLabel,
                    $"projection sync ({mode}){which} failed with {ExceptionClassifier.Describe(ex)} (ProjectionTimeout {context.Options.ProjectionTimeout})",
                    ex);
            }
        }
    }

    private static async Task SyncAsync(ScenarioContext context, StoreRef store, ProjectionSync mode, CancellationToken cancellationToken)
    {
        var timeout = context.Options.ProjectionTimeout;

        if (mode == ProjectionSync.WaitForNonStale)
        {
            // Marten throws "No event activity" for a store without any events. With several stores in a scenario,
            // one of them may hold none yet, and then nothing in it can be stale.
            if (await HasEventsAsync(context.StoreFor(store), cancellationToken).ConfigureAwait(false))
            {
                await store.WaitForNonStaleAsync(context.Host, timeout).ConfigureAwait(false);
            }

            return;
        }

        // CatchUp. Forcing the host's daemon needs that store's coordinator; without one, build a temporary daemon.
        // Never build one for a store whose daemon runs in the host: two daemons fight over progress.
        if (store.HasCoordinator(context.Host.Services))
        {
            await CatchUpRunningDaemonAsync(context, store, timeout, cancellationToken).ConfigureAwait(false);
            return;
        }

        using var daemon = await context.StoreFor(store).BuildProjectionDaemonAsync().ConfigureAwait(false);
        await daemon.CatchUpAsync(timeout, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<bool> HasEventsAsync(IDocumentStore store, CancellationToken cancellationToken)
    {
        foreach (var database in await store.Storage.AllDatabases().ConfigureAwait(false))
        {
            var statistics = await database.FetchEventStoreStatistics(cancellationToken).ConfigureAwait(false);
            // A fresh store reports sequence 1 with no events, so count them.
            if (statistics.EventCount > 0)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Catches up the host's own daemon for a store. Marten only reports an apply error that happens during the forced
    /// catch-up. If the daemon's background run reached the bad event first (before the catch-up, or while the
    /// coordinator restarts the shards after it), the shard is paused outside the catch-up and Marten reports
    /// nothing. So pauses with an exception that the store's shard watcher saw count as errors too.
    /// </summary>
    private static async Task CatchUpRunningDaemonAsync(ScenarioContext context, StoreRef store, TimeSpan timeout, CancellationToken cancellationToken)
    {
        using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeoutSource.CancelAfter(timeout);
        var errors = (await store.ForceCatchUpAsync(context.Host, timeoutSource.Token).ConfigureAwait(false)).ToList();

        if (errors.Count == 0 && context.ShardFailures.TryGetValue(store, out var watcher))
        {
            errors.AddRange(await watcher.WaitForFailuresAsync(cancellationToken).ConfigureAwait(false));
        }

        if (errors.Count > 0)
        {
            throw new AggregateException("Marten reported projection errors while catching up.", errors);
        }
    }
}
