// -----------------------------------------------------------------------
// <copyright file="WidgetSummaryProjection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten.Events.Aggregation;

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Builds <see cref="WidgetSummary"/>. Registered as async.
/// </summary>
public sealed class WidgetSummaryProjection : SingleStreamProjection<WidgetSummary, Guid>
{
    /// <summary>Creates the summary.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The summary.</returns>
    public static WidgetSummary Create(WidgetCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id, Name = e.Name };
    }

    /// <summary>Applies a rename.</summary>
    /// <param name="e">The event.</param>
    /// <param name="summary">The summary.</param>
    public static void Apply(WidgetRenamed e, WidgetSummary summary)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(summary);
        summary.Name = e.Name;
    }

    /// <summary>Applies a publish.</summary>
    /// <param name="e">The event.</param>
    /// <param name="summary">The summary.</param>
    public static void Apply(WidgetPublished e, WidgetSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        summary.Published = true;
    }
}
