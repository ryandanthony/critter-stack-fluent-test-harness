// -----------------------------------------------------------------------
// <copyright file="PostgresContainerFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(PostgresContainerFixture))]

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// One Postgres container for the whole test assembly. Each host fixture uses its own schema in it.
/// </summary>
public sealed class PostgresContainerFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:17-alpine").Build();

    /// <summary>
    /// Gets the connection string for the running container.
    /// </summary>
    /// <value>The connection string.</value>
    public string ConnectionString => _container.GetConnectionString();

    /// <inheritdoc/>
    public async ValueTask InitializeAsync() => await _container.StartAsync().ConfigureAwait(false);

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await _container.DisposeAsync().ConfigureAwait(false);
}
