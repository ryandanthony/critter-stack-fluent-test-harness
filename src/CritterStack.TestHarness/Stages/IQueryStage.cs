// -----------------------------------------------------------------------
// <copyright file="IQueryStage.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// The stage after <c>WhenQuery&lt;TResponse&gt;(query)</c>. Adds a typed assertion on the query's response.
/// </summary>
/// <typeparam name="TResponse">The query's response type.</typeparam>
public interface IQueryStage<TResponse> : IThenStage<IQueryStage<TResponse>>
{
    /// <summary>
    /// Asserts on the response of the most recent <c>WhenQuery</c>. If the query threw, this fails with
    /// "query threw <c>&lt;type&gt;</c>, no result" rather than passing a null response to the predicate.
    /// </summary>
    /// <param name="predicate">The check. Receives <c>null</c> if the handler returned <c>null</c>.</param>
    /// <returns>The query stage.</returns>
    IQueryStage<TResponse> ThenResult(Func<TResponse, bool> predicate);
}
