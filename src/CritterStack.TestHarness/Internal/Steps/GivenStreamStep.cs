// -----------------------------------------------------------------------
// <copyright file="GivenStreamStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Writes events to a stream: starts it on the scenario's first Given for the id, appends after that.
/// </summary>
internal sealed class GivenStreamStep : IStreamStep
{
    private readonly Func<object[]> _events;
    private readonly Type? _aggregateType;

    /// <summary>
    /// Initializes a new instance of the <see cref="GivenStreamStep"/> class.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="events">Builds the events, from the id in the form the test passed it.</param>
    /// <param name="aggregateType">The aggregate type to start the stream as, or null.</param>
    internal GivenStreamStep(StreamKey id, Func<object[]> events, Type? aggregateType)
    {
        StreamId = id;
        _events = events;
        _aggregateType = aggregateType;
    }

    /// <inheritdoc/>
    public StreamKey StreamId { get; }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Given;

    /// <inheritdoc/>
    public string Describe() =>
        _aggregateType is null ? $"GivenStream({StreamId})" : $"GivenStream<{Failure.TypeName(_aggregateType)}>({StreamId})";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        object[] events;
        try
        {
            events = _events();
        }
        catch (Exception ex)
        {
            throw Failure.Create(context.CurrentStepLabel, $"building the events threw {ExceptionClassifier.Describe(ex)}", ex);
        }

        try
        {
            await using var session = context.OpenSession();
            if (context.StartedStreams.Contains(StreamId))
            {
                StreamId.Append(session, events);
            }
            else
            {
                StreamId.StartStream(session, _aggregateType, events);
            }

            await session.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"writing the Given failed with {ExceptionClassifier.Describe(ex)}. If the stream already exists, use a fresh id per test.",
                ex);
        }

        context.StartedStreams.Add(StreamId);
        context.ProjectionsDirty = true;
    }
}
