// -----------------------------------------------------------------------
// <copyright file="GadgetHandlers.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Marten.Events.Aggregation;
using Wolverine.Http;

#pragma warning disable MA0048 // Small related types are kept together in the test domain.
#pragma warning disable SA1402 // Small related types are kept together in the test domain.
#pragma warning disable SA1649 // Small related types are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Wolverine handlers for the string-keyed Gadget domain.
/// </summary>
public static class GadgetHandlers
{
    /// <summary>Appends <see cref="GadgetRenamed"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The session.</param>
    public static void Handle(RenameGadget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Key, new GadgetRenamed(command.Key, command.Name));
    }

    /// <summary>Always throws.</summary>
    /// <param name="command">The command.</param>
    public static void Handle(FailGadget command) =>
        throw new WidgetException($"gadget {command?.Key} failed");

    /// <summary>Returns the async summary or null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="session">The session.</param>
    /// <returns>The summary, or null.</returns>
    public static Task<GadgetSummary?> Handle(FindGadget query, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(session);
        return session.LoadAsync<GadgetSummary>(query.Key);
    }
}

/// <summary>
/// Wolverine.Http endpoints for the Gadget domain.
/// </summary>
public static class GadgetEndpoints
{
    /// <summary>Appends <see cref="GadgetRenamed"/> and answers 204.</summary>
    /// <param name="command">The command, from the JSON body.</param>
    /// <param name="session">The session.</param>
    [WolverinePost("/gadgets/rename")]
    [EmptyResponse]
    public static void Rename(RenameGadget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Key, new GadgetRenamed(command.Key, command.Name));
    }
}

/// <summary>
/// Async single-stream projection keyed by the string stream key.
/// </summary>
public sealed class GadgetSummaryProjection : SingleStreamProjection<GadgetSummary, string>
{
    /// <summary>Creates the summary.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The summary.</returns>
    public static GadgetSummary Create(GadgetCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Key, Name = e.Name };
    }

    /// <summary>Applies a rename.</summary>
    /// <param name="e">The event.</param>
    /// <param name="summary">The summary.</param>
    public static void Apply(GadgetRenamed e, GadgetSummary summary)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(summary);
        summary.Name = e.Name;
    }
}
