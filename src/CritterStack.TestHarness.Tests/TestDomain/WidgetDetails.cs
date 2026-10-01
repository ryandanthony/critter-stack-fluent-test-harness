// -----------------------------------------------------------------------
// <copyright file="WidgetDetails.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Inline read model: always current after <c>SaveChangesAsync</c>.
/// </summary>
public sealed class WidgetDetails
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The widget id.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    /// <value>The widget name.</value>
    public string Name { get; set; } = string.Empty;
}
