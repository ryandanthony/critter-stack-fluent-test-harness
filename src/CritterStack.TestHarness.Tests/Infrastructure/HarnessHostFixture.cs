// -----------------------------------------------------------------------
// <copyright file="HarnessHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Alba;
using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Projections;
using JasperFx.MultiTenancy;
using Marten;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Builds an Alba host with Marten (conjoined tenancy, the test projections) and Wolverine (local only) in its own
/// schema of the shared Postgres container.
/// </summary>
public abstract class HarnessHostFixture : IAsyncLifetime
{
    private readonly PostgresContainerFixture _postgres;
    private IAlbaHost? _host;

    /// <summary>
    /// Initializes a new instance of the <see cref="HarnessHostFixture"/> class.
    /// </summary>
    /// <param name="postgres">The shared container.</param>
    protected HarnessHostFixture(PostgresContainerFixture postgres)
    {
        _postgres = postgres;
    }

    /// <summary>
    /// Gets the running host.
    /// </summary>
    /// <value>The host.</value>
    public IAlbaHost Host => _host ?? throw new InvalidOperationException("The host has not started.");

    /// <summary>
    /// Gets the schema this host's Marten and Wolverine tables live in.
    /// </summary>
    /// <value>The schema name.</value>
    protected abstract string SchemaName { get; }

    /// <summary>
    /// Gets a value indicating whether the host runs an async daemon.
    /// </summary>
    /// <value>True to register <c>AddAsyncDaemon(Solo)</c>.</value>
    protected abstract bool RunDaemon { get; }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        var marten = builder.Services.AddMarten(options =>
            {
                options.Connection(_postgres.ConnectionString);
                options.DatabaseSchemaName = SchemaName;
                options.Events.DatabaseSchemaName = SchemaName;
                options.Events.TenancyStyle = TenancyStyle.Conjoined;
                options.Policies.AllDocumentsAreMultiTenanted();
                options.Projections.Add<WidgetDetailsProjection>(ProjectionLifecycle.Inline);
                options.Projections.Add<WidgetSummaryProjection>(ProjectionLifecycle.Async);
            })
            .IntegrateWithWolverine(integration => integration.MessageStorageSchemaName = SchemaName + "_wolverine");

        if (RunDaemon)
        {
            marten.AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.Solo);
        }

        builder.Services.DisableAllExternalWolverineTransports();
        builder.Host.UseWolverine(options =>
        {
            options.Policies.AutoApplyTransactions();
            options.Discovery.IncludeType(typeof(WidgetHandlers));
        });

        _host = await AlbaHost.For(builder, _ => { }).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync()
    {
        if (_host is not null)
        {
            await _host.DisposeAsync().ConfigureAwait(false);
        }

        GC.SuppressFinalize(this);
    }
}
