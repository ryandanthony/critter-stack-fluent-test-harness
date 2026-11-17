// -----------------------------------------------------------------------
// <copyright file="PoisonDaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host with an async daemon and a projection that fails on <see cref="WidgetPoisoned"/>, with apply errors
/// not skipped. Kept apart from <see cref="DaemonHostFixture"/> so a stalled shard can't affect other tests. It also
/// adds <see cref="IFaultyDaemonStore"/>, an ancillary store with its own daemon and a projection that fails on
/// <see cref="LedgerFaulted"/> (ADR-0020).
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class PoisonDaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_poison";

    /// <inheritdoc/>
    protected override bool RunDaemon => true;

    /// <inheritdoc/>
    protected override void ConfigureMarten(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Projections.Add<PoisonProjection>(ProjectionLifecycle.Async);
        options.Projections.Errors.SkipApplyErrors = false;
    }

    /// <inheritdoc/>
    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddMartenStore<IFaultyDaemonStore>(o =>
            {
                LedgerStores.Configure(o, ConnectionString, SchemaName + "_faulty");
                o.Projections.Add<FaultyDaemonProjection>(ProjectionLifecycle.Async);
                o.Projections.Errors.SkipApplyErrors = false;
            })
            .AddAsyncDaemon(DaemonMode.Solo);
}
