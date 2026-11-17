// -----------------------------------------------------------------------
// <copyright file="NoDaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host with no async daemon, so <c>CatchUp</c> is proven without one and <c>None</c> reliably leaves the async
/// read model stale. Besides the Widget domain it hosts the Parcel domain, whose aggregate, events and read model use
/// a strongly typed id that wraps a Guid. It also adds <see cref="ILedgerStore"/> as a plain ancillary store (no
/// Wolverine integration, so no daemon coordinator and the temporary-daemon sync path).
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class NoDaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_nodaemon";

    /// <inheritdoc/>
    protected override bool RunDaemon => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<Type> HandlerTypes => [typeof(WidgetHandlers), typeof(ParcelHandlers), typeof(LedgerDirectHandlers)];

    /// <inheritdoc/>
    protected override void ConfigureStreams(StoreOptions options)
    {
        base.ConfigureStreams(options);
        options.Projections.Add<ParcelSummaryProjection>(ProjectionLifecycle.Async);
    }

    /// <inheritdoc/>
    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddMartenStore<ILedgerStore>(o =>
        {
            LedgerStores.Configure(o, ConnectionString, SchemaName + "_ledger");
            o.Projections.Add<LedgerBalanceProjection>(ProjectionLifecycle.Async);
        });
}
