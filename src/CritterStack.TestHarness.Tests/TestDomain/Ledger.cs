// -----------------------------------------------------------------------
// <copyright file="Ledger.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;
using Marten.Events.Aggregation;

#pragma warning disable MA0048 // Small related types are kept together in the test domain.
#pragma warning disable SA1402 // Small related types are kept together in the test domain.
#pragma warning disable SA1649 // Small related types are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>An ancillary Marten store for the Ledger domain, registered with <c>AddMartenStore&lt;ILedgerStore&gt;()</c>.</summary>
public interface ILedgerStore : IDocumentStore;

/// <summary>
/// An ancillary store on the poison host, with its own daemon, whose only projection always fails (ADR-0020). Only
/// one host may run a failing projection in a test run (ADR-0003), so no other host registers one.
/// </summary>
public interface IFaultyDaemonStore : IDocumentStore;

/// <summary>A store type no test host registers.</summary>
public interface IUnregisteredStore : IDocumentStore;

/// <summary>A ledger was opened.</summary>
/// <param name="Id">The ledger id.</param>
/// <param name="Owner">The owner.</param>
public sealed record LedgerOpened(Guid Id, string Owner);

/// <summary>An entry was posted to a ledger.</summary>
/// <param name="Id">The ledger id.</param>
/// <param name="Amount">The amount.</param>
public sealed record EntryPosted(Guid Id, decimal Amount);

/// <summary>Breaks the faulty projection on the poison host.</summary>
/// <param name="Id">The ledger id.</param>
public sealed record LedgerFaulted(Guid Id);

/// <summary>Opens a ledger in the ledger store.</summary>
/// <param name="Id">The ledger id.</param>
/// <param name="Owner">The owner.</param>
public sealed record OpenLedger(Guid Id, string Owner);

/// <summary>Posts an entry to a ledger in the ledger store.</summary>
/// <param name="Id">The ledger id.</param>
/// <param name="Amount">The amount.</param>
public sealed record PostEntry(Guid Id, decimal Amount);

/// <summary>Returns the ledger's async balance, or null.</summary>
/// <param name="Id">The ledger id.</param>
public sealed record FindLedger(Guid Id);

/// <summary>The Ledger aggregate. Only used as the stream's aggregate type.</summary>
public sealed class Ledger
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The ledger id.</value>
    public Guid Id { get; set; }
}

/// <summary>Async read model of a ledger in the ledger store.</summary>
public sealed class LedgerBalance
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The ledger id.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the owner.</summary>
    /// <value>The owner.</value>
    public string Owner { get; set; } = string.Empty;

    /// <summary>Gets or sets the balance.</summary>
    /// <value>The sum of posted entries.</value>
    public decimal Balance { get; set; }
}

/// <summary>Async single-stream projection of a ledger's balance.</summary>
public sealed class LedgerBalanceProjection : SingleStreamProjection<LedgerBalance, Guid>
{
    /// <summary>Creates the balance.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The balance.</returns>
    public static LedgerBalance Create(LedgerOpened e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id, Owner = e.Owner };
    }

    /// <summary>Applies an entry.</summary>
    /// <param name="e">The event.</param>
    /// <param name="balance">The balance.</param>
    public static void Apply(EntryPosted e, LedgerBalance balance)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(balance);
        balance.Balance += e.Amount;
    }
}

/// <summary>Read model of the faulty projection.</summary>
public sealed class FaultyDaemonView
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The ledger id.</value>
    public Guid Id { get; set; }
}

/// <summary>Async projection in the faulty store on the poison host; throws on <see cref="LedgerFaulted"/>.</summary>
public sealed class FaultyDaemonProjection : SingleStreamProjection<FaultyDaemonView, Guid>
{
    /// <summary>Creates the view.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The view.</returns>
    public static FaultyDaemonView Create(LedgerOpened e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id };
    }

    /// <summary>Always throws.</summary>
    /// <param name="e">The event.</param>
    /// <param name="view">The view.</param>
    public static void Apply(LedgerFaulted e, FaultyDaemonView view) =>
        throw new InvalidOperationException($"faulted ledger {e?.Id}");
}
