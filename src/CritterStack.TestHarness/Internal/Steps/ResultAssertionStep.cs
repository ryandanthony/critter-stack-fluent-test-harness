// -----------------------------------------------------------------------
// <copyright file="ResultAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Asserts on the most recent <c>WhenQuery</c>'s response.
/// </summary>
/// <typeparam name="TResponse">The response type.</typeparam>
internal sealed class ResultAssertionStep<TResponse> : IScenarioStep
{
    private readonly Func<TResponse, bool> _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ResultAssertionStep{TResponse}"/> class.
    /// </summary>
    /// <param name="predicate">The check.</param>
    internal ResultAssertionStep(Func<TResponse, bool> predicate)
    {
        _predicate = predicate;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Then;

    /// <inheritdoc/>
    public string Describe() => $"ThenResult<{Failure.TypeName(typeof(TResponse))}>";

    /// <inheritdoc/>
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        if (context.ActionException is { } thrown)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} threw {ExceptionClassifier.Describe(thrown)}, so there is no result",
                thrown);
        }

        var result = context.QueryResult is TResponse typed ? typed : default!;
        if (!Predicates.Evaluate(context, () => _predicate(result)))
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"the result of {context.ActionLabel} did not match the predicate.{Environment.NewLine}Actual result:{Environment.NewLine}  {PayloadFormatter.Json(context, context.QueryResult)}");
        }

        return Task.CompletedTask;
    }
}
