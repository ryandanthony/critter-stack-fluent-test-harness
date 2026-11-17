// -----------------------------------------------------------------------
// <copyright file="ScenarioContext.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Wolverine.Tracking;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// State shared by the steps of one scenario run.
/// </summary>
internal sealed class ScenarioContext
{
    private readonly Dictionary<StoreRef, IDocumentStore> _stores = [];

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioContext"/> class.
    /// </summary>
    /// <param name="host">The host under test.</param>
    /// <param name="options">The harness options.</param>
    /// <param name="tenantId">The scenario's tenant, or null for the default tenant.</param>
    /// <param name="syncStores">The ancillary stores to sync besides the main store, in order. Null or empty for the
    /// main store only.</param>
    internal ScenarioContext(IHost host, HarnessOptions options, string? tenantId, IReadOnlyList<StoreRef>? syncStores = null)
    {
        Host = host;
        Options = options;
        TenantId = tenantId;
        Store = host.Services.GetRequiredService<IDocumentStore>();
        _stores[StoreRef.Main] = Store;
        SyncStores = [StoreRef.Main, .. (syncStores ?? []).Where(s => !s.IsMain).Distinct()];
    }

    /// <summary>Gets the host under test.</summary>
    /// <value>The host.</value>
    internal IHost Host { get; }

    /// <summary>Gets the harness options.</summary>
    /// <value>The options.</value>
    internal HarnessOptions Options { get; }

    /// <summary>Gets the scenario's tenant.</summary>
    /// <value>The tenant id, or null for Marten's default tenant.</value>
    internal string? TenantId { get; }

    /// <summary>Gets the host's main document store.</summary>
    /// <value>The main store. Used for payloads that don't belong to a stream, such as query results.</value>
    internal IDocumentStore Store { get; }

    /// <summary>Gets the stream ids this scenario's Givens have already started, per store.</summary>
    /// <value>The started streams.</value>
    internal HashSet<StreamAddress> StartedStreams { get; } = [];

    /// <summary>Gets the stores projection sync brings up to date, main store first.</summary>
    /// <value>The sync set.</value>
    internal IReadOnlyList<StoreRef> SyncStores { get; }

    /// <summary>Gets the watchers that record daemon shards pausing with an error during this scenario, one per
    /// synced store whose daemon runs in the host.</summary>
    /// <value>Store to watcher; empty when no store has a daemon or the sync mode isn't <c>CatchUp</c>.</value>
    internal Dictionary<StoreRef, ShardFailureWatcher> ShardFailures { get; } = [];

    /// <summary>Gets each snapshotted stream's version, taken just before the most recent <c>When</c>.</summary>
    /// <value>Stream to version.</value>
    internal Dictionary<StreamAddress, long> Snapshots { get; } = [];

    /// <summary>Gets or sets a value indicating whether anything was written since the last projection sync.</summary>
    /// <value>True when a sync is needed.</value>
    internal bool ProjectionsDirty { get; set; }

    /// <summary>Gets or sets how many projection syncs actually ran.</summary>
    /// <value>The sync count.</value>
    internal int SyncCount { get; set; }

    /// <summary>Gets or sets the label of the step currently running.</summary>
    /// <value>For example <c>step 3 When(RenameWidget)</c>.</value>
    internal string CurrentStepLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets the 1-based number of the step currently running.</summary>
    /// <value>The step number.</value>
    internal int CurrentStepNumber { get; set; }

    /// <summary>
    /// Rebuilds <see cref="CurrentStepLabel"/> after a step has learned more about itself, such as a <c>When</c>
    /// learning its command type once the factory has run.
    /// </summary>
    /// <param name="step">The running step.</param>
    internal void Relabel(IScenarioStep step)
    {
        ArgumentNullException.ThrowIfNull(step);
        CurrentStepLabel = $"step {CurrentStepNumber} {step.Describe()}";
    }

    /// <summary>Gets the label of the most recent <c>When</c>, <c>WhenQuery</c> or <c>WhenHttp</c>.</summary>
    /// <value>The action label, or null before the first action.</value>
    internal string? ActionLabel { get; private set; }

    /// <summary>Gets or sets the handler exception captured from the most recent action.</summary>
    /// <value>The exception, or null.</value>
    internal Exception? ActionException { get; set; }

    /// <summary>Gets or sets a value indicating whether a <c>ThenThrows</c> has claimed <see cref="ActionException"/>.</summary>
    /// <value>True once claimed.</value>
    internal bool ActionExceptionClaimed { get; set; }

    /// <summary>Gets or sets the tracked session from the most recent <c>When</c>.</summary>
    /// <value>The tracked session, or null if the command threw.</value>
    internal ITrackedSession? TrackedSession { get; set; }

    /// <summary>Gets or sets the response of the most recent <c>WhenQuery</c>.</summary>
    /// <value>The response, already checked to be the expected type.</value>
    internal object? QueryResult { get; set; }

    /// <summary>Gets or sets the Alba result of the most recent <c>WhenHttp</c>.</summary>
    /// <value>The result, or null if the request threw or the last action wasn't a <c>WhenHttp</c>.</value>
    internal Alba.IScenarioResult? HttpResult { get; set; }

    /// <summary>
    /// Gets a value indicating whether an exception from the most recent action has not been claimed yet.
    /// </summary>
    /// <value>True when an unclaimed exception is pending.</value>
    internal bool HasUnclaimedException => ActionException is not null && !ActionExceptionClaimed;

    /// <summary>
    /// Resets the per-action state at the start of a <c>When</c> or <c>WhenQuery</c>.
    /// </summary>
    /// <param name="actionLabel">The new action's label.</param>
    internal void BeginAction(string actionLabel)
    {
        ActionLabel = actionLabel;
        ActionException = null;
        ActionExceptionClaimed = false;
        TrackedSession = null;
        QueryResult = null;
        HttpResult = null;
        Snapshots.Clear();
    }

    /// <summary>
    /// Updates <see cref="ActionLabel"/> to the current step label without resetting any action state, for a step
    /// that only learns its full description part way through running.
    /// </summary>
    internal void RelabelAction() => ActionLabel = CurrentStepLabel;

    /// <summary>
    /// Opens a writable session on <paramref name="store"/> for the scenario's tenant.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>The session.</returns>
    internal IDocumentSession OpenSession(StoreRef store)
    {
        var documentStore = StoreFor(store);
        return TenantId is null ? documentStore.LightweightSession() : documentStore.LightweightSession(TenantId);
    }

    /// <summary>
    /// Opens a read-only session on <paramref name="store"/> for the scenario's tenant.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>The session.</returns>
    internal IQuerySession OpenQuerySession(StoreRef store)
    {
        var documentStore = StoreFor(store);
        return TenantId is null ? documentStore.QuerySession() : documentStore.QuerySession(TenantId);
    }

    /// <summary>
    /// Resolves a store from the host, once per scenario.
    /// </summary>
    /// <param name="store">The store.</param>
    /// <returns>The document store.</returns>
    internal IDocumentStore StoreFor(StoreRef store)
    {
        if (!_stores.TryGetValue(store, out var documentStore))
        {
            documentStore = store.Resolve(Host.Services);
            _stores[store] = documentStore;
        }

        return documentStore;
    }
}
