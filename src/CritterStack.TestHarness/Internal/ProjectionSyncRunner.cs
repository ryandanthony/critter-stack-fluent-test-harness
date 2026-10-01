// -----------------------------------------------------------------------
// <copyright file="ProjectionSyncRunner.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Marten.Events;
using Marten.Events.Daemon.Coordination;
using Microsoft.Extensions.DependencyInjection;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Runs the configured <see cref="ProjectionSync"/> before a <c>When</c> or <c>WhenQuery</c>, but only when a Given or
/// <c>When</c> has written something since the last sync.
/// </summary>
internal static class ProjectionSyncRunner
{
    /// <summary>
    /// Syncs projections if anything was written since the last sync.
    /// </summary>
    /// <param name="context">The scenario context.</param>
    /// <param name="cancellationToken">Cancels the sync.</param>
    /// <returns>A task that completes when projections are current.</returns>
    /// <exception cref="ScenarioAssertionException">The sync failed or timed out.</exception>
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
        try
        {
            await SyncAsync(context, mode, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"projection sync ({mode}) failed with {ExceptionClassifier.Describe(ex)} (ProjectionTimeout {context.Options.ProjectionTimeout})",
                ex);
        }
    }

    private static async Task SyncAsync(ScenarioContext context, ProjectionSync mode, CancellationToken cancellationToken)
    {
        var timeout = context.Options.ProjectionTimeout;

        if (mode == ProjectionSync.WaitForNonStale)
        {
            await context.Host.WaitForNonStaleProjectionDataAsync(timeout).ConfigureAwait(false);
            return;
        }

        // CatchUp. ForceAllMartenDaemonActivityToCatchUpAsync needs the daemon's coordinator; without a daemon,
        // build a temporary one. Never build one on a host that has a daemon: two daemons fight over progress.
        if (context.Host.Services.GetService<IProjectionCoordinator>() is not null)
        {
            using var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutSource.CancelAfter(timeout);
            var errors = await context.Host
                .ForceAllMartenDaemonActivityToCatchUpAsync(timeoutSource.Token)
                .ConfigureAwait(false);
            if (errors.Count > 0)
            {
                throw new AggregateException("Marten reported projection errors while catching up.", errors);
            }

            return;
        }

        using var daemon = await context.Store.BuildProjectionDaemonAsync().ConfigureAwait(false);
        await daemon.CatchUpAsync(timeout, cancellationToken).ConfigureAwait(false);
    }
}
