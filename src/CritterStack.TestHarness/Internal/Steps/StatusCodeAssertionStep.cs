// -----------------------------------------------------------------------
// <copyright file="StatusCodeAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Asserts the status code of the most recent <c>WhenHttp</c>'s response.
/// </summary>
internal sealed class StatusCodeAssertionStep : IScenarioStep
{
    private readonly int _expected;

    /// <summary>
    /// Initializes a new instance of the <see cref="StatusCodeAssertionStep"/> class.
    /// </summary>
    /// <param name="expected">The expected status code.</param>
    internal StatusCodeAssertionStep(int expected)
    {
        _expected = expected;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Then;

    /// <inheritdoc/>
    public string Describe() => $"ThenStatusCode({_expected})";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var result = HttpBody.RequireResult(context);
        var actual = result.Context.Response.StatusCode;
        if (actual != _expected)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"expected status {_expected} from {context.ActionLabel}, got {actual}.{Environment.NewLine}Response body:{Environment.NewLine}  {await HttpBody.ReadAsync(result).ConfigureAwait(false)}");
        }
    }
}
