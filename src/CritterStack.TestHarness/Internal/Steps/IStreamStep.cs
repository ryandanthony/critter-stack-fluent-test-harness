// -----------------------------------------------------------------------
// <copyright file="IStreamStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// A step that reads or writes one stream, so the runner can check its id against the store's stream identity
/// before anything runs.
/// </summary>
internal interface IStreamStep : IScenarioStep
{
    /// <summary>
    /// Gets the stream the step reads or writes.
    /// </summary>
    /// <value>The stream key.</value>
    StreamKey StreamId { get; }
}
