// -----------------------------------------------------------------------
// <copyright file="WidgetEvents.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0048 // Small related records are kept together in the test domain.
#pragma warning disable SA1402 // Small related records are kept together in the test domain.
#pragma warning disable SA1649 // Small related records are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>A widget was created.</summary>
/// <param name="Id">The widget id.</param>
/// <param name="Name">The widget name.</param>
public sealed record WidgetCreated(Guid Id, string Name);

/// <summary>A widget was renamed.</summary>
/// <param name="Id">The widget id.</param>
/// <param name="Name">The new name.</param>
public sealed record WidgetRenamed(Guid Id, string Name);

/// <summary>A widget was published.</summary>
/// <param name="Id">The widget id.</param>
public sealed record WidgetPublished(Guid Id);

/// <summary>A widget was archived.</summary>
/// <param name="Id">The widget id.</param>
public sealed record WidgetArchived(Guid Id);

/// <summary>Records which tenant the handler's session ran as.</summary>
/// <param name="Id">The widget id.</param>
/// <param name="TenantId">The session's tenant id.</param>
public sealed record TenantRecorded(Guid Id, string TenantId);

/// <summary>Makes <see cref="PoisonProjection"/> throw.</summary>
/// <param name="Id">The widget id.</param>
public sealed record WidgetPoisoned(Guid Id);
