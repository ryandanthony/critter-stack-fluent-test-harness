// -----------------------------------------------------------------------
// <copyright file="LedgerStores.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using JasperFx.MultiTenancy;
using Marten;

namespace CritterStack.TestHarness.Tests.Infrastructure;

/// <summary>
/// Shared configuration for the ancillary stores the test hosts add (ADR-0020): conjoined tenancy like the main store,
/// and a schema of their own so no two stores or hosts share tables (ADR-0003).
/// </summary>
internal static class LedgerStores
{
    /// <summary>
    /// Points an ancillary store at the shared container in its own schema, with conjoined tenancy.
    /// </summary>
    /// <param name="options">The store options.</param>
    /// <param name="connectionString">The container's connection string.</param>
    /// <param name="schema">The schema for documents and events.</param>
    internal static void Configure(StoreOptions options, string connectionString, string schema)
    {
        options.Connection(connectionString);
        options.DatabaseSchemaName = schema;
        options.Events.DatabaseSchemaName = schema;
        options.Events.TenancyStyle = TenancyStyle.Conjoined;
        options.Policies.AllDocumentsAreMultiTenanted();
    }
}
