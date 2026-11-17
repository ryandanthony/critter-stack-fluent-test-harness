// -----------------------------------------------------------------------
// <copyright file="IThenStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace CritterStack.TestHarness;

/// <summary>
/// Assertions valid after any <c>When</c> or <c>WhenQuery</c>. Stream assertions here read the whole stream,
/// including events from Givens and earlier commands.
/// </summary>
/// <typeparam name="TSelf">The concrete stage, so chained calls keep the stage-specific methods.</typeparam>
public interface IThenStage<out TSelf> : IScenario
    where TSelf : IThenStage<TSelf>
{
    /// <summary>
    /// Asserts that the whole stream contains at least one <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole string-keyed stream contains at least one <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole stream named by a strongly typed id contains at least one <typeparamref name="TEvent"/>
    /// matching <paramref name="predicate"/>. See <see cref="IGivenStage.GivenStream{TId}(TId, Func{TId, object[]})"/> for how the id is unwrapped.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole stream contains no <typeparamref name="TEvent"/> matching <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasNoEvent<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole string-keyed stream contains no <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasNoEvent<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole stream named by a strongly typed id contains no <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/>.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamHasNoEvent<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the whole stream has exactly <paramref name="expected"/> events.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected event count. Zero matches a stream that doesn't exist.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamEventCount(Guid id, int expected);

    /// <summary>
    /// Asserts that the whole string-keyed stream has exactly <paramref name="expected"/> events.
    /// </summary>
    /// <param name="id">The stream key.</param>
    /// <param name="expected">The expected event count. Zero matches a stream that doesn't exist.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamEventCount(string id, int expected);

    /// <summary>
    /// Asserts that the whole stream named by a strongly typed id has exactly <paramref name="expected"/> events.
    /// </summary>
    /// <typeparam name="TId">The id type, inferred from <paramref name="id"/>.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected event count. Zero matches a stream that doesn't exist.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenStreamEventCount<TId>(TId id, int expected);

    /// <summary>
    /// Claims the exception thrown by the handler of the most recent <c>When</c> or <c>WhenQuery</c>. Put it first
    /// after the step: any other Then that runs while an exception is unclaimed fails with the exception instead.
    /// </summary>
    /// <typeparam name="TException">The expected exception type, or a base type of it. For a cascaded handler's
    /// <see cref="AggregateException"/>, any inner exception of this type matches.</typeparam>
    /// <param name="predicate">Optional check against the matching exception.</param>
    /// <returns>The same stage.</returns>
    TSelf ThenThrows<TException>(Func<TException, bool>? predicate = null)
        where TException : Exception;

    /// <summary>
    /// Invokes another command. See <see cref="IGivenStage.When"/>.
    /// </summary>
    /// <param name="command">Builds the command. Called when the step runs.</param>
    /// <returns>The command stage, whose Thens refer to this command.</returns>
    [SuppressMessage("Naming", "CA1716:Identifiers should not match keywords", Justification = "When is the Given/When/Then vocabulary of the harness, which is a C#-only test library.")]
    ICommandStage When(Func<object> command);

    /// <summary>
    /// Invokes another query. See <see cref="IGivenStage.WhenQuery{TResponse}"/>.
    /// </summary>
    /// <typeparam name="TResponse">The response type the query handler returns.</typeparam>
    /// <param name="query">Builds the query. Called when the step runs.</param>
    /// <returns>The query stage, whose Thens refer to this query.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The response type is chosen by the caller; the query is untyped by design so it can't be inferred.")]
    IQueryStage<TResponse> WhenQuery<TResponse>(Func<object> query);

    /// <summary>
    /// Sends another HTTP request. See <see cref="IGivenStage.WhenHttp"/>.
    /// </summary>
    /// <param name="request">Configures the Alba request. Called when the step runs.</param>
    /// <returns>The HTTP stage, whose Thens refer to this request.</returns>
    IHttpStage WhenHttp(Action<Alba.Scenario> request);
}
