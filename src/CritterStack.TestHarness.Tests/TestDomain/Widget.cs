// -----------------------------------------------------------------------
// <copyright file="Widget.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// The test domain's aggregate. Only used as the stream's aggregate type.
/// </summary>
public sealed class Widget
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The widget id.</value>
    public Guid Id { get; set; }
}
