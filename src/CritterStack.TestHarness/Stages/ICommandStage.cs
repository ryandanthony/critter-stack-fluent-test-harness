// -----------------------------------------------------------------------
// <copyright file="ICommandStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// The stage after <c>When(command)</c>. Adds assertions on the events the command appended and on the messages
/// it published. Every assertion here refers to the most recent <c>When</c>. Stream assertions come in
/// <see cref="Guid"/>, <see cref="string"/> and strongly typed id forms; see <see cref="IGivenStage"/>.
/// </summary>
public interface ICommandStage : IThenStage<ICommandStage>
{
    /// <summary>
    /// Asserts that the most recent <c>When</c> appended at least one <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/> to the stream. Events from Givens and earlier commands are ignored.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended at least one matching <typeparamref name="TEvent"/> to the
    /// string-keyed stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended at least one matching <typeparamref name="TEvent"/> to the
    /// stream named by a strongly typed id.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type matches.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended no <typeparamref name="TEvent"/> matching
    /// <paramref name="predicate"/> to the stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenNotAppended<TEvent>(Guid id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended no matching <typeparamref name="TEvent"/> to the
    /// string-keyed stream.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <param name="id">The stream key.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenNotAppended<TEvent>(string id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended no matching <typeparamref name="TEvent"/> to the stream named
    /// by a strongly typed id.
    /// </summary>
    /// <typeparam name="TEvent">The event type.</typeparam>
    /// <typeparam name="TId">The id type: a single public, gettable property wrapping a Guid or a string.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="predicate">Optional filter. When null, any event of the type counts.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenNotAppended<TEvent, TId>(TId id, Func<TEvent, bool>? predicate = null);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended exactly <paramref name="expected"/> events to the stream.
    /// </summary>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppendedCount(Guid id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended exactly <paramref name="expected"/> events to the
    /// string-keyed stream.
    /// </summary>
    /// <param name="id">The stream key.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppendedCount(string id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>When</c> appended exactly <paramref name="expected"/> events to the stream
    /// named by a strongly typed id.
    /// </summary>
    /// <typeparam name="TId">The id type, inferred from <paramref name="id"/>.</typeparam>
    /// <param name="id">The stream id.</param>
    /// <param name="expected">The expected number of appended events.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenAppendedCount<TId>(TId id, int expected);

    /// <summary>
    /// Asserts that the most recent <c>When</c> published or sent at least one <typeparamref name="TMessage"/>
    /// matching <paramref name="predicate"/>, as recorded by Wolverine's tracked session.
    /// </summary>
    /// <typeparam name="TMessage">The message type.</typeparam>
    /// <param name="predicate">Optional filter. When null, any message of the type matches.</param>
    /// <returns>The command stage.</returns>
    ICommandStage ThenPublished<TMessage>(Func<TMessage, bool>? predicate = null);
}
