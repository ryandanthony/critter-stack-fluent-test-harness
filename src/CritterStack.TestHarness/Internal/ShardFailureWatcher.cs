// -----------------------------------------------------------------------
// <copyright file="ShardFailureWatcher.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Collections.Concurrent;
using JasperFx.Events.Projections;
using Marten;
using Marten.Storage;
using ShardAction = JasperFx.Events.Daemon.ShardAction;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Watches a daemon host's shard state for the length of one scenario and records every shard that pauses with an
/// exception. The daemon can hit a bad event on its own background run, outside a forced catch-up, and Marten then
/// reports nothing from the catch-up. Watching the tracker catches those pauses whenever they happen.
/// </summary>
internal sealed class ShardFailureWatcher : IObserver<ShardState>, IDisposable
{
    /// <summary>
    /// After a catch-up the coordinator restarts its shards, and a shard that hits a bad event pauses a moment
    /// later. This is how long to wait for that before deciding there was no failure.
    /// </summary>
    private static readonly TimeSpan SettleTime = TimeSpan.FromMilliseconds(500);

    private readonly ConcurrentDictionary<string, Exception> _failures = new(StringComparer.Ordinal);
    private readonly List<IDisposable> _subscriptions = [];
    private readonly IMartenDatabase[] _databases;
    private readonly string[] _shards;

    private ShardFailureWatcher(IMartenDatabase[] databases, string[] shards)
    {
        _databases = databases;
        _shards = shards;
    }

    /// <summary>
    /// Starts watching a store if the host runs its async daemon and the scenario uses <see cref="ProjectionSync.CatchUp"/>.
    /// </summary>
    /// <param name="host">The host under test.</param>
    /// <param name="options">The harness options.</param>
    /// <param name="storeRef">Which store to watch.</param>
    /// <param name="store">The resolved store.</param>
    /// <returns>The watcher, or null when there is nothing to watch.</returns>
    internal static async Task<ShardFailureWatcher?> StartAsync(Microsoft.Extensions.Hosting.IHost host, HarnessOptions options, StoreRef storeRef, IDocumentStore store)
    {
        if (options.ProjectionSync != ProjectionSync.CatchUp
            || !storeRef.HasCoordinator(host.Services)
            || store is not DocumentStore documentStore)
        {
            return null;
        }

        var shards = documentStore.Options.Projections.AllShards().Select(s => s.Name.Identity).ToArray();
        if (shards.Length == 0)
        {
            return null;
        }

        var databases = (await documentStore.Storage.AllDatabases().ConfigureAwait(false)).ToArray();
        var watcher = new ShardFailureWatcher(databases, shards);
        foreach (var database in databases)
        {
            watcher._subscriptions.Add(database.Tracker.Subscribe(watcher));
        }

        return watcher;
    }

    /// <summary>
    /// Returns the failures seen so far, first giving a shard that is about to pause (its event is already loaded but
    /// not applied) the time to do so. Waits only while some shard is still behind the high water mark.
    /// </summary>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>The failures, oldest first per shard.</returns>
    internal async Task<IReadOnlyList<Exception>> WaitForFailuresAsync(CancellationToken cancellationToken)
    {
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        while (_failures.IsEmpty && stopwatch.Elapsed < SettleTime && AnyShardBehind())
        {
            await Task.Delay(TimeSpan.FromMilliseconds(25), cancellationToken).ConfigureAwait(false);
        }

        return [.. _failures.Values];
    }

    /// <inheritdoc/>
    public void OnNext(ShardState value)
    {
        if (value is { Action: ShardAction.Paused or ShardAction.Stopped, Exception: { } exception })
        {
            _failures.TryAdd(value.ShardName, exception);
        }
    }

    /// <inheritdoc/>
    public void OnError(Exception error)
    {
        // The tracker never reports its own errors here.
    }

    /// <inheritdoc/>
    public void OnCompleted()
    {
        // Nothing to clean up until Dispose.
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        _subscriptions.Clear();
    }

    private bool AnyShardBehind() =>
        Array.Exists(_databases, database => Array.Exists(_shards, shard =>
            !_failures.ContainsKey(shard)
            && (database.Tracker.CurrentState(shard)?.Sequence ?? 0) < database.Tracker.HighWaterMark));
}
