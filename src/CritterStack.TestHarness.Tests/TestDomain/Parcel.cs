// -----------------------------------------------------------------------
// <copyright file="Parcel.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Marten.Events.Aggregation;
using Wolverine.Marten;

#pragma warning disable MA0048 // Small related types are kept together in the test domain.
#pragma warning disable SA1402 // Small related types are kept together in the test domain.
#pragma warning disable SA1649 // Small related types are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>A strongly typed parcel id that wraps a Guid stream id.</summary>
/// <param name="Value">The stream id.</param>
public readonly record struct ParcelId(Guid Value);

/// <summary>A parcel was created.</summary>
/// <param name="Id">The parcel id.</param>
/// <param name="Label">The parcel label.</param>
public sealed record ParcelCreated(ParcelId Id, string Label);

/// <summary>A parcel was shipped.</summary>
/// <param name="Id">The parcel id.</param>
public sealed record ParcelShipped(ParcelId Id);

/// <summary>Ships a parcel through Wolverine's aggregate handler workflow.</summary>
/// <param name="ParcelId">The parcel id.</param>
public sealed record ShipParcel(ParcelId ParcelId);

/// <summary>Returns the parcel's async summary, or null.</summary>
/// <param name="Id">The parcel id.</param>
public sealed record FindParcel(ParcelId Id);

/// <summary>
/// Live-aggregated parcel, identified by a strongly typed id.
/// </summary>
public sealed class Parcel
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The parcel id.</value>
    public ParcelId Id { get; set; }

    /// <summary>Gets or sets a value indicating whether the parcel has shipped.</summary>
    /// <value>True once shipped.</value>
    public bool Shipped { get; set; }

    /// <summary>Creates the parcel.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The parcel.</returns>
    public static Parcel Create(ParcelCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id };
    }

    /// <summary>Applies a shipment.</summary>
    /// <param name="e">The event.</param>
    public void Apply(ParcelShipped e)
    {
        ArgumentNullException.ThrowIfNull(e);
        Shipped = true;
    }
}

/// <summary>
/// Async read model of a parcel, keyed by its strongly typed id.
/// </summary>
public sealed class ParcelSummary
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The parcel id.</value>
    public ParcelId Id { get; set; }

    /// <summary>Gets or sets the label.</summary>
    /// <value>The parcel label.</value>
    public string Label { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the parcel has shipped.</summary>
    /// <value>True once shipped.</value>
    public bool Shipped { get; set; }
}

/// <summary>
/// Async single-stream projection keyed by the strongly typed id.
/// </summary>
public sealed class ParcelSummaryProjection : SingleStreamProjection<ParcelSummary, ParcelId>
{
    /// <summary>Creates the summary.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The summary.</returns>
    public static ParcelSummary Create(ParcelCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id, Label = e.Label };
    }

    /// <summary>Applies a shipment.</summary>
    /// <param name="e">The event.</param>
    /// <param name="summary">The summary.</param>
    public static void Apply(ParcelShipped e, ParcelSummary summary)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(summary);
        summary.Shipped = true;
    }
}

/// <summary>
/// Wolverine handlers for the Parcel domain.
/// </summary>
public static class ParcelHandlers
{
    /// <summary>Ships the parcel, appending <see cref="ParcelShipped"/> through the aggregate handler workflow.</summary>
    /// <param name="command">The command.</param>
    /// <param name="parcel">The parcel, loaded by Wolverine from the typed id.</param>
    /// <returns>The event to append.</returns>
    [AggregateHandler]
    public static ParcelShipped Handle(ShipParcel command, Parcel parcel)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(parcel);
        return new ParcelShipped(parcel.Id);
    }

    /// <summary>Returns the async summary or null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="session">The session.</param>
    /// <returns>The summary, or null.</returns>
    public static Task<ParcelSummary?> Handle(FindParcel query, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(session);
        return session.LoadAsync<ParcelSummary>(query.Id);
    }
}
