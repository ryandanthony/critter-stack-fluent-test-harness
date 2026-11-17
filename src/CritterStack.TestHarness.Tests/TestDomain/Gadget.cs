// -----------------------------------------------------------------------
// <copyright file="Gadget.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0048 // Small related records are kept together in the test domain.
#pragma warning disable SA1402 // Small related records are kept together in the test domain.
#pragma warning disable SA1649 // Small related records are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// The string-keyed test domain's aggregate, on the host with <c>StreamIdentity.AsString</c>. Only used as the
/// stream's aggregate type.
/// </summary>
public sealed class Gadget
{
    /// <summary>Gets or sets the stream key.</summary>
    /// <value>The gadget key.</value>
    public string Id { get; set; } = string.Empty;
}

/// <summary>A strongly typed gadget key that wraps a string, used as a stream id.</summary>
/// <param name="Value">The stream key.</param>
public readonly record struct GadgetCode(string Value);

/// <summary>A gadget was created.</summary>
/// <param name="Key">The gadget key.</param>
/// <param name="Name">The gadget name.</param>
public sealed record GadgetCreated(string Key, string Name);

/// <summary>A gadget was renamed.</summary>
/// <param name="Key">The gadget key.</param>
/// <param name="Name">The new name.</param>
public sealed record GadgetRenamed(string Key, string Name);

/// <summary>Renames a gadget.</summary>
/// <param name="Key">The gadget key.</param>
/// <param name="Name">The new name.</param>
public sealed record RenameGadget(string Key, string Name);

/// <summary>Its handler always throws <see cref="WidgetException"/>.</summary>
/// <param name="Key">The gadget key.</param>
public sealed record FailGadget(string Key);

/// <summary>Returns the gadget's async summary, or null.</summary>
/// <param name="Key">The gadget key.</param>
public sealed record FindGadget(string Key);

/// <summary>
/// Async read model of a gadget, keyed by the stream key.
/// </summary>
public sealed class GadgetSummary
{
    /// <summary>Gets or sets the stream key.</summary>
    /// <value>The gadget key.</value>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets or sets the name.</summary>
    /// <value>The gadget name.</value>
    public string Name { get; set; } = string.Empty;
}
