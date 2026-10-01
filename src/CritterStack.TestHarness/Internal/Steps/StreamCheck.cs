// -----------------------------------------------------------------------
// <copyright file="StreamCheck.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// What a stream assertion checks.
/// </summary>
internal enum StreamCheck
{
    /// <summary>At least one matching event.</summary>
    Has = 0,

    /// <summary>No matching event.</summary>
    HasNo = 1,

    /// <summary>An exact event count.</summary>
    Count = 2,
}
