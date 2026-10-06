// -----------------------------------------------------------------------
// <copyright file="PoisonDaemonCollection.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Tests that share the daemon host with a failing projection.
/// </summary>
[CollectionDefinition(nameof(PoisonDaemonCollection))]
public sealed class PoisonDaemonCollection : ICollectionFixture<PoisonDaemonHostFixture>
{
}
