// -----------------------------------------------------------------------
// <copyright file="WidgetDetailsProjection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten.Events.Aggregation;

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Builds <see cref="WidgetDetails"/>. Registered inline.
/// </summary>
public sealed class WidgetDetailsProjection : SingleStreamProjection<WidgetDetails, Guid>
{
    /// <summary>Creates the details.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The details.</returns>
    public static WidgetDetails Create(WidgetCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id, Name = e.Name };
    }

    /// <summary>Applies a rename.</summary>
    /// <param name="e">The event.</param>
    /// <param name="details">The details.</param>
    public static void Apply(WidgetRenamed e, WidgetDetails details)
    {
        ArgumentNullException.ThrowIfNull(e);
        ArgumentNullException.ThrowIfNull(details);
        details.Name = e.Name;
    }
}
