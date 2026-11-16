// -----------------------------------------------------------------------
// <copyright file="WhenHttpStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Alba;
using Wolverine;
using Wolverine.Tracking;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Sends an HTTP request through Alba under Wolverine tracking, after syncing projections and snapshotting the
/// streams that later appended-event assertions read. Alba's own status check is turned off; the harness requires a
/// 2xx unless a <c>ThenStatusCode</c> follows.
/// </summary>
internal sealed class WhenHttpStep : ISnapshottingStep
{
    private const int FirstSuccessStatus = 200;
    private const int LastSuccessStatus = 299;

    private readonly Action<Scenario> _request;
    private string? _requestLine;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhenHttpStep"/> class.
    /// </summary>
    /// <param name="request">Configures the Alba request.</param>
    internal WhenHttpStep(Action<Scenario> request)
    {
        _request = request;
    }

    /// <inheritdoc/>
    public HashSet<Guid> SnapshotIds { get; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether a <c>ThenStatusCode</c> follows this step before the next action, set
    /// by the runner. When it does, any status is accepted here and the assertion decides.
    /// </summary>
    /// <value>True when the status is asserted explicitly.</value>
    internal bool StatusAsserted { get; set; }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Action;

    /// <inheritdoc/>
    public string Describe() => _requestLine is null ? "WhenHttp" : $"WhenHttp({_requestLine})";

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        context.BeginAction(context.CurrentStepLabel);
        var host = RequireAlbaHost(context);

        await ProjectionSyncRunner.SyncIfDirtyAsync(context, cancellationToken).ConfigureAwait(false);
        await StreamSnapshots.TakeAsync(context, SnapshotIds, cancellationToken).ConfigureAwait(false);

        var result = await SendTrackedAsync(context, host, cancellationToken).ConfigureAwait(false);
        if (result is null)
        {
            return;
        }

        context.HttpResult = result;
        var status = result.Context.Response.StatusCode;
        if (!StatusAsserted && status is < FirstSuccessStatus or > LastSuccessStatus)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"expected a 2xx status, got {status}. To expect it, put ThenStatusCode({status}) after this step.{Environment.NewLine}Response body:{Environment.NewLine}  {await HttpBody.ReadAsync(result).ConfigureAwait(false)}");
        }
    }

    private static IAlbaHost RequireAlbaHost(ScenarioContext context)
    {
        var host = context.Host as IAlbaHost
            ?? throw Failure.Create(
                context.CurrentStepLabel,
                $"WhenHttp needs an Alba host (IAlbaHost), but Setup was given {Failure.TypeName(context.Host.GetType())}");

        if (context.TenantId is not null && context.Options.HttpTenant is null)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                "the scenario has ForTenant but HarnessOptions.HttpTenant is not set, so the request can't carry the tenant. Set it to how your service reads the tenant, for example (x, tenant) => x.WithRequestHeader(\"tenant\", tenant)");
        }

        return host;
    }

    /// <summary>
    /// Sends the request under tracking. Returns the result, or null when the endpoint threw (captured for
    /// <c>ThenThrows</c>).
    /// </summary>
    private async Task<IScenarioResult?> SendTrackedAsync(ScenarioContext context, IAlbaHost host, CancellationToken cancellationToken)
    {
        var tracking = context.Host.TrackActivity(context.Options.TrackingTimeout);
        if (context.Options.IncludeExternalMessages)
        {
            tracking = tracking.IncludeExternalTransports();
        }

        IScenarioResult? result = null;
        var stopwatch = System.Diagnostics.Stopwatch.StartNew();
        try
        {
            context.TrackedSession = await tracking
                .ExecuteAndWaitAsync((Func<IMessageContext, Task>)(async _ =>
                {
                    result = await host.Scenario(x => Configure(context, x)).ConfigureAwait(false);
                }))
                .ConfigureAwait(false);
            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (Failure.Find(ex) is { } harnessFailure)
        {
            throw harnessFailure;
        }
        catch (Exception ex)
        {
            if (ExceptionClassifier.IsTrackingTimeout(ex, context.Options.TrackingTimeout, stopwatch.Elapsed))
            {
                throw Failure.Create(
                    context.CurrentStepLabel,
                    $"Wolverine tracking timed out after {context.Options.TrackingTimeout} (TrackingTimeout) waiting for the request and the messages it cascaded",
                    ex);
            }

            context.ActionException = ExceptionClassifier.HandlerException(ex);
            return null;
        }
        finally
        {
            context.ProjectionsDirty = true;
        }
    }

    private void Configure(ScenarioContext context, Scenario scenario)
    {
        try
        {
            _request(scenario);
            if (context.TenantId is not null)
            {
                context.Options.HttpTenant!(scenario, context.TenantId);
            }
        }
        catch (Exception ex)
        {
            throw Failure.Create(context.CurrentStepLabel, $"configuring the request threw {ExceptionClassifier.Describe(ex)}", ex);
        }

        scenario.IgnoreStatusCode();
        scenario.ConfigureHttpContext(c =>
        {
            _requestLine = $"{c.Request.Method} {c.Request.Path}{c.Request.QueryString}";
            context.Relabel(this);
            context.RelabelAction();
        });
    }
}
