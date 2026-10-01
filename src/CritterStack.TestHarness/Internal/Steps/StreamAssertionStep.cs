// -----------------------------------------------------------------------
// <copyright file="StreamAssertionStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JasperFx.Events;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Asserts on a stream's events, either the whole stream or only what the most recent <c>When</c> appended.
/// </summary>
internal sealed class StreamAssertionStep : IScenarioStep
{
    private readonly StreamCheck _check;
    private readonly Type? _eventType;
    private readonly Func<object, bool>? _matches;
    private readonly bool _hasPredicate;
    private readonly int _expectedCount;

    private StreamAssertionStep(Guid streamId, StreamScope scope, StreamCheck check, Type? eventType, Func<object, bool>? matches, bool hasPredicate, int expectedCount)
    {
        StreamId = streamId;
        Scope = scope;
        _check = check;
        _eventType = eventType;
        _matches = matches;
        _hasPredicate = hasPredicate;
        _expectedCount = expectedCount;
    }

    /// <summary>Gets the stream id.</summary>
    /// <value>The stream id.</value>
    internal Guid StreamId { get; }

    /// <summary>Gets which events the assertion looks at.</summary>
    /// <value>The scope.</value>
    internal StreamScope Scope { get; }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Then;

    /// <summary>
    /// Creates a "has" or "has no" assertion for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="scope">Which events to look at.</param>
    /// <param name="check"><see cref="StreamCheck.Has"/> or <see cref="StreamCheck.HasNo"/>.</param>
    /// <param name="predicate">Optional filter.</param>
    /// <returns>The step.</returns>
    internal static StreamAssertionStep ForEvent<TEvent>(Guid id, StreamScope scope, StreamCheck check, Func<TEvent, bool>? predicate) =>
        new(id, scope, check, typeof(TEvent), data => data is TEvent e && (predicate is null || predicate(e)), predicate is not null, 0);

    /// <summary>
    /// Creates an event count assertion.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="scope">Which events to look at.</param>
    /// <param name="expected">The expected count.</param>
    /// <returns>The step.</returns>
    internal static StreamAssertionStep ForCount(Guid id, StreamScope scope, int expected) =>
        new(id, scope, StreamCheck.Count, null, null, false, expected);

    /// <inheritdoc/>
    public string Describe()
    {
        var name = (Scope, _check) switch
        {
            (StreamScope.Whole, StreamCheck.Has) => "ThenStreamHasEvent",
            (StreamScope.Whole, StreamCheck.HasNo) => "ThenStreamHasNoEvent",
            (StreamScope.Whole, _) => "ThenStreamEventCount",
            (_, StreamCheck.Has) => "ThenAppended",
            (_, StreamCheck.HasNo) => "ThenNotAppended",
            _ => "ThenAppendedCount",
        };

        return _eventType is null
            ? $"{name}({StreamId}, {_expectedCount})"
            : $"{name}<{Failure.TypeName(_eventType)}>({StreamId})";
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var events = await LoadAsync(context, cancellationToken).ConfigureAwait(false);
        var where = Scope == StreamScope.Whole ? "in the stream" : $"appended by {context.ActionLabel}";

        if (_check == StreamCheck.Count && events.Count != _expectedCount)
        {
            throw Fail(context, $"expected {_expectedCount} event(s) {where}, found {events.Count}", events);
        }

        if (_check == StreamCheck.Has && !AnyMatch(context, events))
        {
            throw Fail(context, $"no {Subject()} {where}", events);
        }

        if (_check == StreamCheck.HasNo && AnyMatch(context, events))
        {
            throw Fail(context, $"found {Subject()} {where}, expected none", events);
        }
    }

    private async Task<IReadOnlyList<IEvent>> LoadAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        await using var session = context.OpenQuerySession();
        if (Scope == StreamScope.Whole)
        {
            return await session.Events.FetchStreamAsync(StreamId, token: cancellationToken).ConfigureAwait(false);
        }

        if (!context.Snapshots.TryGetValue(StreamId, out var snapshot))
        {
            throw Failure.Create(context.CurrentStepLabel, "no stream snapshot was taken before the When; this is a harness bug");
        }

        return await session.Events.FetchStreamAsync(StreamId, fromVersion: snapshot + 1, token: cancellationToken).ConfigureAwait(false);
    }

    private bool AnyMatch(ScenarioContext context, IReadOnlyList<IEvent> events) =>
        events.Any(e => Predicates.Evaluate(context, () => _matches!(e.Data)));

    private string Subject()
    {
        var type = Failure.TypeName(_eventType!);
        return _hasPredicate ? $"{type} matching the predicate" : type;
    }

    private static ScenarioAssertionException Fail(ScenarioContext context, string message, IReadOnlyList<IEvent> events) =>
        Failure.Create(
            context.CurrentStepLabel,
            $"{message}.{Environment.NewLine}Actual events:{Environment.NewLine}{PayloadFormatter.FormatEvents(context, events)}");
}
