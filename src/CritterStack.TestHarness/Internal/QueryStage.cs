// -----------------------------------------------------------------------
// <copyright file="QueryStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CritterStack.TestHarness.Internal.Steps;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// The typed stage after a <c>WhenQuery</c>. Records every step on the owning <see cref="ScenarioBuilder"/>.
/// </summary>
/// <typeparam name="TResponse">The query's response type.</typeparam>
internal sealed class QueryStage<TResponse> : IQueryStage<TResponse>
{
    private readonly ScenarioBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="QueryStage{TResponse}"/> class.
    /// </summary>
    /// <param name="builder">The owning builder.</param>
    internal QueryStage(ScenarioBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenResult(Func<TResponse, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(new ResultAssertionStep<TResponse>(predicate));
    }

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasNoEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamHasNoEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamEventCount(Guid id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamEventCount(string id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenStreamEventCount<TId>(TId id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.ForTyped(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IQueryStage<TResponse> ThenThrows<TException>(Func<TException, bool>? predicate = null)
        where TException : Exception =>
        Add(new ThrowsAssertionStep<TException>(predicate));

    /// <inheritdoc/>
    public ICommandStage When(Func<object> command) => _builder.When(command);

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IThenStage.WhenQuery<TResponse>.")]
    public IQueryStage<TNext> WhenQuery<TNext>(Func<object> query) => _builder.WhenQuery<TNext>(query);

    /// <inheritdoc/>
    public IHttpStage WhenHttp(Action<Alba.Scenario> request) => _builder.WhenHttp(request);

    /// <inheritdoc/>
    public Task RunAsync(CancellationToken cancellationToken = default) => _builder.RunAsync(cancellationToken);

    /// <inheritdoc/>
    public TaskAwaiter GetAwaiter() => _builder.GetAwaiter();

    private QueryStage<TResponse> Add(IScenarioStep step)
    {
        _builder.AddStep(step);
        return this;
    }
}
