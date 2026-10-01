// -----------------------------------------------------------------------
// <copyright file="NoDaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host with no async daemon, so <c>CatchUp</c> is proven without one and <c>None</c> reliably leaves the async
/// read model stale.
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class NoDaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_nodaemon";

    /// <inheritdoc/>
    protected override bool RunDaemon => false;
}
