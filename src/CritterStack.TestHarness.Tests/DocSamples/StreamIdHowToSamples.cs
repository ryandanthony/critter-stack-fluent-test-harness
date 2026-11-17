// -----------------------------------------------------------------------
// <copyright file="StreamIdHowToSamples.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

#pragma warning disable MA0006 // Samples use == on strings, as readers' own tests would.

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;

namespace CritterStack.TestHarness.Tests.DocSamples;

/// <summary>
/// Backs the string-key block in docs/how-to/use-string-and-typed-stream-ids.md, on the host whose store keys
/// streams by string. The typed-id block runs on the Guid host, in <see cref="HowToSamples"/>.
/// </summary>
/// <param name="fixture">The string-id host fixture.</param>
[Collection(nameof(StringIdCollection))]
public sealed class StreamIdHowToSamples(StringIdHostFixture fixture)
{
    // Sample: use-string-and-typed-stream-ids.md, "String-keyed streams".
    [Fact]
    [Trait("business_value", "The stream-id guide's string-key sample works as documented.")]
    public async Task UseStreamIds_string_keys()
    {
        var key = $"gadget-{Guid.NewGuid():N}";

        await TestHarness
            .Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppended<GadgetRenamed>(key, e => e.Name == "Gear")
            .ThenStreamEventCount(key, 2);
    }
}
