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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Wolverine;
using Wolverine.Http;
using Wolverine.Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Builds an Alba host with Marten (conjoined tenancy, the test projections) and Wolverine (local only) in its own
/// schema of the shared Postgres container.
/// </summary>
public abstract class HarnessHostFixture : IAsyncLifetime
{
    /// <summary>
    /// Gets the request header Wolverine.Http reads the tenant id from on the test host.
    /// </summary>
    /// <value>The header name.</value>
    public static string TenantHeader => "tenant";

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

    /// <summary>
    /// Adds host-specific Marten configuration, such as an extra projection, after the shared setup.
    /// </summary>
    /// <param name="options">The Marten options.</param>
    protected virtual void ConfigureMarten(StoreOptions options)
    {
    }

    /// <summary>
    /// Sets the event store's stream identity and registers the host's projections. The default is Guid streams with
    /// the Widget projections; the string-id host replaces both.
    /// </summary>
    /// <param name="options">The Marten options.</param>
    protected virtual void ConfigureStreams(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Projections.Add<WidgetDetailsProjection>(ProjectionLifecycle.Inline);
        options.Projections.Add<WidgetSummaryProjection>(ProjectionLifecycle.Async);
    }

    /// <summary>
    /// Gets the handler classes Wolverine discovers on this host.
    /// </summary>
    /// <value>The handler types.</value>
    protected virtual IReadOnlyList<Type> HandlerTypes => [typeof(WidgetHandlers)];

    /// <summary>
    /// Gets the class name suffix of the Wolverine.Http endpoint classes this host maps.
    /// </summary>
    /// <value>The suffix, such as <c>WidgetEndpoints</c>.</value>
    protected virtual string EndpointSuffix => nameof(WidgetEndpoints);

    /// <summary>
    /// Gets the shared container's connection string, for ancillary stores a host adds.
    /// </summary>
    /// <value>The connection string.</value>
    protected string ConnectionString => _postgres.ConnectionString;

    /// <summary>
    /// Adds host-specific services after Marten and Wolverine, such as ancillary Marten stores.
    /// </summary>
    /// <param name="services">The host's services.</param>
    protected virtual void ConfigureServices(IServiceCollection services)
    {
    }

    /// <inheritdoc/>
    public async ValueTask InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.Logging.ClearProviders();

        // Validate scopes in every environment, not just Development, so resolving a scoped service (such as
        // IMessageBus) from the root provider fails here the same way it does under an IDE's test runner.
        builder.Host.UseDefaultServiceProvider(o => o.ValidateScopes = true);

        AddMarten(builder.Services);
        AddWolverine(builder);
        ConfigureServices(builder.Services);

        // No ConfigureAwait: Alba 8 returns a Task, Alba 9 an awaitable AlbaHostBuilder, and this must build on both.
        _host = await AlbaHost.For(builder, MapEndpoints);
    }

    private void AddWolverine(WebApplicationBuilder builder)
    {
        builder.Services.DisableAllExternalWolverineTransports();
        builder.Services.AddWolverineHttp();
        builder.Host.UseWolverine(options =>
        {
            options.Policies.AutoApplyTransactions();
            foreach (var type in HandlerTypes)
            {
                options.Discovery.IncludeType(type);
            }
        });
    }

    private void MapEndpoints(WebApplication app) =>
        app.MapWolverineEndpoints(http =>
        {
            http.CustomizeHttpEndpointDiscovery(q => q.Includes.WithNameSuffix(EndpointSuffix));
            http.TenantId.IsRequestHeaderValue(TenantHeader);
        });

    private void AddMarten(IServiceCollection services)
    {
        var marten = services.AddMarten(options =>
            {
                options.Connection(_postgres.ConnectionString);
                options.DatabaseSchemaName = SchemaName;
                options.Events.DatabaseSchemaName = SchemaName;
                options.Events.TenancyStyle = TenancyStyle.Conjoined;
                options.Policies.AllDocumentsAreMultiTenanted();
                ConfigureStreams(options);
                ConfigureMarten(options);
            })
            .IntegrateWithWolverine(integration => integration.MessageStorageSchemaName = SchemaName + "_wolverine");

        if (RunDaemon)
        {
            marten.AddAsyncDaemon(JasperFx.Events.Daemon.DaemonMode.Solo);
        }
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
