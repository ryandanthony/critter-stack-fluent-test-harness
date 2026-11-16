// -----------------------------------------------------------------------
// <copyright file="WidgetReceipt.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Tests.TestDomain;

/// <summary>The JSON body an HTTP endpoint answers with.</summary>
/// <param name="Id">The widget id.</param>
/// <param name="Status">What happened.</param>
public sealed record WidgetReceipt(Guid Id, string Status);
