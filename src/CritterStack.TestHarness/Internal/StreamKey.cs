// -----------------------------------------------------------------------
// <copyright file="StreamKey.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Reflection;
using JasperFx.Core.Reflection;
using JasperFx.Events;
using Marten;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// A stream id as Marten sees it: a <see cref="System.Guid"/> or a <see cref="string"/>. Strongly typed ids are
/// unwrapped to one of these when the step is recorded. Equality ignores the label, so a typed id and its wrapped
/// value name the same stream.
/// </summary>
internal sealed class StreamKey : IEquatable<StreamKey>
{
    private StreamKey(Guid? guid, string? key, string label)
    {
        Guid = guid;
        Key = key;
        Label = label;
    }

    /// <summary>Gets the Guid id, or null for a string id.</summary>
    /// <value>The Guid id.</value>
    internal Guid? Guid { get; }

    /// <summary>Gets the string id, or null for a Guid id.</summary>
    /// <value>The string id.</value>
    internal string? Key { get; }

    /// <summary>Gets how the id appears in step descriptions.</summary>
    /// <value>For example <c>3f0c…</c>, <c>"order-17"</c> or <c>OrderId(3f0c…)</c>.</value>
    internal string Label { get; }

    /// <summary>Gets a value indicating whether the id is a string.</summary>
    /// <value>True for a string id.</value>
    internal bool IsString => Key is not null;

    /// <summary>
    /// Creates a key for a Guid stream id.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <returns>The key.</returns>
    internal static StreamKey For(Guid id) => new(id, null, id.ToString());

