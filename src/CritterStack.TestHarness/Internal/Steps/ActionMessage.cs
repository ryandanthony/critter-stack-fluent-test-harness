// -----------------------------------------------------------------------
// <copyright file="ActionMessage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Builds the message for a <c>When</c> or <c>WhenQuery</c> from the caller's factory.
/// </summary>
internal static class ActionMessage
{
    /// <summary>
    /// Calls the factory, failing clearly if it throws or returns null.
    /// </summary>
    /// <param name="context">The scenario context, for the step label.</param>
    /// <param name="factory">The caller's factory.</param>
    /// <param name="kind">"command" or "query", for messages.</param>
    /// <returns>The message.</returns>
    internal static object Build(ScenarioContext context, Func<object> factory, string kind)
    {
        object? message;
        try
        {
            message = factory();
        }
        catch (Exception ex)
        {
            throw Failure.Create(context.CurrentStepLabel, $"building the {kind} threw {ExceptionClassifier.Describe(ex)}", ex);
        }

        return message ?? throw Failure.Create(context.CurrentStepLabel, $"the {kind} factory returned null");
    }
}
