// -----------------------------------------------------------------------
// <copyright file="WidgetMessages.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0048 // Small related records are kept together in the test domain.
#pragma warning disable SA1402 // Small related records are kept together in the test domain.
#pragma warning disable SA1649 // Small related records are kept together in the test domain.

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>Renames a widget.</summary>
/// <param name="Id">The widget id.</param>
/// <param name="Name">The new name.</param>
public sealed record RenameWidget(Guid Id, string Name);

/// <summary>Publishes a widget and cascades a <see cref="WidgetPublishedNotice"/>.</summary>
/// <param name="Id">The widget id.</param>
public sealed record PublishWidget(Guid Id);

/// <summary>Cascaded by <see cref="PublishWidget"/>.</summary>
/// <param name="Id">The widget id.</param>
public sealed record WidgetPublishedNotice(Guid Id);

/// <summary>Its handler always throws <see cref="WidgetException"/>.</summary>
/// <param name="Id">The widget id.</param>
public sealed record FailWidget(Guid Id);

/// <summary>Cascades a <see cref="FailingNotice"/>, whose handler throws.</summary>
/// <param name="Id">The widget id.</param>
public sealed record FailWidgetLater(Guid Id);

/// <summary>Cascaded by <see cref="FailWidgetLater"/>. Its handler throws <see cref="WidgetException"/>.</summary>
/// <param name="Id">The widget id.</param>
public sealed record FailingNotice(Guid Id);

/// <summary>Its handler throws a <see cref="TimeoutException"/> of its own.</summary>
/// <param name="Id">The widget id.</param>
public sealed record TimeOutWidget(Guid Id);

/// <summary>Its handler waits longer than a short test tracking timeout.</summary>
/// <param name="Delay">How long the handler waits.</param>
public sealed record StallWidget(TimeSpan Delay);

/// <summary>Its handler loads the async <see cref="WidgetSummary"/> and throws if it is missing.</summary>
/// <param name="Id">The widget id.</param>
public sealed record ArchiveWidget(Guid Id);

/// <summary>A command with no handler.</summary>
public sealed record UnhandledCommand;

/// <summary>Appends a <see cref="TenantRecorded"/> with the handler session's tenant id.</summary>
/// <param name="Id">The widget id.</param>
public sealed record RecordTenant(Guid Id);

/// <summary>Returns the async <see cref="WidgetSummary"/> for a widget.</summary>
/// <param name="Id">The widget id.</param>
public sealed record GetWidgetById(Guid Id);

/// <summary>Returns a <see cref="WidgetSummary"/> or null, never throws.</summary>
/// <param name="Id">The widget id.</param>
public sealed record FindWidget(Guid Id);

/// <summary>A query whose handler waits for the delay, honouring cancellation, then returns null.</summary>
/// <param name="Delay">How long the handler waits.</param>
public sealed record SlowFindWidget(TimeSpan Delay);
