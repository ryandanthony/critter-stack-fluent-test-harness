// -----------------------------------------------------------------------
// <copyright file="PoisonDaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Projections;
using Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host with an async daemon and a projection that fails on <see cref="WidgetPoisoned"/>, with apply errors
/// not skipped. Kept apart from <see cref="DaemonHostFixture"/> so a stalled shard can't affect other tests.
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
}
