// -----------------------------------------------------------------------
// <copyright file="PublishedAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Asserts that the most recent <c>When</c> sent a message, using the tracked session's <c>Sent</c> records.
/// </summary>
/// <typeparam name="TMessage">The message type.</typeparam>
internal sealed class PublishedAssertionStep<TMessage> : IScenarioStep
{
    private readonly Func<TMessage, bool>? _predicate;

    /// <summary>
    /// Initializes a new instance of the <see cref="PublishedAssertionStep{TMessage}"/> class.
    /// </summary>
    /// <param name="predicate">Optional filter.</param>
    internal PublishedAssertionStep(Func<TMessage, bool>? predicate)
    {
        _predicate = predicate;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Then;

    /// <inheritdoc/>
    public string Describe() => $"ThenPublished<{Failure.TypeName(typeof(TMessage))}>";

    /// <inheritdoc/>
    public Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var tracked = context.TrackedSession
            ?? throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} threw, so there is no tracked session to check",
                context.ActionException);

        var sent = tracked.Sent.AllMessages().ToList();
        var found = sent.OfType<TMessage>()
            .Any(m => _predicate is null || Predicates.Evaluate(context, () => _predicate(m)));
        if (!found)
        {
            var subject = Failure.TypeName(typeof(TMessage)) + (_predicate is null ? string.Empty : " matching the predicate");
            throw Failure.Create(
                context.CurrentStepLabel,
                $"{context.ActionLabel} did not send {subject}.{Environment.NewLine}Sent messages:{Environment.NewLine}{PayloadFormatter.FormatMessages(context, sent)}");
        }

        return Task.CompletedTask;
    }
}
