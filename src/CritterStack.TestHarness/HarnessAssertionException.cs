// -----------------------------------------------------------------------
// <copyright file="HarnessAssertionException.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// Thrown when a scenario step fails. The message starts with the failing step and, for assertion failures,
/// shows what was actually there. When the failure was caused by an exception, that exception is the
/// <see cref="Exception.InnerException"/>.
/// </summary>
public sealed class HarnessAssertionException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="HarnessAssertionException"/> class.
    /// </summary>
    public HarnessAssertionException()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HarnessAssertionException"/> class.
    /// </summary>
    /// <param name="message">The failure message.</param>
    public HarnessAssertionException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HarnessAssertionException"/> class.
    /// </summary>
    /// <param name="message">The failure message.</param>
    /// <param name="innerException">The exception that caused the failure.</param>
    public HarnessAssertionException(string message, Exception? innerException)
        : base(message, innerException)
    {
    }
}
