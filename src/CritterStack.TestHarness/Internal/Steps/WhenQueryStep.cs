// -----------------------------------------------------------------------
// <copyright file="WhenQueryStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Microsoft.Extensions.DependencyInjection;
using Wolverine;

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Invokes a query through Wolverine after syncing projections, and checks the response is a
/// <typeparamref name="TResponse"/> (Wolverine itself returns <c>default</c> for a wrong type instead of throwing).
/// </summary>
/// <typeparam name="TResponse">The expected response type.</typeparam>
internal sealed class WhenQueryStep<TResponse> : IActionStep
{
    private readonly Func<object> _query;
    private string? _queryName;

    /// <summary>
    /// Initializes a new instance of the <see cref="WhenQueryStep{TResponse}"/> class.
    /// </summary>
    /// <param name="query">Builds the query.</param>
    internal WhenQueryStep(Func<object> query)
    {
        _query = query;
    }

    /// <inheritdoc/>
    public StepKind Kind => StepKind.Action;

    /// <inheritdoc/>
    public string Describe()
    {
        var response = Failure.TypeName(typeof(TResponse));
        return _queryName is null ? $"WhenQuery<{response}>" : $"WhenQuery<{response}>({_queryName})";
    }

    /// <inheritdoc/>
    public async Task ExecuteAsync(ScenarioContext context, CancellationToken cancellationToken)
    {
        var query = ActionMessage.Build(context, _query, "query");
        _queryName = Failure.TypeName(query.GetType());
        context.Relabel(this);
        context.BeginAction(context.CurrentStepLabel);

        await ProjectionSyncRunner.SyncIfDirtyAsync(context, cancellationToken).ConfigureAwait(false);

        // IMessageBus is scoped. Resolving it from the root provider throws when the host validates scopes, which
        // .NET turns on in the Development environment (the default under an IDE's test runner).
        var scope = context.Host.Services.CreateAsyncScope();
        await using var scopeDisposal = scope.ConfigureAwait(false);
        var bus = scope.ServiceProvider.GetRequiredService<IMessageBus>();
        object? result;
        try
        {
            result = context.TenantId is null
                ? await bus.InvokeAsync<object>(query, cancellationToken).ConfigureAwait(false)
                : await bus.InvokeForTenantAsync<object>(context.TenantId, query, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            var reason = ExceptionClassifier.InfrastructureReason(ex, query.GetType(), context.Options.TrackingTimeout, TimeSpan.Zero);
            if (reason is not null)
            {
                throw Failure.Create(context.CurrentStepLabel, reason, ex);
            }

            context.ActionException = ex;
            return;
        }

        if (result is not null and not TResponse)
        {
            throw Failure.Create(
                context.CurrentStepLabel,
                $"expected the handler to return {Failure.TypeName(typeof(TResponse))}, but it returned {Failure.TypeName(result.GetType())}");
        }

        context.QueryResult = result;
    }
}
