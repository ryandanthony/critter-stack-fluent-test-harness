// -----------------------------------------------------------------------
// <copyright file="StringIdCollection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Tests that share the host whose event store keys streams by string.
/// </summary>
[CollectionDefinition(nameof(StringIdCollection))]
public sealed class StringIdCollection : ICollectionFixture<StringIdHostFixture>
{
}
