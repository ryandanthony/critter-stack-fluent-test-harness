// -----------------------------------------------------------------------
// <copyright file="ScenarioBuilder.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using CritterStack.TestHarness.Internal.Steps;
using Microsoft.Extensions.Hosting;

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// Records a scenario's steps. Implements the start, Given and command stages; each <c>WhenQuery</c> and
/// <c>WhenHttp</c> returns a <see cref="QueryStage{TResponse}"/> or <see cref="HttpStage"/> that forwards back here,
/// so the whole scenario shares one step list.
/// </summary>
internal sealed class ScenarioBuilder : IScenarioStart, ICommandStage
{
    private readonly IHost _host;
    private readonly HarnessOptions _options;
    private readonly List<IScenarioStep> _steps = [];
    private string? _tenantId;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScenarioBuilder"/> class.
    /// </summary>
    /// <param name="host">The host under test.</param>
    /// <param name="options">The harness options.</param>
    internal ScenarioBuilder(IHost host, HarnessOptions options)
    {
        _host = host;
        _options = options;
    }

    /// <summary>
    /// Gets the context from the most recent run, for tests of the harness itself.
    /// </summary>
    /// <value>The last context, or null before the first run.</value>
    internal ScenarioContext? LastContext { get; private set; }

    /// <inheritdoc/>
    public IGivenStage ForTenant(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        _tenantId = tenantId;
        return this;
    }

    /// <inheritdoc/>
    public IGivenStage GivenStream(Guid id, Func<Guid, object[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.For(id), () => events(id), null);
    }

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IGivenStage.GivenStream<TAggregate>.")]
    public IGivenStage GivenStream<TAggregate>(Guid id, Func<Guid, object[]> events)
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.For(id), () => events(id), typeof(TAggregate));
    }

    /// <inheritdoc/>
    public IGivenStage GivenStream(string id, Func<string, object[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.For(id), () => events(id), null);
    }

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IGivenStage.GivenStream<TAggregate>.")]
    public IGivenStage GivenStream<TAggregate>(string id, Func<string, object[]> events)
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.For(id), () => events(id), typeof(TAggregate));
    }

    /// <inheritdoc/>
    public IGivenStage GivenStream<TId>(TId id, Func<TId, object[]> events)
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.ForTyped(id), () => events(id), null);
    }

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IGivenStage.GivenStream<TAggregate, TId>.")]
    public IGivenStage GivenStream<TAggregate, TId>(TId id, Func<TId, object[]> events)
        where TAggregate : class
    {
        ArgumentNullException.ThrowIfNull(events);
        return Given(StreamKey.ForTyped(id), () => events(id), typeof(TAggregate));
    }

    /// <inheritdoc/>
    public ICommandStage When(Func<object> command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _steps.Add(new WhenCommandStep(command));
        return this;
    }

    /// <inheritdoc/>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "Implements IGivenStage.WhenQuery<TResponse>.")]
    public IQueryStage<TResponse> WhenQuery<TResponse>(Func<object> query)
    {
        ArgumentNullException.ThrowIfNull(query);
        _steps.Add(new WhenQueryStep<TResponse>(query));
        return new QueryStage<TResponse>(this);
    }

    /// <inheritdoc/>
    public IHttpStage WhenHttp(Action<Alba.Scenario> request)
    {
        ArgumentNullException.ThrowIfNull(request);
        _steps.Add(new WhenHttpStep(request));
        return new HttpStage(this);
    }

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasNoEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamHasNoEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.ForTyped(id), StreamScope.Whole, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenStreamEventCount(Guid id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public ICommandStage ThenStreamEventCount(string id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public ICommandStage ThenStreamEventCount<TId>(TId id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.ForTyped(id), StreamScope.Whole, expected));

    /// <inheritdoc/>
    public ICommandStage ThenThrows<TException>(Func<TException, bool>? predicate = null)
        where TException : Exception =>
        Add(new ThrowsAssertionStep<TException>(predicate));

    /// <inheritdoc/>
    public ICommandStage ThenAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.Has(StreamKey.ForTyped(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenNotAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenNotAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.For(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenNotAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null) =>
        Add(StreamAssertionStep.HasNo(StreamKey.ForTyped(id), StreamScope.Appended, predicate));

    /// <inheritdoc/>
    public ICommandStage ThenAppendedCount(Guid id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public ICommandStage ThenAppendedCount(string id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.For(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public ICommandStage ThenAppendedCount<TId>(TId id, int expected) =>
        Add(StreamAssertionStep.ForCount(StreamKey.ForTyped(id), StreamScope.Appended, expected));

    /// <inheritdoc/>
    public ICommandStage ThenPublished<TMessage>(Func<TMessage, bool>? predicate = null) =>
        Add(new PublishedAssertionStep<TMessage>(predicate));

    /// <inheritdoc/>
    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        LastContext = await ScenarioRunner.RunAsync(_steps, _host, _options, _tenantId, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public TaskAwaiter GetAwaiter() => RunAsync().GetAwaiter();

    /// <summary>
    /// Records a step for a query stage.
    /// </summary>
    /// <param name="step">The step.</param>
    internal void AddStep(IScenarioStep step) => _steps.Add(step);

    private ScenarioBuilder Given(StreamKey key, Func<object[]> events, Type? aggregateType)
    {
        _steps.Add(new GivenStreamStep(key, events, aggregateType));
        return this;
    }

    private ScenarioBuilder Add(IScenarioStep step)
    {
        _steps.Add(step);
        return this;
    }
}
