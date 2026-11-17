// -----------------------------------------------------------------------
// <copyright file="AncillaryStoreTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Scenarios that use ancillary Marten stores (PDR-0014) on the host with no daemon, where the ledger store is a plain
/// <c>AddMartenStore</c>: no coordinator, so projection sync builds a temporary daemon from that store (ADR-0020).
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class AncillaryStoreTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "One scenario covers a flow across the main and an ancillary store: Givens and Thens name their store, and the choice carries across the When.")]
    public async Task One_scenario_uses_the_main_and_an_ancillary_store()
    {
        var widgetId = Guid.NewGuid();
        var ledgerId = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(widgetId, i => [new WidgetCreated(i, "Sprocket")])
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(ledgerId, i => [new LedgerOpened(i, "acme")])
            .When(() => new PostEntry(ledgerId, 10m))
            .ThenAppended<EntryPosted>(ledgerId, e => e.Amount == 10m)
            .ThenAppendedCount(ledgerId, 1)
            .ThenStreamEventCount(ledgerId, 2)
            .InStore<IDocumentStore>()
            .ThenStreamEventCount(widgetId, 1)
            .ThenStreamEventCount(ledgerId, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Givens land in the store the scenario names, not the main store.")]
    public async Task Givens_land_in_the_named_store()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenStreamEventCount(id, 1)
            .RunAsync(Ct);

        await using var main = fixture.Host.Services.GetRequiredService<IDocumentStore>().QuerySession();
        Assert.Empty(await main.Events.FetchStreamAsync(id, token: Ct));
        await using var ledger = fixture.Host.Services.GetRequiredService<ILedgerStore>().QuerySession();
        Assert.Single(await ledger.Events.FetchStreamAsync(id, token: Ct));
    }

    [Fact]
    [Trait("business_value", "An async read model in an ancillary store is current before a query, so tests of such stores don't read stale data.")]
    public async Task Async_read_model_in_an_ancillary_store_is_current_before_a_query()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme"), new EntryPosted(i, 5m)])
            .When(() => new PostEntry(id, 10m))
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is { Balance: 15m, Owner: "acme" })
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "UseStore syncs a store no step names, for a store that only a handler writes and only a query reads.")]
    public async Task UseStore_syncs_a_store_no_step_names()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.UseStore<ILedgerStore>())
            .When(() => new OpenLedger(id, "acme"))
            .When(() => new PostEntry(id, 7m))
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is { Balance: 7m })
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Without InStore or UseStore an ancillary read model isn't synced, which is why the options exist.")]
    public async Task Ancillary_store_is_not_synced_unless_the_scenario_uses_it()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .When(() => new OpenLedger(id, "acme"))
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is null)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "The same stream id in two stores is two streams: started-stream tracking and appended snapshots are per store.")]
    public async Task Same_id_in_two_stores_is_two_streams()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .GivenStream(id, i => [new EntryPosted(i, 1m)])
            .When(() => new PostEntry(id, 2m))
            .ThenAppendedCount(id, 1)
            .ThenStreamEventCount(id, 3)
            .InStore<IDocumentStore>()
            .ThenAppendedCount(id, 0)
            .ThenStreamEventCount(id, 1)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "ForTenant applies to ancillary stores too: a ledger written for one tenant is invisible to another.")]
    public async Task ForTenant_isolates_ancillary_streams()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("acme")
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenStreamEventCount(id, 1)
            .RunAsync(Ct);

        await TestHarness.Setup(fixture.Host)
            .ForTenant("globex")
            .InStore<ILedgerStore>()
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenStreamEventCount(id, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A failure on an ancillary stream names the store and shows that store's events.")]
    public async Task Failure_names_the_store_and_shows_its_events()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .When(() => new PostEntry(id, 10m))
            .ThenAppended<EntryPosted>(id, e => e.Amount == 99m));

        Assert.StartsWith($"step 3 ThenAppended<EntryPosted>({id}) in ILedgerStore: no EntryPosted matching the predicate appended by step 2 When(PostEntry)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v2 EntryPosted {", failure.Message, StringComparison.Ordinal);
        Assert.Contains("\"Amount\":10", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A WhenQuery or When followed by InStore keeps the stage, so Thens after a query can switch store too.")]
    public async Task InStore_works_on_query_and_http_stages()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host)
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .InStore<IDocumentStore>()
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamEventCount(id, 0)
            .InStore<ILedgerStore>()
            .ThenStreamHasEvent<LedgerOpened>(id)
            .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Gear")).ToUrl("/widgets/rename"))
            .ThenStatusCode(204)
            .ThenAppendedCount(id, 0)
            .InStore<IDocumentStore>()
            .ThenAppendedCount(id, 1)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A store the host doesn't register fails at the InStore or Setup line, saying how to register it.")]
    public void Unregistered_store_fails_where_it_is_named()
    {
        var atInStore = Assert.Throws<InvalidOperationException>(() => TestHarness.Setup(fixture.Host).InStore<IUnregisteredStore>());
        Assert.Equal("IUnregisteredStore is not registered in the host. Register it with AddMartenStore<IUnregisteredStore>() before using it in a scenario.", atInStore.Message);

        var atThen = Assert.Throws<InvalidOperationException>(() => TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(Guid.NewGuid(), "Gear"))
            .InStore<IUnregisteredStore>());
        Assert.StartsWith("IUnregisteredStore is not registered", atThen.Message, StringComparison.Ordinal);

        var atSetup = Assert.Throws<InvalidOperationException>(() => TestHarness.Setup(fixture.Host, o => o.UseStore<IUnregisteredStore>()));
        Assert.StartsWith("IUnregisteredStore is not registered", atSetup.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "UseStore of the main store, or of a store already named, is harmless.")]
    public async Task UseStore_of_the_main_or_a_named_store_is_harmless()
    {
        var id = Guid.NewGuid();

        await TestHarness.Setup(fixture.Host, o => o.UseStore<IDocumentStore>().UseStore<ILedgerStore>().UseStore<ILedgerStore>())
            .InStore<ILedgerStore>()
            .GivenStream<Ledger>(id, i => [new LedgerOpened(i, "acme")])
            .WhenQuery<LedgerBalance?>(() => new FindLedger(id))
            .ThenResult(b => b is not null)
            .RunAsync(Ct);
    }
}
