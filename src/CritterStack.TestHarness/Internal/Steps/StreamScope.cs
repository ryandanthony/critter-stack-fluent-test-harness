// -----------------------------------------------------------------------
// <copyright file="StreamScope.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Which events a stream assertion looks at.
/// </summary>
internal enum StreamScope
{
    /// <summary>The whole stream.</summary>
    Whole = 0,

    /// <summary>Only events the most recent <c>When</c> appended.</summary>
    Appended = 1,
}
