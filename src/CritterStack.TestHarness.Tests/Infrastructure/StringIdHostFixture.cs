// -----------------------------------------------------------------------
// <copyright file="StringIdHostFixture.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.TestDomain;
using JasperFx.Events;
using JasperFx.Events.Projections;
using Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// A host whose event store keys streams by string (<c>StreamIdentity.AsString</c>), with no async daemon. It runs
/// the Gadget domain, whose streams are string keys or a strongly typed id that wraps a string (ADR-0019).
/// </summary>
/// <param name="postgres">The shared container.</param>
public sealed class StringIdHostFixture(PostgresContainerFixture postgres) : HarnessHostFixture(postgres)
{
    /// <inheritdoc/>
    protected override string SchemaName => "harness_stringids";

    /// <inheritdoc/>
    protected override bool RunDaemon => false;

    /// <inheritdoc/>
    protected override IReadOnlyList<Type> HandlerTypes => [typeof(GadgetHandlers)];

    /// <inheritdoc/>
    protected override string EndpointSuffix => nameof(GadgetEndpoints);

    /// <inheritdoc/>
    protected override void ConfigureStreams(StoreOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        options.Events.StreamIdentity = StreamIdentity.AsString;
        options.Projections.Add<GadgetSummaryProjection>(ProjectionLifecycle.Async);
    }
}
