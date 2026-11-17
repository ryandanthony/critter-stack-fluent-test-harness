// -----------------------------------------------------------------------
// <copyright file="StreamAddress.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal;

/// <summary>
/// A stream in a particular store: the same <see cref="StreamKey"/> in two stores is two streams (ADR-0020).
/// </summary>
/// <param name="Store">The store the stream lives in.</param>
/// <param name="Key">The stream id.</param>
internal sealed record StreamAddress(StoreRef Store, StreamKey Key)
{
    /// <summary>
    /// Gets the suffix a step label carries for a stream outside the main store.
    /// </summary>
    /// <value>Empty for the main store, otherwise for example <c> in ILedgerStore</c>.</value>
    internal string StoreSuffix => Store.IsMain ? string.Empty : $" in {Store.Name}";

    /// <inheritdoc/>
    public override string ToString() => Key.Label + StoreSuffix;
}
