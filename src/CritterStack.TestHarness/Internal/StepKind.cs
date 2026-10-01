// -----------------------------------------------------------------------
// <copyright file="StepKind.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// What a step is, so the runner can apply the unclaimed-exception rule.
/// </summary>
internal enum StepKind
{
    /// <summary>A Given.</summary>
    Given = 0,

    /// <summary>A <c>When</c> or <c>WhenQuery</c>.</summary>
    Action = 1,

    /// <summary>A <c>ThenThrows</c>, the only step allowed to run while an exception is unclaimed.</summary>
    ThenThrows = 2,

    /// <summary>Any other Then.</summary>
    Then = 3,
}
