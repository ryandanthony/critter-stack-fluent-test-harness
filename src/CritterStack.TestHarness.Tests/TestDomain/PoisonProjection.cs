// -----------------------------------------------------------------------
// <copyright file="PoisonProjection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten.Events.Aggregation;

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// An async projection whose Apply throws for <see cref="WidgetPoisoned"/>, so projection-error handling can be
/// tested. Only registered on hosts that opt in.
/// </summary>
public sealed class PoisonProjection : SingleStreamProjection<PoisonView, Guid>
{
    /// <summary>Creates the view.</summary>
    /// <param name="e">The event.</param>
    /// <returns>The view.</returns>
    public static PoisonView Create(WidgetCreated e)
    {
        ArgumentNullException.ThrowIfNull(e);
        return new() { Id = e.Id };
    }

    /// <summary>Always throws.</summary>
    /// <param name="e">The event.</param>
    /// <param name="view">The view.</param>
    public static void Apply(WidgetPoisoned e, PoisonView view) =>
        throw new InvalidOperationException($"poisoned widget {e?.Id}");
}
