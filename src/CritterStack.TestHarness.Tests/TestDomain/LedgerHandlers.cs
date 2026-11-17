// -----------------------------------------------------------------------
// <copyright file="LedgerHandlers.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Wolverine.Marten;

#pragma warning disable MA0048 // Small related types are kept together in the test domain.
#pragma warning disable SA1402 // Small related types are kept together in the test domain.
#pragma warning disable SA1649 // Small related types are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Ledger handlers routed to the ancillary store by Wolverine (<c>[MartenStore]</c>), on the host where the ledger
/// store is integrated with Wolverine.
/// </summary>
public static class LedgerStoreHandlers
{
    /// <summary>Opens the ledger.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The ledger store's session.</param>
    [MartenStore(typeof(ILedgerStore))]
    public static void Handle(OpenLedger command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.StartStream<Ledger>(command.Id, new LedgerOpened(command.Id, command.Owner));
    }

    /// <summary>Appends <see cref="EntryPosted"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="session">The ledger store's session.</param>
    [MartenStore(typeof(ILedgerStore))]
    public static void Handle(PostEntry command, IDocumentSession session)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(session);
        session.Events.Append(command.Id, new EntryPosted(command.Id, command.Amount));
    }

    /// <summary>Returns the async balance or null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="session">The ledger store's session.</param>
    /// <returns>The balance, or null.</returns>
    [MartenStore(typeof(ILedgerStore))]
    public static Task<LedgerBalance?> Handle(FindLedger query, IQuerySession session)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(session);
        return session.LoadAsync<LedgerBalance>(query.Id);
    }
}

/// <summary>
/// Ledger handlers that use the ancillary store directly, on the host where it is a plain <c>AddMartenStore</c> (no
/// Wolverine integration, so no daemon coordinator and the temporary-daemon sync path).
/// </summary>
public static class LedgerDirectHandlers
{
    /// <summary>Opens the ledger.</summary>
    /// <param name="command">The command.</param>
    /// <param name="store">The ledger store.</param>
    /// <returns>A task.</returns>
    public static async Task Handle(OpenLedger command, ILedgerStore store)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(store);
        await using var session = store.LightweightSession();
        session.Events.StartStream<Ledger>(command.Id, new LedgerOpened(command.Id, command.Owner));
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>Appends <see cref="EntryPosted"/>.</summary>
    /// <param name="command">The command.</param>
    /// <param name="store">The ledger store.</param>
    /// <returns>A task.</returns>
    public static async Task Handle(PostEntry command, ILedgerStore store)
    {
        ArgumentNullException.ThrowIfNull(command);
        ArgumentNullException.ThrowIfNull(store);
        await using var session = store.LightweightSession();
        session.Events.Append(command.Id, new EntryPosted(command.Id, command.Amount));
        await session.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <summary>Returns the async balance or null.</summary>
    /// <param name="query">The query.</param>
    /// <param name="store">The ledger store.</param>
    /// <returns>The balance, or null.</returns>
    public static async Task<LedgerBalance?> Handle(FindLedger query, ILedgerStore store)
    {
        ArgumentNullException.ThrowIfNull(query);
        ArgumentNullException.ThrowIfNull(store);
        await using var session = store.QuerySession();
        return await session.LoadAsync<LedgerBalance>(query.Id).ConfigureAwait(false);
    }
}
