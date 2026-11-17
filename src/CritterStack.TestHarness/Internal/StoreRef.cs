// -----------------------------------------------------------------------
// <copyright file="StoreRef.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Marten;
using Marten.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MainCoordinator = Marten.Events.Daemon.Coordination.IProjectionCoordinator;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Names a Marten store a scenario uses: the host's main <see cref="IDocumentStore"/>, or an ancillary store
/// registered with <c>AddMartenStore&lt;TStore&gt;()</c>. Built inside the generic <see cref="For{TStore}"/>, so the
/// typed Marten calls an ancillary store needs are captured as delegates and the runner never reflects over generic
/// methods (ADR-0020). Two refs to the same store type are equal.
/// </summary>
internal sealed class StoreRef : IEquatable<StoreRef>
{
    private readonly Func<IServiceProvider, IDocumentStore> _resolve;
    private readonly Func<IServiceProvider, bool> _hasCoordinator;
    private readonly Func<IHost, CancellationToken, Task<IReadOnlyList<Exception>>> _forceCatchUp;
    private readonly Func<IHost, TimeSpan, Task> _waitForNonStale;

    private StoreRef(
        Type storeType,
        Func<IServiceProvider, IDocumentStore> resolve,
        Func<IServiceProvider, bool> hasCoordinator,
        Func<IHost, CancellationToken, Task<IReadOnlyList<Exception>>> forceCatchUp,
        Func<IHost, TimeSpan, Task> waitForNonStale)
    {
        StoreType = storeType;
        _resolve = resolve;
        _hasCoordinator = hasCoordinator;
        _forceCatchUp = forceCatchUp;
        _waitForNonStale = waitForNonStale;
    }

    /// <summary>Gets the host's main store.</summary>
    /// <value>The main store ref.</value>
    internal static StoreRef Main { get; } = new(
        typeof(IDocumentStore),
        services => services.GetRequiredService<IDocumentStore>(),
        services => services.GetService<MainCoordinator>() is not null,
        (host, token) => host.ForceAllMartenDaemonActivityToCatchUpAsync(token),
        (host, timeout) => host.WaitForNonStaleProjectionDataAsync(timeout));

    /// <summary>Gets the store type: <see cref="IDocumentStore"/> for the main store.</summary>
    /// <value>The store type.</value>
    internal Type StoreType { get; }

    /// <summary>Gets a value indicating whether this is the main store.</summary>
    /// <value>True for the main store.</value>
    internal bool IsMain => StoreType == typeof(IDocumentStore);

    /// <summary>Gets the store's name for step labels and failures.</summary>
    /// <value>For example <c>ILedgerStore</c>.</value>
    internal string Name => Failure.TypeName(StoreType);

    /// <summary>
    /// Creates a ref for <typeparamref name="TStore"/>. <see cref="IDocumentStore"/> means the main store.
    /// </summary>
    /// <typeparam name="TStore">The store type.</typeparam>
    /// <returns>The store ref.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The store is chosen by type; there is no value to infer it from.")]
    internal static StoreRef For<TStore>()
        where TStore : class, IDocumentStore
    {
        if (typeof(TStore) == typeof(IDocumentStore))
        {
            return Main;
        }

        return new(
            typeof(TStore),
            services => services.GetRequiredService<TStore>(),
            services => services.GetService<Marten.Events.Daemon.Coordination.IProjectionCoordinator<TStore>>() is not null,
            (host, token) => host.ForceAllMartenDaemonActivityToCatchUpAsync<TStore>(token),
            (host, timeout) => host.WaitForNonStaleProjectionDataAsync<TStore>(timeout));
    }

    /// <summary>
    /// Resolves the store from the host, failing with a message that says how to register it.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>The store.</returns>
    /// <exception cref="InvalidOperationException">The store isn't registered.</exception>
    internal IDocumentStore Resolve(IServiceProvider services)
    {
        try
        {
            return _resolve(services);
        }
        catch (InvalidOperationException ex)
        {
            var how = IsMain ? "AddMarten()" : $"AddMartenStore<{Name}>()";
            throw new InvalidOperationException($"{Name} is not registered in the host. Register it with {how} before using it in a scenario.", ex);
        }
    }

    /// <summary>
    /// Tells whether the host runs a projection coordinator (an async daemon) for this store.
    /// </summary>
    /// <param name="services">The host's services.</param>
    /// <returns>True when the forced catch-up path can be used.</returns>
    internal bool HasCoordinator(IServiceProvider services) => _hasCoordinator(services);

    /// <summary>
    /// Forces the host's daemon for this store to catch up.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="cancellationToken">Cancels the catch-up.</param>
    /// <returns>The apply errors Marten reported.</returns>
    internal Task<IReadOnlyList<Exception>> ForceCatchUpAsync(IHost host, CancellationToken cancellationToken) =>
        _forceCatchUp(host, cancellationToken);

    /// <summary>
    /// Waits for the host's daemon for this store to reach the high water mark.
    /// </summary>
    /// <param name="host">The host.</param>
    /// <param name="timeout">How long to wait.</param>
    /// <returns>A task that completes when the store's projections are current.</returns>
    internal Task WaitForNonStaleAsync(IHost host, TimeSpan timeout) => _waitForNonStale(host, timeout);

    /// <inheritdoc/>
    public bool Equals(StoreRef? other) => other is not null && StoreType == other.StoreType;

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as StoreRef);

    /// <inheritdoc/>
    public override int GetHashCode() => StoreType.GetHashCode();

    /// <inheritdoc/>
    public override string ToString() => Name;
}
