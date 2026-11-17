// -----------------------------------------------------------------------
// <copyright file="HttpStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CritterStack.TestHarness.Internal.Steps;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// The stage after a <c>WhenHttp</c>. Records every step on the owning <see cref="ScenarioBuilder"/>.
/// </summary>
internal sealed class HttpStage : IHttpStage
{
    private readonly ScenarioBuilder _builder;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpStage"/> class.
    /// </summary>
    /// <param name="builder">The owning builder.</param>
    internal HttpStage(ScenarioBuilder builder)
    {
        _builder = builder;
    }

    /// <inheritdoc/>
    public IHttpStage ThenStatusCode(int expected) => Add(new StatusCodeAssertionStep(expected));

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IHttpStage.ThenResponse<TBody>.")]
    public IHttpStage ThenResponse<TBody>(Func<TBody, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);
        return Add(new ResponseAssertionStep<TBody>(predicate));
    }

    /// <inheritdoc/>
    public IHttpStage ThenAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.ForTyped(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenNotAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenNotAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenNotAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.ForTyped(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenAppendedCount(Guid id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public IHttpStage ThenAppendedCount(string id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public IHttpStage ThenAppendedCount<TId>(TId id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.ForTyped(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public IHttpStage ThenPublished<TMessage>(Func<TMessage, bool>? predicate = null) =>
        Add(new PublishedAssertionStep<TMessage>(predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasNoEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamHasNoEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public IHttpStage ThenStreamEventCount(Guid id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IHttpStage ThenStreamEventCount(string id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IHttpStage ThenStreamEventCount<TId>(TId id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.ForTyped(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public IHttpStage ThenThrows<TException>(Func<TException, bool>? predicate = null)
        where TException : Exception =>
        Add(new ThrowsAssertionStep<TException>(predicate));

    /// <inheritdoc/>
    public ICommandStage When(Func<object> command) => _builder.When(command);

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IThenStage.WhenQuery<TResponse>.")]
    public IQueryStage<TResponse> WhenQuery<TResponse>(Func<object> query) => _builder.WhenQuery<TResponse>(query);

    /// <inheritdoc/>
    public IHttpStage WhenHttp(Action<Alba.Scenario> request) => _builder.WhenHttp(request);

    /// <inheritdoc/>
    public Task RunAsync(CancellationToken cancellationToken = default) => _builder.RunAsync(cancellationToken);

    /// <inheritdoc/>
    public TaskAwaiter GetAwaiter() => _builder.GetAwaiter();

    private HttpStage Add(IScenarioStep step)
    {
        _builder.AddStep(step);
        return this;
    }
}
