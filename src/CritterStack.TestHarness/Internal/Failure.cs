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
