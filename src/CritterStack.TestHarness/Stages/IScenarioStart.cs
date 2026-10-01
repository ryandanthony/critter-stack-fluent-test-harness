// -----------------------------------------------------------------------
// <copyright file="IScenarioStart.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness;

/// <summary>
/// The stage returned by <see cref="TestHarness.Setup"/>. It offers everything <see cref="IGivenStage"/> does,
/// plus <see cref="ForTenant"/>, which is only available here so a scenario has exactly one tenant.
/// </summary>
public interface IScenarioStart : IGivenStage
{
    /// <summary>
    /// Runs every step of the scenario as <paramref name="tenantId"/>: Givens and Then reads use a session for
    /// that tenant, <c>When</c> uses <c>InvokeForTenantAsync</c> and <c>WhenQuery</c> uses
    /// <c>InvokeForTenantAsync&lt;T&gt;</c>. Without this call the scenario uses Marten's default tenant.
    /// </summary>
    /// <param name="tenantId">The tenant id. Must not be null or white space.</param>
    /// <returns>The Given stage. <c>ForTenant</c> can't be called again.</returns>
    IGivenStage ForTenant(string tenantId);
}
