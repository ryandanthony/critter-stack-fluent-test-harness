// -----------------------------------------------------------------------
// <copyright file="WidgetEndpoints.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Wolverine.Http;

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Wolverine.Http endpoints over the widget domain, for the <c>WhenHttp</c> tests.
/// </summary>
public static class WidgetEndpoints
{
    /// <summary>Appends <see cref="WidgetRenamed"/> and answers 204.</summary>
    /// <param name="command">The command, from the JSON body.</param>
    /// <param name="session">The session.</param>
    [WolverinePost("/widgets/rename")]
    [EmptyResponse]
    public static void Rename(RenameWidget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new WidgetRenamed(command.Id, command.Name));
    }

    /// <summary>Appends <see cref="WidgetPublished"/>, cascades a notice and answers with the widget id.</summary>
    /// <param name="command">The command, from the JSON body.</param>
    /// <param name="session">The session.</param>
    /// <returns>The response body and the cascaded notice.</returns>
    [WolverinePost("/widgets/publish")]
    public static (WidgetReceipt receipt, WidgetPublishedNotice notice) Publish(PublishWidget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new WidgetPublished(command.Id));
        return (new WidgetReceipt(command.Id, "published"), new WidgetPublishedNotice(command.Id));
    }

    /// <summary>Always throws <see cref="WidgetException"/>.</summary>
    /// <param name="command">The command, from the JSON body.</param>
    [WolverinePost("/widgets/fail")]
    [EmptyResponse]
    public static void Fail(FailWidget command) =>
        throw new WidgetException($"widget {command?.Id} failed over HTTP");

    /// <summary>Appends the session's tenant id, to prove which tenant the endpoint ran as.</summary>
    /// <param name="command">The command, from the JSON body.</param>
    /// <param name="session">The session.</param>
    [WolverinePost("/widgets/tenant")]
    [EmptyResponse]
    public static void RecordTenant(RecordTenant command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new TenantRecorded(command.Id, session.TenantId));
    }

    /// <summary>Reads the async <see cref="WidgetSummary"/> read model.</summary>
    /// <param name="id">The widget id, from the route.</param>
    /// <param name="session">The session.</param>
    /// <returns>The summary, or null (404) when the projection hasn't seen the widget.</returns>
    [WolverineGet("/widgets/{id}")]
    public static Task<WidgetSummary?> Get(Guid id, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(session);
        return session.LoadAsync<WidgetSummary>(id);
    }
}
