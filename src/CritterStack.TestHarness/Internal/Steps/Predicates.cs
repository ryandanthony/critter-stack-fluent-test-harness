// -----------------------------------------------------------------------
// <copyright file="Predicates.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Runs caller-supplied predicates so an exception inside one fails the step clearly instead of escaping raw.
/// </summary>
internal static class Predicates
{
    /// <summary>
    /// Evaluates a predicate.
    /// </summary>
    /// <param name="context">The scenario context, for the step label.</param>
    /// <param name="predicateCall">The predicate call.</param>
    /// <returns>The predicate's result.</returns>
    /// <exception cref="ScenarioAssertionException">The predicate threw.</exception>
    internal static bool Evaluate(ScenarioContext context, Func<bool> predicateCall)
    {
        try
        {
            return predicateCall();
        }
        catch (Exception ex)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"the predicate threw {ExceptionClassifier.Describe(ex)}",
                ex);
        }
    }
}
