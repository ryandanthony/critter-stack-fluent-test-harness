// -----------------------------------------------------------------------
// <copyright file="DaemonCollection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Tests that share the host running an async daemon.
/// </summary>
[CollectionDefinition(nameof(DaemonCollection))]
public sealed class DaemonCollection : ICollectionFixture<DaemonHostFixture>
{
}
