// -----------------------------------------------------------------------
// <copyright file="ISnapshottingStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// An action whose appended-event assertions need stream versions taken just before it runs: <c>When</c> and
/// <c>WhenHttp</c>.
/// </summary>
internal interface ISnapshottingStep : IActionStep
{
    /// <summary>
    /// Gets the stream ids to snapshot before running, filled in by the runner from the assertions that follow.
    /// </summary>
    /// <value>The stream ids.</value>
    HashSet<StreamAddress> SnapshotIds { get; }
}
