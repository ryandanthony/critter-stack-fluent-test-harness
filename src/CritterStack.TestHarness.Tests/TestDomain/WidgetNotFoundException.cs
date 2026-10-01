// -----------------------------------------------------------------------
// <copyright file="WidgetNotFoundException.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Thrown by the <see cref="GetWidgetById"/> handler for an unknown widget.
/// </summary>
public sealed class WidgetNotFoundException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetNotFoundException"/> class.
    /// </summary>
    public WidgetNotFoundException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public WidgetNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetNotFoundException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WidgetNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
