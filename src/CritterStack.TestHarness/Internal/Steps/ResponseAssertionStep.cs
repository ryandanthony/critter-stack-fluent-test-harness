// -----------------------------------------------------------------------
// <copyright file="ResponseAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Reads the most recent <c>WhenHttp</c>'s JSON body as <typeparamref name="TBody"/> and asserts on it.
/// </summary>
/// <typeparam name="TBody">The body type.</typeparam>
internal sealed class ResponseAssertionStep<TBody> : IScenarioStep
{
    private readonly Func<TBody, bool> _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResponseAssertionStep{TBody}"/> class.
    /// </summary>
    /// <param name="predicate">The check.</param>
    internal ResponseAssertionStep(Func<TBody, bool> predicate)
    {
        _predicate = predicate;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Then;

    /// <inheritdoc/>
    public string Describe() => $"ThenResponse<{Failure.TypeName(typeof(TBody))}>";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var result = HttpBody.RequireResult(context);
        var text = await HttpBody.ReadAsync(result).ConfigureAwait(false);
        if (string.IsNullOrWhiteSpace(text) || string.Equals(text, HttpBody.Empty, StringComparison.Ordinal))
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} answered {result.Context.Response.StatusCode} with an empty body, so there is no {Failure.TypeName(typeof(TBody))} to check");
        }

        TBody body;
        try
        {
            body = await result.ReadAsJsonAsync<TBody>().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"the body of {context.ActionLabel} could not be read as {Failure.TypeName(typeof(TBody))}: {ExceptionClassifier.Describe(ex)}.{Environment.NewLine}Response body:{Environment.NewLine}  {text}",
                ex);
        }

        if (!Predicates.Evaluate(context, () => _predicate(body)))
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"the body of {context.ActionLabel} did not match the predicate.{Environment.NewLine}Response body:{Environment.NewLine}  {text}");
        }
    }
}
