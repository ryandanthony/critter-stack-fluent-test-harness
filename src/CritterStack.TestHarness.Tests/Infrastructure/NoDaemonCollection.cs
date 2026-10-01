// -----------------------------------------------------------------------
// <copyright file="NoDaemonCollection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Tests that share the host with no async daemon.
/// </summary>
[CollectionDefinition(nameof(NoDaemonCollection))]
public sealed class NoDaemonCollection : ICollectionFixture<NoDaemonHostFixture>
{
}
