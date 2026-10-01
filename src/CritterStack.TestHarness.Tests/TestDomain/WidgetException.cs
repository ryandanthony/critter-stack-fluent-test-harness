// -----------------------------------------------------------------------
// <copyright file="WidgetException.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>
/// Thrown by the test domain's failing handlers.
/// </summary>
public sealed class WidgetException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetException"/> class.
    /// </summary>
    public WidgetException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    public WidgetException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WidgetException"/> class.
    /// </summary>
    /// <param name="message">The message.</param>
    /// <param name="innerException">The inner exception.</param>
    public WidgetException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
