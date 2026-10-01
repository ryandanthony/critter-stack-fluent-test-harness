// -----------------------------------------------------------------------
// <copyright file="WhenCommandStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Wolverine;
using Wolverine.Tracking;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Invokes a command under Wolverine tracking, after syncing projections and snapshotting the versions of the
/// streams that later appended-event assertions read.
/// </summary>
internal sealed class WhenCommandStep : IActionStep
{
    private readonly Func<object> _command;
    private string? _commandName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhenCommandStep"/> class.
    /// </summary>
    /// <param name="command">Builds the command.</param>
    internal WhenCommandStep(Func<object> command)
    {
        _command = command;
    }

    /// <summary>
    /// Gets the stream ids to snapshot before invoking, filled in by the runner from the assertions that follow.
    /// </summary>
    /// <value>The stream ids.</value>
    internal HashSet<Guid> SnapshotIds { get; } = [];

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Action;

    /// <inheritdoc/>
    public string Describe() => _commandName is null ? "When" : $"When({_commandName})";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var command = ActionMessage.Build(context, _command, "command");
        _commandName = Failure.TypeName(command.GetType());
        context.Relabel(this);
        context.BeginAction(context.CurrentStepLabel);

        await ProjectionSyncRunner.SyncIfDirtyAsync(context, cancellationToken).ConfigureAwait(false);
        await SnapshotAsync(context, cancellationToken).ConfigureAwait(false);

        var tracking = context.Host.TrackActivity(context.Options.TrackingTimeout);
        if (context.Options.IncludeExternalMessages)
        {
            tracking = tracking.IncludeExternalTransports();
        }

        var tenantId = context.TenantId;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            context.TrackedSession = await tracking
                .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(c => tenantId is null
                    ? c.InvokeAsync(command, cancellationToken)
                    : c.InvokeForTenantAsync(tenantId, command, cancellationToken)))
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = ExceptionClassifier.InfrastructureReason(ex, command.GetType(), context.Options.TrackingTimeout, stopwatch.Elapsed);
            if (reason is not null)
            {
                throw Failure.Create(context.CurrentStepLabel, reason, ex);
            }

            context.ActionException = ExceptionClassifier.HandlerException(ex);
        }
        finally
        {
            context.ProjectionsDirty = true;
        }
    }

    private async Task SnapshotAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        if (SnapshotIds.Count == 0)
        {
            return;
        }

        await using var session = context.OpenQuerySession();
        foreach (var id in SnapshotIds)
        {
            var state = await session.Events.FetchStreamStateAsync(id, cancellationToken).ConfigureAwait(false);
            context.Snapshots[id] = state?.Version ?? 0;
        }
    }
}
