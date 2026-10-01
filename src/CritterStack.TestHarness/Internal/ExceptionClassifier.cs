// -----------------------------------------------------------------------
// <copyright file="ExceptionClassifier.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using Wolverine.Runtime.Routing;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Tells handler exceptions (behaviour under test, claimable by <c>ThenThrows</c>) apart from infrastructure
/// failures (test setup bugs, which fail the scenario at once). The rules match Marten 9.44 and Wolverine 6.44 as
/// verified in the design's classifier table.
/// </summary>
internal static class ExceptionClassifier
{
    private const string TrackedSessionTimeoutMessage = "This TrackedSession timed out";

    /// <summary>
    /// A real tracking timeout only fires once the configured timeout has passed. A slightly early timer is
    /// tolerated; a handler's own <see cref="TimeoutException"/> thrown well before then is not a tracking timeout.
    /// </summary>
    private const double TimeoutTolerance = 0.9;

    /// <summary>
    /// Describes why an exception from a <c>When</c> or <c>WhenQuery</c> is an infrastructure failure.
    /// </summary>
    /// <param name="exception">The exception the invoke threw.</param>
    /// <param name="messageType">The command or query type.</param>
    /// <param name="trackingTimeout">The tracking timeout in force.</param>
    /// <param name="elapsed">How long the invoke ran before it threw.</param>
    /// <returns>The reason, or null if the exception came from the handler.</returns>
    internal static string? InfrastructureReason(Exception exception, Type messageType, TimeSpan trackingTimeout, TimeSpan elapsed)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is IndeterminateRoutesException)
        {
            return $"no Wolverine handler is registered for {Failure.TypeName(messageType)}";
        }

        if (IsTrackedSessionWrapper(exception) && elapsed >= trackingTimeout * TimeoutTolerance)
        {
            return $"Wolverine tracking timed out after {trackingTimeout} (TrackingTimeout) waiting for {Failure.TypeName(messageType)} and its cascaded messages";
        }

        return null;
    }

    /// <summary>
    /// Gets the exception a <c>ThenThrows</c> should see for a handler exception. Wolverine's tracked session wraps
    /// a handler's own <see cref="TimeoutException"/> in its "TrackedSession timed out" exception; this unwraps it.
    /// </summary>
    /// <param name="exception">The exception the invoke threw, already known not to be infrastructure.</param>
    /// <returns>The handler's exception.</returns>
    internal static Exception HandlerException(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return IsTrackedSessionWrapper(exception) ? exception.InnerException! : exception;
    }

    /// <summary>
    /// Finds the exception <c>ThenThrows&lt;T&gt;</c> should match: the exception itself, or for a cascaded
    /// handler's <see cref="AggregateException"/>, the first inner exception of the type.
    /// </summary>
    /// <typeparam name="TException">The expected type.</typeparam>
    /// <param name="exception">The captured exception.</param>
    /// <returns>The match, or null.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The expected exception type comes from ThenThrows<TException> and can't be inferred from the captured exception.")]
    internal static TException? FindMatch<TException>(Exception exception)
        where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            TException direct => direct,
            AggregateException aggregate => aggregate.Flatten().InnerExceptions.OfType<TException>().FirstOrDefault(),
            _ => null,
        };
    }

    /// <summary>
    /// Describes a captured exception's type, listing the inner types of an <see cref="AggregateException"/>.
    /// </summary>
    /// <param name="exception">The exception.</param>
    /// <returns>For example <c>AggregateException [WidgetException]</c>.</returns>
    internal static string Describe(Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (exception is AggregateException aggregate)
        {
            var inner = aggregate.Flatten().InnerExceptions.Select(e => Failure.TypeName(e.GetType()));
            return $"{nameof(AggregateException)} [{string.Join(", ", inner)}]";
        }

        return Failure.TypeName(exception.GetType());
    }

    private static bool IsTrackedSessionWrapper(Exception exception) =>
        exception is TimeoutException
        && exception.InnerException is TimeoutException
        && exception.Message.StartsWith(TrackedSessionTimeoutMessage, StringComparison.Ordinal);
}
