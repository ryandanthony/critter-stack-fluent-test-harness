// -----------------------------------------------------------------------
// <copyright file="ProjectionSync.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// How the harness brings async projections up to date before each <c>When</c> and <c>WhenQuery</c>.
/// </summary>
public enum ProjectionSync
{
    /// <summary>
    /// Forces every async projection to catch up. Works whether or not the host runs an async daemon: with a daemon
    /// it uses <c>ForceAllMartenDaemonActivityToCatchUpAsync</c>, without one it builds a temporary daemon and calls
    /// <c>CatchUpAsync</c>. This is the default.
    /// </summary>
    CatchUp = 0,

    /// <summary>
    /// Waits for the host's running daemon with <c>WaitForNonStaleProjectionDataAsync</c>. Only valid on a host that
    /// runs <c>AddAsyncDaemon</c>; without one it always times out.
    /// </summary>
    WaitForNonStale = 1,

    /// <summary>
    /// Does nothing. For services whose projections are all inline.
    /// </summary>
    None = 2,
}
