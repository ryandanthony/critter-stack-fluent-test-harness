// -----------------------------------------------------------------------
// <copyright file="HarnessOptions.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// Everything that varies between services. Set through the <c>configure</c> callback of
/// <see cref="TestHarness.Setup"/>.
/// </summary>
public sealed class HarnessOptions
{
    /// <summary>
    /// Gets or sets how async projections are brought up to date before each <c>When</c> and <c>WhenQuery</c>.
    /// </summary>
    /// <value>Defaults to <c>ProjectionSync.CatchUp</c>.</value>
    public ProjectionSync ProjectionSync { get; set; } = ProjectionSync.CatchUp;

    /// <summary>
    /// Gets or sets how long one projection sync may take before the scenario fails.
    /// </summary>
    /// <value>Defaults to 30 seconds.</value>
    public TimeSpan ProjectionTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Gets or sets how long Wolverine's tracked session waits for a <c>When</c> and the messages it cascades.
    /// </summary>
    /// <value>Defaults to 10 seconds.</value>
    public TimeSpan TrackingTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Gets or sets a value indicating whether Wolverine tracking also waits for messages sent to external
    /// transports. Only useful when the host runs real brokers.
    /// </summary>
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool IncludeExternalMessages { get; set; }
}
