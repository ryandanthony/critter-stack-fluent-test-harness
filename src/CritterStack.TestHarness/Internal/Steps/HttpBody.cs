// -----------------------------------------------------------------------
// <copyright file="HttpBody.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Alba;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Shared helpers for the HTTP assertions.
/// </summary>
internal static class HttpBody
{
    /// <summary>What failure messages show for a response with no body.</summary>
    internal const string Empty = "(empty)";

    /// <summary>
    /// Gets the most recent <c>WhenHttp</c>'s result, failing clearly if the request threw instead of answering.
    /// </summary>
    /// <param name="context">The scenario context.</param>
    /// <returns>The result.</returns>
    internal static IScenarioResult RequireResult(ScenarioContext context)
    {
        if (context.ActionException is { } thrown)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} threw {ExceptionClassifier.Describe(thrown)}, so there is no response",
                thrown);
        }

        return context.HttpResult
            ?? throw Failure.Create(context.CurrentStepLabel, "no HTTP response was recorded for the most recent WhenHttp; this is a harness bug");
    }

    /// <summary>
    /// Reads the response body as text for a failure message. The body stays readable afterwards.
    /// </summary>
    /// <param name="result">The result.</param>
    /// <returns>The body, or <see cref="Empty"/>.</returns>
    internal static async Task<string> ReadAsync(IScenarioResult result)
    {
        var text = await result.ReadAsTextAsync().ConfigureAwait(false);
        return string.IsNullOrEmpty(text) ? Empty : text;
    }
}
