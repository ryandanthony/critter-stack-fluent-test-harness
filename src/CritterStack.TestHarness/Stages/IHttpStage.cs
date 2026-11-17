// -----------------------------------------------------------------------
// <copyright file="IHttpStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

namespace CritterStack.TestHarness;

/// <summary>
/// The stage after <c>WhenHttp(request)</c>. Adds assertions on the HTTP response, on the events the request
/// appended and on the messages it published. Every assertion here refers to the most recent <c>WhenHttp</c>.
/// </summary>
public interface IHttpStage : IThenStage<IHttpStage>
{
    /// <summary>
    /// Asserts the status code of the most recent <c>WhenHttp</c>'s response. Without a <c>ThenStatusCode</c>,
    /// <c>WhenHttp</c> itself requires a 2xx status; with one, any status can be expected.
    /// </summary>
    /// <param name="expected">The expected status code, for example 201 or 400.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenStatusCode(int expected);

    /// <summary>
    /// Reads the response body of the most recent <c>WhenHttp</c> as JSON, using the host's HTTP serializer, and
    /// asserts on it. Fails if the request threw, the body is empty or the body isn't a <typeparamref name="TBody"/>.
    /// </summary>
    /// <typeparam name="TBody">The type to read the JSON body as.</typeparam>
    /// <param name="predicate">The check.</param>
    /// <returns>The HTTP stage.</returns>
    [SuppressMessage("Major Code Smell", "S4018:Generic methods should provide type parameters", Justification = "The body type is chosen by the caller and only appears in the predicate.")]
    IHttpStage ThenResponse<TBody>(Func<TBody, bool> predicate);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended at least one <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/> to the stream. Events from Givens and earlier actions are ignored.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended at least one matching <typeparamref name="TEvent"/> to
    /// the string-keyed stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended at least one matching <typeparamref name="TEvent"/> to
    /// the stream named by a strongly typed id.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended no <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/> to the stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenNotAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended no matching <typeparamref name="TEvent"/> to the
    /// string-keyed stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenNotAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended no matching <typeparamref name="TEvent"/> to the stream
    /// named by a strongly typed id.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenNotAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended exactly <paramref name="expected"/> events to the stream.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppendedCount(Guid id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended exactly <paramref name="expected"/> events to the
    /// string-keyed stream.
    /// </summary>
    /// <param name="id">The stream key.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppendedCount(string id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> appended exactly <paramref name="expected"/> events to the stream
    /// named by a strongly typed id.
    /// </summary>
    /// <typeparam name="TId">The id type, inferred from <paramref name="id"/>.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenAppendedCount<TId>(TId id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>WhenHttp</c> published or sent at least one <typeparamref name="TMessage"/>
    /// matching <paramref name="predicate"/>, as recorded by Wolverine's tracked session around the request.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="predicate">Optional filter. When null, any message of the type matches.</param>
    /// <returns>The HTTP stage.</returns>
    IHttpStage ThenPublished<TMessage>(Func<TMessage, bool>? predicate = null);
}
