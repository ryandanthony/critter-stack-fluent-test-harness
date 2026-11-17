// -----------------------------------------------------------------------
// <copyright file="StreamSnapshots.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

namespace CritterStack.TestHarness.Internal.Steps;

/// <summary>
/// Records stream versions just before an action, so appended-event assertions can read only what it appended.
/// </summary>
internal static class StreamSnapshots
{
    /// <summary>
    /// Reads and stores the current version of each stream (0 for a stream that doesn't exist yet).
    /// </summary>
    /// <param name="context">The scenario context.</param>
    /// <param name="ids">The stream ids to snapshot.</param>
    /// <param name="cancellationToken">Cancels the reads.</param>
    /// <returns>A task that completes when every snapshot is stored.</returns>
    internal static async Task TakeAsync(ScenarioContext context, IReadOnlyCollection<StreamKey> ids, CancellationToken cancellationToken)
    {
        if (ids.Count == 0)
        {
            return;
        }

        await using var session = context.OpenQuerySession();
        foreach (var id in ids)
        {
            var state = await id.FetchStateAsync(session, cancellationToken).ConfigureAwait(false);
            context.Snapshots[id] = state?.Version ?? 0;
        }
    }
}
