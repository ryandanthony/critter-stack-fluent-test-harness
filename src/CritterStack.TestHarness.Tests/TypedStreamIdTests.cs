// -----------------------------------------------------------------------
// <copyright file="TypedStreamIdTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Strongly typed stream ids that wrap a Guid, on the Guid host: the Parcel domain uses <see cref="ParcelId"/> on its
/// aggregate, events, Wolverine aggregate handler and async read model (PDR-0013). Also the id-kind check and the
/// typed-id argument rules.
/// </summary>
/// <param name="fixture">The host fixture.</param>
[Collection(nameof(NoDaemonCollection))]
public sealed class TypedStreamIdTests(NoDaemonHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    [Trait("business_value", "A service with typed ids writes Givens and asserts appended events with its own id type, through Wolverine's aggregate handler workflow.")]
    public async Task Typed_id_works_with_an_aggregate_handler()
    {
        var id = new ParcelId(Guid.NewGuid());

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Parcel, ParcelId>(id, p => [new ParcelCreated(p, "box")])
            .When(() => new ShipParcel(id))
            .ThenAppended<ParcelShipped, ParcelId>(id, e => e.Id == id)
            .ThenNotAppended<ParcelCreated, ParcelId>(id)
            .ThenAppendedCount(id, 1)
            .ThenStreamHasEvent<ParcelCreated, ParcelId>(id, e => e.Id == id)
            .ThenStreamHasNoEvent<ParcelShipped, ParcelId>(id, e => e.Id != id)
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An async read model keyed by a typed id is caught up before a query, and the query sees the typed id.")]
    public async Task Async_read_model_keyed_by_a_typed_id_is_current_before_a_query()
    {
        var id = new ParcelId(Guid.NewGuid());

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Parcel, ParcelId>(id, p => [new ParcelCreated(p, "box")])
            .When(() => new ShipParcel(id))
            .WhenQuery<ParcelSummary?>(() => new FindParcel(id))
            .ThenResult(s => s is not null && s.Id == id && s.Shipped)
            .ThenStreamHasEvent<ParcelShipped, ParcelId>(id)
            .ThenStreamHasNoEvent<ParcelCreated, ParcelId>(id, e => Is(e.Label, "crate"))
            .ThenStreamEventCount(id, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A typed id and the Guid it wraps name the same stream, so tests can mix them.")]
    public async Task Typed_id_and_its_guid_name_the_same_stream()
    {
        var id = new ParcelId(Guid.NewGuid());

        await TestHarness.Setup(fixture.Host)
            .GivenStream(id, p => [new ParcelCreated(p, "box")])
            .When(() => new ShipParcel(id))
            .ThenAppendedCount(id.Value, 1)
            .ThenAppended<ParcelShipped>(id.Value)
            .ThenStreamEventCount(id.Value, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A failure on a typed-id stream names the id type, and the event JSON shows the typed id as stored.")]
    public async Task Failure_message_shows_the_typed_id()
    {
        var id = new ParcelId(Guid.NewGuid());

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Parcel, ParcelId>(id, p => [new ParcelCreated(p, "box")])
            .When(() => new ShipParcel(id))
            .ThenAppendedCount(id, 2));

        Assert.StartsWith($"step 3 ThenAppendedCount(ParcelId({id.Value}), 2): expected 2 event(s) appended by step 2 When(ShipParcel), found 1", failure.Message, StringComparison.Ordinal);
        Assert.Contains($"v2 ParcelShipped {{\"Id\":{{\"Value\":\"{id.Value}\"}}}}", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "A string id on a Guid store fails before anything is written, naming the id kind the store wants.")]
    public async Task String_id_on_a_guid_store_fails_before_any_step()
    {
        var id = Guid.NewGuid();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
            .GivenStream("order-17", k => [new WidgetCreated(id, k)])
            .When(() => new RenameWidget(id, "Gear")));

        Assert.Equal(
            "step 2 GivenStream(\"order-17\"): the stream id \"order-17\" is a string, but the store uses Guid stream ids (StreamIdentity.AsGuid). Pass a Guid, or a typed id that wraps a Guid.",
            failure.Message);

        await TestHarness.Setup(fixture.Host)
            .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
            .ThenStreamEventCount(id, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A typed id that wraps a string is caught on a Guid store too, so the mistake can't hide behind the wrapper.")]
    public async Task Typed_string_id_on_a_guid_store_fails_before_any_step()
    {
        var code = new GadgetCode("gadget-1");

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .When(() => new RenameWidget(Guid.NewGuid(), "Gear"))
            .ThenAppendedCount(code, 1));

        Assert.StartsWith("step 2 ThenAppendedCount(GadgetCode(\"gadget-1\"), 1): the stream id GadgetCode(\"gadget-1\") is a string", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "An id type that isn't a single Guid or string wrapper fails when the scenario is built, at the line with the mistake.")]
    public void Unsupported_id_types_fail_when_the_scenario_is_built()
    {
        var start = TestHarness.Setup(fixture.Host);

        var notAWrapper = Assert.Throws<ArgumentException>(() => start.GivenStream(new TwoPartId(Guid.NewGuid(), 1), _ => []));
        Assert.Equal("id", notAWrapper.ParamName);
        Assert.StartsWith("TwoPartId can't be used as a stream id", notAWrapper.Message, StringComparison.Ordinal);

        var wrapsAnInt = Assert.Throws<ArgumentException>(() => start.GivenStream(new NumberId(7), _ => []));
        Assert.Equal("id", wrapsAnInt.ParamName);
        Assert.StartsWith("NumberId wraps Int32, but Marten stream ids are a Guid or a string.", wrapsAnInt.Message, StringComparison.Ordinal);

        var primitive = Assert.Throws<ArgumentException>(() => start.GivenStream(7, _ => []));
        Assert.StartsWith("Int32 can't be used as a stream id", primitive.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Blank and null ids fail when the scenario is built, instead of writing to an empty-named stream.")]
    public void Blank_and_null_ids_fail_when_the_scenario_is_built()
    {
        var start = TestHarness.Setup(fixture.Host);

        Assert.Equal("id", Assert.ThrowsAny<ArgumentException>(() => start.GivenStream(" ", _ => [])).ParamName);
        Assert.Equal("id", Assert.ThrowsAny<ArgumentException>(() => start.GivenStream((string)null!, _ => [])).ParamName);
        Assert.Equal("id", Assert.Throws<ArgumentException>(() => start.GivenStream(new GadgetCode(string.Empty), _ => [])).ParamName);
        Assert.Equal("id", Assert.Throws<ArgumentNullException>(() => start.GivenStream((ClassId)null!, _ => [])).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream("k", null!)).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream<Widget>("k", null!)).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream(new ParcelId(Guid.NewGuid()), null!)).ParamName);
        Assert.Equal("events", Assert.Throws<ArgumentNullException>(() => start.GivenStream<Parcel, ParcelId>(new ParcelId(Guid.NewGuid()), null!)).ParamName);
    }

    [Fact]
    [Trait("business_value", "An id type whose value getter throws fails when the scenario is built and keeps the getter's exception.")]
    public void Typed_id_whose_getter_throws_fails_when_the_scenario_is_built()
    {
        var start = TestHarness.Setup(fixture.Host);

        var failure = Assert.Throws<ArgumentException>(() => start.GivenStream(new ThrowingId(Guid.NewGuid()), _ => []));

        Assert.Equal("id", failure.ParamName);
        Assert.StartsWith("reading ThrowingId.Value threw InvalidOperationException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<InvalidOperationException>(failure.InnerException);
    }

    /// <summary>Not a single-value wrapper.</summary>
    /// <param name="Value">The Guid part.</param>
    /// <param name="Part">The other part.</param>
    private sealed record TwoPartId(Guid Value, int Part);

    /// <summary>Wraps a value Marten can't use as a stream id.</summary>
    /// <param name="Value">The number.</param>
    private readonly record struct NumberId(int Value);

    /// <summary>A reference-type wrapper, so null can be passed.</summary>
    /// <param name="Value">The Guid.</param>
    private sealed record ClassId(Guid Value);

    /// <summary>A valid wrapper shape whose getter throws.</summary>
    /// <param name="value">Ignored.</param>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S1144:Unused private types or members should be removed", Justification = "The harness reads Value through Marten's value type rules.")]
    private sealed class ThrowingId(Guid value)
    {
        /// <summary>Gets nothing: always throws.</summary>
        /// <value>Never returns.</value>
        public Guid Value => throw new InvalidOperationException($"no value for {value}");
    }
}
