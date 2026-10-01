// -----------------------------------------------------------------------
// <copyright file="WidgetServiceFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using Alba;
using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events.Projections;
using Marten;
using Microsoft.AspNetCore.Builder;
using Wolverine;
using Wolverine.Marten;

namespace CritterStack.TestHarness.Tests.DocSamples;

// Sample: docs/tutorials/first-scenario.md, step 2 (the fixture).

/// <summary>
/// The tutorial's fixture: a small service host built with Alba, against the shared Postgres container.
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class WidgetServiceFixture(PostgresContainerFixture postgres) : IAsyncLifetime
{
    /// <summary>Gets the running host.</summary>
    /// <value>The host.</value>
    public IAlbaHost Host { get; private set; } = null!;

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddMarten(options =>
            {
                options.Connection(postgres.ConnectionString);
                options.DatabaseSchemaName = "tutorial";
                options.Projections.Add<WidgetSummaryProjection>(ProjectionLifecycle.Async);
            })
            .IntegrateWithWolverine(integration => integration.MessageStorageSchemaName = "tutorial_wolverine");

        builder.Services.DisableAllExternalWolverineTransports();
        builder.Host.UseWolverine(options =>
        {
            options.Policies.AutoApplyTransactions();
            options.Discovery.IncludeType(typeof(WidgetHandlers));
        });

        Host = await AlbaHost.For(builder, _ => { });
    }

    /// <inheritdoc/>
    public async ValueTask DisposeAsync() => await Host.DisposeAsync();
}