    /// <summary>
    /// Creates a key for a string stream id.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <returns>The key.</returns>
    /// <exception cref="ArgumentException">The id is null, empty or white space.</exception>
    internal static StreamKey For(string id)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(id);
        return new(null, id, Quote(id));
    }

    /// <summary>
    /// Creates a key for a strongly typed stream id that wraps a Guid or a string, using Marten's own value type
    /// rules. A <see cref="System.Guid"/> or <see cref="string"/> given as <typeparamref name="TId"/> is used as is.
    /// </summary>
    /// <typeparam name="TId">The id type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <returns>The key.</returns>
    /// <exception cref="ArgumentNullException">The id, or the string it wraps, is null.</exception>
    /// <exception cref="ArgumentException"><typeparamref name="TId"/> isn't a single-value wrapper of a Guid or a
    /// string, or reading the wrapped value threw.</exception>
    internal static StreamKey ForTyped<TId>(TId id)
    {
        ArgumentNullException.ThrowIfNull(id);

        if (id is Guid guid)
        {
            return For(guid);
        }

        if (id is string key)
        {
            return For(key);
        }

        var type = typeof(TId);
        var info = Describe(type, nameof(id));
        object? inner;
        try
        {
            inner = info.ValueProperty.GetValue(id);
        }
        catch (TargetInvocationException ex)
        {
            throw new ArgumentException(
                $"reading {Failure.TypeName(type)}.{info.ValueProperty.Name} threw {Failure.TypeName(ex.InnerException?.GetType() ?? ex.GetType())}",
                nameof(id),
                ex.InnerException ?? ex);
        }

        var name = Failure.TypeName(type);
        if (inner is Guid wrappedGuid)
        {
            return new(wrappedGuid, null, $"{name}({wrappedGuid})");
        }

        if (inner is not string wrappedKey || string.IsNullOrWhiteSpace(wrappedKey))
        {
            throw new ArgumentException($"{name} wraps an empty string, which can't be a stream id.", nameof(id));
        }

        return new(null, wrappedKey, $"{name}({Quote(wrappedKey)})");
    }

    /// <summary>
    /// Says why this key can't be used on a store, or null if it matches the store's stream identity.
    /// </summary>
    /// <param name="identity">The store's stream identity.</param>
    /// <returns>The reason, or null.</returns>
    internal string? Mismatch(StreamIdentity identity)
    {
        var storeUsesStrings = identity == StreamIdentity.AsString;
        if (storeUsesStrings == IsString)
        {
            return null;
        }

        var (mine, store) = IsString ? ("a string", "Guid") : ("a Guid", "string");
        var fix = IsString ? "a Guid, or a typed id that wraps a Guid" : "a string, or a typed id that wraps a string";
        return $"the stream id {Label} is {mine}, but the store uses {store} stream ids (StreamIdentity.{identity}). Pass {fix}.";
    }

    /// <summary>
    /// Starts the stream, as an aggregate of type <paramref name="aggregateType"/> if one is given.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="aggregateType">The aggregate type, or null.</param>
    /// <param name="events">The events.</param>
    internal void StartStream(IDocumentSession session, Type? aggregateType, object[] events)
    {
        if (Key is null)
        {
            _ = aggregateType is null
                ? session.Events.StartStream(Guid!.Value, events)
                : session.Events.StartStream(aggregateType, Guid!.Value, events);
        }
        else
        {
            _ = aggregateType is null
                ? session.Events.StartStream(Key, events)
                : session.Events.StartStream(aggregateType, Key, events);
        }
    }

    /// <summary>
    /// Appends events to the stream.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="events">The events.</param>
    internal void Append(IDocumentSession session, object[] events)
    {
        if (Key is null)
        {
            session.Events.Append(Guid!.Value, events);
        }
        else
        {
            session.Events.Append(Key, events);
        }
    }

    /// <summary>
    /// Reads the stream's events from <paramref name="fromVersion"/> on.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="fromVersion">The first version to read, or 0 for the whole stream.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The events.</returns>
    internal Task<IReadOnlyList<IEvent>> FetchAsync(IQuerySession session, long fromVersion, CancellationToken cancellationToken) =>
        Key is null
            ? session.Events.FetchStreamAsync(Guid!.Value, fromVersion: fromVersion, token: cancellationToken)
            : session.Events.FetchStreamAsync(Key, fromVersion: fromVersion, token: cancellationToken);

    /// <summary>
    /// Reads the stream's state.
    /// </summary>
    /// <param name="session">The session.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The state, or null if the stream doesn't exist.</returns>
    internal Task<StreamState?> FetchStateAsync(IQuerySession session, CancellationToken cancellationToken) =>
        Key is null
            ? session.Events.FetchStreamStateAsync(Guid!.Value, cancellationToken)
            : session.Events.FetchStreamStateAsync(Key, cancellationToken);

    /// <inheritdoc/>
    public bool Equals(StreamKey? other) =>
        other is not null && Guid == other.Guid && string.Equals(Key, other.Key, StringComparison.Ordinal);

    /// <inheritdoc/>
    public override bool Equals(object? obj) => Equals(obj as StreamKey);

    /// <inheritdoc/>
    public override int GetHashCode() =>
        Key is null ? Guid.GetHashCode() : StringComparer.Ordinal.GetHashCode(Key);

    /// <inheritdoc/>
    public override string ToString() => Label;

    private static ValueTypeInfo Describe(Type type, string paramName)
    {
        ValueTypeInfo info;
        try
        {
            info = ValueTypeInfo.ForType(type);
        }
        catch (InvalidValueTypeException ex)
        {
            throw new ArgumentException(
                $"{Failure.TypeName(type)} can't be used as a stream id: a strongly typed id needs a single public, gettable property that wraps a Guid or a string.",
                paramName,
                ex);
        }

        if (info.SimpleType != typeof(Guid) && info.SimpleType != typeof(string))
        {
            throw new ArgumentException(
                $"{Failure.TypeName(type)} wraps {Failure.TypeName(info.SimpleType)}, but Marten stream ids are a Guid or a string.",
                paramName);
        }

        return info;
    }

    private static string Quote(string id) => $"\"{id}\"";
}
