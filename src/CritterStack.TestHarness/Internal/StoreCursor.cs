// -----------------------------------------------------------------------
// <copyright file="StoreCursor.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Marten;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Tracks the store that the next stream steps a scenario records will use. A scenario starts on the main store and
/// <c>InStore&lt;TStore&gt;()</c> moves the cursor (ADR-0020).
/// </summary>
internal sealed class StoreCursor
{
    private readonly IServiceProvider _services;
    private StoreRef _current = StoreRef.Main;

    /// <summary>
    /// Initializes a new instance of the <see cref="StoreCursor"/> class.
    /// </summary>
    /// <param name="services">The host's services, to check that a store is registered.</param>
    internal StoreCursor(IServiceProvider services)
    {
        _services = services;
    }

    /// <summary>
    /// Places a stream id in the current store.
    /// </summary>
    /// <param name="key">The stream id.</param>
    /// <returns>The stream in the current store.</returns>
    internal StreamAddress Address(StreamKey key) => new(_current, key);

    /// <summary>
    /// Places a Guid stream id in the current store.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <returns>The stream in the current store.</returns>
    internal StreamAddress Address(Guid id) => Address(StreamKey.For(id));

    /// <summary>
    /// Places a string stream id in the current store.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <returns>The stream in the current store.</returns>
    /// <exception cref="ArgumentException">The id is null, empty or white space.</exception>
    internal StreamAddress Address(string id) => Address(StreamKey.For(id));

    /// <summary>
    /// Places a strongly typed stream id in the current store.
    /// </summary>
    /// <typeparam name="TId">The id type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <returns>The stream in the current store.</returns>
    /// <exception cref="ArgumentException">The id type can't be unwrapped to a Guid or string.</exception>
    internal StreamAddress TypedAddress<TId>(TId id) => Address(StreamKey.ForTyped(id));

    /// <summary>
    /// Moves the cursor to <typeparamref name="TStore"/>, checking that the host registers it.
    /// </summary>
    /// <typeparam name="TStore">The store type; <see cref="IDocumentStore"/> for the main store.</typeparam>
    /// <exception cref="InvalidOperationException">The store isn't registered in the host.</exception>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The store is chosen by type; there is no value to infer it from.")]
    internal void MoveTo<TStore>()
        where TStore : class, IDocumentStore
    {
        var store = StoreRef.For<TStore>();
        _ = store.Resolve(_services);
        _current = store;
    }
}
