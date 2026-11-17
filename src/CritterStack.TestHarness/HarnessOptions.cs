// -----------------------------------------------------------------------
// <copyright file="HarnessOptions.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// Everything that varies between services. Set through the <c>configure</c> callback of
/// <see cref="TestHarness.Setup"/>.
/// </summary>
public sealed class HarnessOptions
{
    /// <summary>
    /// Gets or sets how async projections are brought up to date before each <c>When</c> and <c>WhenQuery</c>.
    /// </summary>
    /// <value>Defaults to <c>ProjectionSync.CatchUp</c>.</value>
    public ProjectionSync ProjectionSync { get; set; } = ProjectionSync.CatchUp;

    /// <summary>
    /// Gets or sets how long one projection sync may take before the scenario fails.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan ProjectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long Wolverine's tracked session waits for a <c>When</c> and the messages it cascades.
    /// </summary>
    /// <value>Defaults to 10 seconds.</value>
    public TimeSpan TrackingTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets a value indicating whether Wolverine tracking also waits for messages sent to external
    /// transports. Only useful when the host runs real brokers.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool IncludeExternalMessages { get; set; }

    /// <summary>
    /// Gets or sets how a <c>WhenHttp</c> request carries the scenario's tenant, for scenarios that call
    /// <c>ForTenant</c>. Services detect the tenant differently (a header, a claim, a route segment), so the harness
    /// can't guess. A <c>ForTenant</c> scenario with a <c>WhenHttp</c> and no <c>HttpTenant</c> fails at once.
    /// </summary>
    /// <value>Called with the Alba request and the tenant id, after the test's own request configuration.
    /// Defaults to <see langword="null"/>. For example <c>(x, tenant) =&gt; x.WithRequestHeader("tenant", tenant)</c>.</value>
    public Action<Alba.Scenario, string>? HttpTenant { get; set; }

    /// <summary>
    /// Gets the ancillary stores to sync besides the main store and the stores the scenario names with
    /// <c>InStore</c>.
    /// </summary>
    /// <value>In the order they were added, without duplicates.</value>
    internal List<Internal.StoreRef> ExtraStores { get; } = [];

    /// <summary>
    /// Adds an ancillary Marten store (registered with <c>AddMartenStore&lt;TStore&gt;()</c>) to the stores whose async
    /// projections are brought up to date before each action, for a store the scenario never names with
    /// <c>InStore</c>: for example one only a <c>[MartenStore]</c> handler writes and only a query reads. Stores named by
    /// <c>InStore</c> and the main store are always synced.
    /// </summary>
    /// <typeparam name="TStore">The store type. <see cref="Marten.IDocumentStore"/> (the main store) is accepted and has no
    /// effect.</typeparam>
    /// <returns>These options, for chaining.</returns>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The store is chosen by type; there is no value to infer it from.")]
    public HarnessOptions UseStore<TStore>()
        where TStore : class, Marten.IDocumentStore
    {
        var store = Internal.StoreRef.For<TStore>();
        if (!store.IsMain && !ExtraStores.Contains(store))
        {
            ExtraStores.Add(store);
        }

        return this;
    }
}
