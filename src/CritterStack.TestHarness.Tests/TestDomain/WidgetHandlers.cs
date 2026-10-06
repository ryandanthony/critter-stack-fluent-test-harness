// -----------------------------------------------------------------------
// <copyright file="WidgetHandlers.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Wolverine handlers for the test domain.
/// </summary>
public static class WidgetHandlers
{
    /// <summary>Appends <see cref="WidgetRenamed"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The session.</param>
    public static void Handle(RenameWidget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new WidgetRenamed(command.Id, command.Name));
    }

    /// <summary>Appends <see cref="WidgetPublished"/> and cascades a notice.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The session.</param>
    /// <returns>The cascaded notice.</returns>
    public static WidgetPublishedNotice Handle(PublishWidget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new WidgetPublished(command.Id));
        return new WidgetPublishedNotice(command.Id);
    }

    /// <summary>Receives the cascaded notice.</summary>
    /// <param name="notice">The notice.</param>
    public static void Handle(WidgetPublishedNotice notice) => ArgumentNullException.ThrowIfNull(notice);

    /// <summary>Appends the session's tenant id, to prove which tenant the handler ran as.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The session.</param>
    public static void Handle(RecordTenant command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new TenantRecorded(command.Id, session.TenantId));
    }

    /// <summary>Always throws.</summary>
    /// <param name="command">The command.</param>
    public static void Handle(FailWidget command) =>
        throw new WidgetException($"widget {command?.Id} failed");

    /// <summary>Cascades a notice whose handler throws.</summary>
    /// <param name="command">The command.</param>
    /// <returns>The failing notice.</returns>
    public static FailingNotice Handle(FailWidgetLater command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return new FailingNotice(command.Id);
    }

    /// <summary>Always throws.</summary>
    /// <param name="notice">The notice.</param>
    public static void Handle(FailingNotice notice) =>
        throw new WidgetException($"cascaded notice for {notice?.Id} failed");

    /// <summary>Throws a handler-owned timeout.</summary>
    /// <param name="command">The command.</param>
    public static void Handle(TimeOutWidget command) =>
        throw new TimeoutException($"widget {command?.Id} supplier timed out");

    /// <summary>Waits for the requested delay.</summary>
    /// <param name="command">The command.</param>
    /// <returns>A task.</returns>
    public static Task Handle(StallWidget command)
    {
        ArgumentNullException.ThrowIfNull(command);
        return Task.Delay(command.Delay);
    }

    /// <summary>Archives a widget, but only if its async summary exists.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The session.</param>
    /// <returns>A task.</returns>
    public static async Task Handle(ArchiveWidget command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        _ = await session.LoadAsync<WidgetSummary>(command.Id).ConfigureAwait(false)
            ?? throw new WidgetNotFoundException($"no summary for widget {command.Id}");
        session.Events.Append(command.Id, new WidgetArchived(command.Id));
    }

    /// <summary>Returns the async summary, or throws if it is missing.</summary>
    /// <param name="query">The query.</param>
    /// <param name="session">The session.</param>
    /// <returns>The summary.</returns>
    public static async Task<WidgetSummary> Handle(GetWidgetById query, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(session);
        return await session.LoadAsync<WidgetSummary>(query.Id).ConfigureAwait(false)
            ?? throw new WidgetNotFoundException($"no widget {query.Id}");
    }

    /// <summary>Returns the async summary or null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="session">The session.</param>
    /// <returns>The summary, or null.</returns>
    public static Task<WidgetSummary?> Handle(FindWidget query, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(session);
        return session.LoadAsync<WidgetSummary>(query.Id);
    }

    /// <summary>Waits for the requested delay, honouring cancellation, then returns null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="cancellationToken">Cancels the wait.</param>
    /// <returns>Always null.</returns>
    public static async Task<WidgetSummary?> Handle(SlowFindWidget query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        await Task.Delay(query.Delay, cancellationToken).ConfigureAwait(false);
        return null;
    }

    /// <summary>Throws a handler-owned timeout.</summary>
    /// <param name="query">The query.</param>
    /// <returns>Never returns.</returns>
    public static WidgetSummary Handle(TimeOutWidgetQuery query) =>
        throw new TimeoutException($"widget {query?.Id} read model timed out");

    /// <summary>Throws a handler timeout that looks like Wolverine's tracking-timeout wrapper.</summary>
    /// <param name="query">The query.</param>
    /// <returns>Never returns.</returns>
    public static WidgetSummary Handle(WrapperShapedTimeoutQuery query) =>
        throw new TimeoutException(
            $"This TrackedSession timed out for widget {query?.Id}",
            new TimeoutException("read model store timed out"));
}
