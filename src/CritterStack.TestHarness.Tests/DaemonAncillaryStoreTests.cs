// -----------------------------------------------------------------------
// <copyright file="DaemonAncillaryStoreTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Ancillary stores on the daemon host, where <see cref="ILedgerStore"/> is integrated with Wolverine, runs its own
/// daemon and has <c>[MartenStore]</c> handlers: projection sync forces that store's daemon (ADR-0020).
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(DaemonCollection))]
public sealed class DaemonAncillaryStoreTests(DaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "A [MartenStore] handler's events and its store's async read model are checked in one scenario, with the store's own daemon caught up.")]
    public async Task MartenStore_handler_and_async_read_model_on_a_daemon_store()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .When(() => new PostEntry(id, 10m))
            .ThenAppended<EntryPosted>(id, e => e.Amount == 10m)
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is { Balance: 10m })
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "WaitForNonStale works per store on a daemon host.")]
    public async Task WaitForNonStale_covers_the_ancillary_store()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.ProjectionSync = ProjectionSync.WaitForNonStale)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme"), new EntryPosted(i, 3m)])
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is { Balance: 3m })
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A tenant routed through a [MartenStore] handler lands in the ancillary store for that tenant only.")]
    public async Task ForTenant_routes_MartenStore_handlers_to_the_tenant()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.UseStore<ILedgerStore>())
            .ForTenant("acme")
            .When(() => new OpenLedger(id, "acme"))
            .InStore<ILedgerStore>()
            .ThenAppended<LedgerOpened>(id)
            .InStore<IDocumentStore>()
            .ThenStreamEventCount(id, 0)
            .RunAsync(Ct);

        await TestHarness.Setup(fixture.Host)
            .ForTenant("globex")
            .InStore<ILedgerStore>()
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is null)
            .ThenStreamEventCount(id, 0)
            .RunAsync(Ct);
    }
}
