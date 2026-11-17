// -----------------------------------------------------------------------
// <copyright file="DaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Daemon;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.Extensions.DependencyInjection;
using Wolverine.Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host running <c>AddAsyncDaemon(Solo)</c>, for <c>WaitForNonStale</c> and the daemon path of <c>CatchUp</c>. It also
/// adds <see cref="ILedgerStore"/> as an ancillary store integrated with Wolverine and running its own daemon, whose
/// handlers are routed to it with <c>[MartenStore]</c> (ADR-0020).
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class DaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_daemon";

    /// <inheritdoc/>
    protected override bool RunDaemon => true;

    /// <inheritdoc/>
    protected override IReadOnlyList<Type> HandlerTypes => [typeof(WidgetHandlers), typeof(LedgerStoreHandlers)];

    /// <inheritdoc/>
    protected override void ConfigureServices(IServiceCollection services) =>
        services.AddMartenStore<ILedgerStore>(o =>
            {
                LedgerStores.Configure(o, ConnectionString, SchemaName + "_ledger");
                o.Projections.Add<LedgerBalanceProjection>(ProjectionLifecycle.Async);
            })
            .IntegrateWithWolverine(i => i.SchemaName = SchemaName + "_ledger_wolverine")
            .AddAsyncDaemon(DaemonMode.Solo);
}
