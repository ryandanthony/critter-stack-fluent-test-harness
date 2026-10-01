// -----------------------------------------------------------------------
// <copyright file="WidgetSummary.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Async read model: only current after projection sync.
/// </summary>
public sealed class WidgetSummary
{
    /// <summary>Gets or sets the id.</summary>
    /// <value>The widget id.</value>
    public Guid Id { get; set; }

    /// <summary>Gets or sets the name.</summary>
    /// <value>The widget name.</value>
    public string Name { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the widget is published.</summary>
    /// <value>True once published.</value>
    public bool Published { get; set; }
}
