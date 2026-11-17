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
internal sealed class StreamAssertionStep : IStreamStep
{
    private readonly StreamCheck _check;
    private readonly Type? _eventType;
    private readonly Func<object, bool>? _matches;
    private readonly bool _hasPredicate;
    private readonly int _expectedCount;

    private StreamAssertionStep(StreamAddress stream, StreamScope scope, StreamCheck check, Type? eventType, Func<object, bool>? matches, bool hasPredicate, int expectedCount)
    {
        Stream = stream;
        Scope = scope;
        _check = check;
        _eventType = eventType;
        _matches = matches;
        _hasPredicate = hasPredicate;
        _expectedCount = expectedCount;
    }

    /// <inheritdoc/>
    public StreamAddress Stream { get; }

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
    internal static StreamAssertionStep ForEvent<TEvent>(StreamAddress id, StreamScope scope, StreamCheck check, Func<TEvent, bool>? predicate) =>
        new(id, scope, check, typeof(TEvent), data => data is TEvent e && (predicate is null || predicate(e)), predicate is not null, 0);

    /// <summary>
    /// Creates a "has" assertion for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="scope">Which events to look at.</param>
    /// <param name="predicate">Optional filter.</param>
    /// <returns>The step.</returns>
    internal static StreamAssertionStep Has<TEvent>(StreamAddress id, StreamScope scope, Func<TEvent, bool>? predicate) =>
        ForEvent(id, scope, StreamCheck.Has, predicate);

    /// <summary>
    /// Creates a "has no" assertion for an event type.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="scope">Which events to look at.</param>
    /// <param name="predicate">Optional filter.</param>
    /// <returns>The step.</returns>
    internal static StreamAssertionStep HasNo<TEvent>(StreamAddress id, StreamScope scope, Func<TEvent, bool>? predicate) =>
        ForEvent(id, scope, StreamCheck.HasNo, predicate);

    /// <summary>
    /// Creates an event count assertion.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="scope">Which events to look at.</param>
    /// <param name="expected">The expected count.</param>
    /// <returns>The step.</returns>
    internal static StreamAssertionStep ForCount(StreamAddress id, StreamScope scope, int expected) =>
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

        return (_eventType is null
            ? $"{name}({Stream.Key}, {_expectedCount})"
            : $"{name}<{Failure.TypeName(_eventType)}>({Stream.Key})") + Stream.StoreSuffix;
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
        await using var session = context.OpenQuerySession(Stream.Store);
        if (Scope == StreamScope.Whole)
        {
            return await Stream.Key.FetchAsync(session, 0, cancellationToken).ConfigureAwait(false);
        }

        if (!context.Snapshots.TryGetValue(Stream, out var snapshot))
        {
            throw Failure.Create(context.CurrentStepLabel, "no stream snapshot was taken before the When; this is a harness bug");
        }

        return await Stream.Key.FetchAsync(session, snapshot + 1, cancellationToken).ConfigureAwait(false);
    }

    private bool AnyMatch(ScenarioContext context, IReadOnlyList<IEvent> events) =>
        events.Any(e => Predicates.Evaluate(context, () => _matches!(e.Data)));

    private string Subject()
    {
        var type = Failure.TypeName(_eventType!);
        return _hasPredicate ? $"{type} matching the predicate" : type;
    }

    private HarnessAssertionException Fail(ScenarioContext context, string message, IReadOnlyList<IEvent> events) =>
        Failure.Create(
            context.CurrentStepLabel,
            $"{message}.{Environment.NewLine}Actual events:{Environment.NewLine}{PayloadFormatter.FormatEvents(context, Stream.Store, events)}");
}
