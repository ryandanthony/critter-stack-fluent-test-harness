// -----------------------------------------------------------------------
// <copyright file="IStreamStep.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// A step that reads or writes one stream, so the runner can check its id against its store's stream identity before
/// anything runs, and add its store to the scenario's sync set.
/// </summary>
internal interface IStreamStep : IScenarioStep
{
    /// <summary>
    /// Gets the stream the step reads or writes, and the store it lives in.
    /// </summary>
    /// <value>The stream address.</value>
    StreamAddress Stream { get; }
}
