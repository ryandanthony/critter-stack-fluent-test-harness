// -----------------------------------------------------------------------
// <copyright file="PoisonView.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Read model of <see cref="PoisonProjection"/>.
/// </summary>
public sealed class PoisonView
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The widget id.</value>
    public Guid Id { get; set; }
}
