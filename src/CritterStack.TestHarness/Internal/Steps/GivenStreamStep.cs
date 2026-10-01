// -----------------------------------------------------------------------
// <copyright file="GivenStreamStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Marten;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Writes events to a stream: starts it on the scenario's first Given for the id, appends after that.
/// </summary>
internal sealed class GivenStreamStep : IScenarioStep
{
    private readonly Guid _id;
    private readonly Func<Guid, object[]> _events;
    private readonly Action<IDocumentSession, Guid, object[]> _start;
    private readonly string _aggregateName;

    /// <summary>
    /// Initializes a new instance of the <see cref="GivenStreamStep"/> class.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="events">Builds the events.</param>
    /// <param name="start">Starts the stream, with or without an aggregate type.</param>
    /// <param name="aggregateName">The aggregate type name for descriptions, or empty.</param>
    internal GivenStreamStep(Guid id, Func<Guid, object[]> events, Action<IDocumentSession, Guid, object[]> start, string aggregateName)
    {
        _id = id;
        _events = events;
        _start = start;
        _aggregateName = aggregateName;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Given;

    /// <inheritdoc/>
    public string Describe() =>
        _aggregateName.Length == 0 ? $"GivenStream({_id})" : $"GivenStream<{_aggregateName}>({_id})";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        object[] events;
        try
        {
            events = _events(_id);
        }
        catch (Exception ex)
        {
            throw Failure.Create(context.CurrentStepLabel, $"building the events threw {ExceptionClassifier.Describe(ex)}", ex);
        }

        try
        {
            await using var session = context.OpenSession();
            if (context.StartedStreams.Contains(_id))
            {
                session.Events.Append(_id, events);
            }
            else
            {
                _start(session, _id, events);
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

        context.StartedStreams.Add(_id);
        context.ProjectionsDirty = true;
    }
}
