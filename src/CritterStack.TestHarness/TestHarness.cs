// -----------------------------------------------------------------------
// <copyright file="TestHarness.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Hosting;

namespace CritterStack.TestHarness;

/// <summary>
/// Entry point for fluent Given/When/Then scenarios against a Marten and Wolverine host.
/// </summary>
[SuppressMessage("Naming", "CA1724:Type names should not match namespaces", Justification = "TestHarness.Setup(...) is the designed entry point; renaming either the type or the namespace would break the public API.")]
[SuppressMessage("Design", "MA0049:Type name should not match containing namespace", Justification = "TestHarness.Setup(...) is the designed entry point; renaming either the type or the namespace would break the public API.")]
public static class TestHarness
{
    /// <summary>
    /// Starts a scenario against <paramref name="host"/>. The harness never builds, starts or disposes the host; the
    /// caller's fixture owns it. An <c>IAlbaHost</c> is an <see cref="IHost"/>, so any Alba fixture works.
    /// </summary>
    /// <param name="host">A running host with Marten and Wolverine registered.</param>
    /// <param name="configure">Optional callback that sets <see cref="HarnessOptions"/>.</param>
    /// <returns>The start stage, where <c>ForTenant</c> or the first Given can be called.</returns>
    /// <example>
    /// <code>
    /// await TestHarness
    ///     .Setup(fixture.Host)
    ///     .GivenStream&lt;Widget&gt;(id, i =&gt; [new WidgetCreated(i, "Sprocket")])
    ///     .When(() =&gt; new RenameWidget(id, "Gear"))
    ///     .ThenAppended&lt;WidgetRenamed&gt;(id, e =&gt; e.Name == "Gear");
    /// </code>
    /// </example>
    public static IScenarioStart Setup(IHost host, Action<HarnessOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(host);

        var options = new HarnessOptions();
        configure?.Invoke(options);

        return new Internal.ScenarioBuilder(host, options);
    }
}
