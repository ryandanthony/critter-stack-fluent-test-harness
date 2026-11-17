// -----------------------------------------------------------------------
// <copyright file="AncillaryStoreHowToSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs docs/how-to/use-an-ancillary-store.md, on the no-daemon host, whose ancillary <see cref="ILedgerStore"/> is a
/// plain <c>AddMartenStore</c> next to the main store.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class AncillaryStoreHowToSamples(NoDaemonHostFixture fixture)
{
    // Sample: use-an-ancillary-store.md, "Name the store for stream steps".
    [Fact]
    [Trait("business_value", "The ancillary-store guide's InStore sample works as documented.")]
    public async Task UseAnAncillaryStore_in_store()
    {
        var ledgerId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(ledgerId, i => [new LedgerOpened(i, "acme")])
            .When(() => new PostEntry(ledgerId, 10m))
            .ThenAppended<EntryPosted>(ledgerId, e => e.Amount == 10m)
            .WhenQuery<LedgerBalance?>(() => new FindLedger(ledgerId))
            .ThenResult(b => b is { Balance: 10m });
    }

    // Sample: use-an-ancillary-store.md, "Use both stores in one scenario".
    [Fact]
    [Trait("business_value", "The ancillary-store guide's two-store sample works as documented.")]
    public async Task UseAnAncillaryStore_two_stores()
    {
        var widgetId = Guid.NewGuid();
        var ledgerId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, i => [new WidgetCreated(i, "Sprocket")])
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(ledgerId, i => [new LedgerOpened(i, "acme")])
            .When(() => new PostEntry(ledgerId, 10m))
            .ThenAppendedCount(ledgerId, 1)
            .InStore<IDocumentStore>()
            .ThenStreamEventCount(widgetId, 1);
    }

    // Sample: use-an-ancillary-store.md, "Sync a store no step names".
    [Fact]
    [Trait("business_value", "The ancillary-store guide's UseStore sample works as documented.")]
    public async Task UseAnAncillaryStore_use_store()
    {
        var ledgerId = Guid.NewGuid();

        await TestHarness
            .Setup(fixture.Host, o => o.UseStore<ILedgerStore>())
            .When(() => new OpenLedger(ledgerId, "acme"))
            .WhenQuery<LedgerBalance?>(() => new FindLedger(ledgerId))
            .ThenResult(b => b is { Owner: "acme" });
    }
}
