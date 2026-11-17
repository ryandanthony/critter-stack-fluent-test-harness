// -----------------------------------------------------------------------
// <copyright file="NoDaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Projections;
using Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host with no async daemon, so <c>CatchUp</c> is proven without one and <c>None</c> reliably leaves the async
/// read model stale. Besides the Widget domain it hosts the Parcel domain, whose aggregate, events and read model use
/// a strongly typed id that wraps a Guid.
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class NoDaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_nodaemon";

    /// <inheritdoc/>
    protected override bool RunDaemon => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<Type> HandlerTypes => [typeof(WidgetHandlers), typeof(ParcelHandlers)];

    /// <inheritdoc/>
    protected override void ConfigureStreams(StoreOptions options)
    {
        base.ConfigureStreams(options);
        options.Projections.Add<ParcelSummaryProjection>(ProjectionLifecycle.Async);
    }
}
