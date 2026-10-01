// -----------------------------------------------------------------------
// <copyright file="DaemonHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host running <c>AddAsyncDaemon(Solo)</c>, for <c>WaitForNonStale</c> and the daemon path of <c>CatchUp</c>.
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class DaemonHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_daemon";

    /// <inheritdoc/>
    protected override bool RunDaemon => true;
}
