// -----------------------------------------------------------------------
// <copyright file="ThrowsAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Claims the most recent action's handler exception.
/// </summary>
/// <typeparam name="TException">The expected exception type.</typeparam>
internal sealed class ThrowsAssertionStep<TException> : IScenarioStep
    where TException : Exception
{
    private readonly Func<TException, bool>? _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThrowsAssertionStep{TException}"/> class.
    /// </summary>
    /// <param name="predicate">Optional check against the matching exception.</param>
    internal ThrowsAssertionStep(Func<TException, bool>? predicate)
    {
        _predicate = predicate;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.ThenThrows;

    /// <inheritdoc/>
    public string Describe() => $"ThenThrows<{Failure.TypeName(typeof(TException))}>";

    /// <inheritdoc/>
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var expected = Failure.TypeName(typeof(TException));
        var exception = context.ActionException
            ?? throw Failure.Create(context.CurrentStepLabel, $"expected {expected}, but {context.ActionLabel} threw nothing");

        var match = ExceptionClassifier.FindMatch<TException>(exception)
            ?? throw Failure.Create(
                context.CurrentStepLabel,
                $"expected {expected}, but {context.ActionLabel} threw {ExceptionClassifier.Describe(exception)}",
                exception);

        if (_predicate is not null && !Predicates.Evaluate(context, () => _predicate(match)))
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} threw {expected}, but it did not match the predicate",
                exception);
        }

        context.ActionExceptionClaimed = true;
        return Task.CompletedTask;
    }
}
