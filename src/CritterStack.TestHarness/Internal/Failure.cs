// -----------------------------------------------------------------------
// <copyright file="Failure.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Builds <see cref="HarnessAssertionException"/>s with a consistent "step: message" shape.
/// </summary>
internal static class Failure
{
    /// <summary>
    /// Creates a failure for a step.
    /// </summary>
    /// <param name="stepLabel">The failing step's label.</param>
    /// <param name="message">What went wrong.</param>
    /// <param name="innerException">The exception that caused the failure, if any.</param>
    /// <returns>The exception to throw.</returns>
    internal static HarnessAssertionException Create(string stepLabel, string message, Exception? innerException = null) =>
        new($"{stepLabel}: {message}", innerException);

    /// <summary>
    /// Finds a harness failure raised inside code that Wolverine or Alba ran for the harness (and may have wrapped),
    /// so it surfaces as itself rather than being mistaken for a handler exception.
    /// </summary>
    /// <param name="exception">The exception that escaped.</param>
    /// <returns>The harness failure, or null if there is none.</returns>
    internal static HarnessAssertionException? Find(Exception exception)
    {
        for (var current = exception; current is not null; current = current.InnerException)
        {
            if (current is HarnessAssertionException failure)
            {
                return failure;
            }

            if (current is AggregateException aggregate)
            {
                return aggregate.Flatten().InnerExceptions.OfType<HarnessAssertionException>().FirstOrDefault();
            }
        }

        return null;
    }

    /// <summary>
    /// Gets a short, readable name for a type, including generic arguments.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>For example <c>List&lt;String&gt;</c>.</returns>
    internal static string TypeName(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);

        if (!type.IsGenericType)
        {
            return type.Name;
        }

        var name = type.Name[..type.Name.IndexOf('`', StringComparison.Ordinal)];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(TypeName))}>";
    }
}
