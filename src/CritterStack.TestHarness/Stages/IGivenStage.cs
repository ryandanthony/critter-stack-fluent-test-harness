// -----------------------------------------------------------------------
// <copyright file="IGivenStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace CritterStack.TestHarness;

/// <summary>
/// Records the starting state of a scenario. Valid after <see cref="TestHarness.Setup"/> or another Given,
/// and before the first <c>When</c> or <c>WhenQuery</c>.
/// </summary>
public interface IGivenStage
{
    /// <summary>
    /// Writes events to the stream <paramref name="id"/> in its own <c>SaveChangesAsync</c>. The first Given for an
    /// id starts the stream; later Givens for the same id append to it.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="events">Builds the events from the stream id. Called when the step runs, not when it is recorded.</param>
    /// <returns>The Given stage.</returns>
    IGivenStage GivenStream(Guid id, Func<Guid, object[]> events);

    /// <summary>
    /// Writes events to the stream <paramref name="id"/>, starting it as an aggregate of type
    /// <typeparamref name="TAggregate"/> if this is the first Given for the id. On later Givens for the same id the
    /// events are appended and <typeparamref name="TAggregate"/> is ignored.
    /// </summary>
    /// <typeparam name="TAggregate">The stream's aggregate type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="events">Builds the events from the stream id. Called when the step runs, not when it is recorded.</param>
    /// <returns>The Given stage.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The aggregate type is chosen by the caller and has no value to infer it from.")]
    IGivenStage GivenStream<TAggregate>(Guid id, Func<Guid, object[]> events)
        where TAggregate : class;

    /// <summary>
    /// Invokes a command through Wolverine under message tracking, after running projection sync if anything was
    /// written since the last sync. A handler exception is captured for <c>ThenThrows</c>.
    /// </summary>
    /// <param name="command">Builds the command. Called when the step runs.</param>
    /// <returns>The command stage, whose Thens refer to this command.</returns>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "When is the Given/When/Then vocabulary of the harness, which is a C#-only test library.")]
    ICommandStage When(Func<object> command);

    /// <summary>
    /// Invokes a query through Wolverine, after running projection sync if anything was written since the last sync,
    /// and stores the response. A non-null response that isn't a <typeparamref name="TResponse"/> fails the scenario
    /// at once. A handler exception is captured for <c>ThenThrows</c>.
    /// </summary>
    /// <typeparam name="TResponse">The response type the query handler returns.</typeparam>
    /// <param name="query">Builds the query. Called when the step runs.</param>
    /// <returns>The query stage, whose Thens refer to this query.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The response type is chosen by the caller; the query is untyped by design so it can't be inferred.")]
    IQueryStage<TResponse> WhenQuery<TResponse>(Func<object> query);

    /// <summary>
    /// Sends an HTTP request through the host's Alba pipeline under Wolverine message tracking, after running
    /// projection sync if anything was written since the last sync. The response is kept for <c>ThenStatusCode</c> and
    /// <c>ThenResponse</c>, and the tracked session for <c>ThenPublished</c>. An exception thrown by the endpoint is
    /// captured for <c>ThenThrows</c>. Unless a <c>ThenStatusCode</c> follows, a non-2xx status fails at once.
    /// Requires the host to be an <c>IAlbaHost</c>.
    /// </summary>
    /// <param name="request">Configures the Alba request, for example <c>x =&gt; x.Post.Json(body).ToUrl("/orders")</c>.
    /// Called when the step runs. Assert the status with <c>ThenStatusCode</c>, not Alba's <c>StatusCodeShouldBe</c>.</param>
    /// <returns>The HTTP stage, whose Thens refer to this request.</returns>
    IHttpStage WhenHttp(Action<Alba.Scenario> request);
}
