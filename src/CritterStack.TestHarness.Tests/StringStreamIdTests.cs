// -----------------------------------------------------------------------
// <copyright file="StringStreamIdTests.cs" company="Ryan Anthony">
// Copyright (c) Ryan Anthony. All rights reserved.
// </copyright>
// -----------------------------------------------------------------------

using CritterStack.TestHarness.Tests.Infrastructure;
using CritterStack.TestHarness.Tests.TestDomain;
using Marten.Exceptions;
using static CritterStack.TestHarness.Tests.Scenarios;

namespace CritterStack.TestHarness.Tests;

/// <summary>
/// Scenarios on a store that keys streams by string (<c>StreamIdentity.AsString</c>), with plain string keys and a
/// strongly typed id that wraps a string (PDR-0013).
/// </summary>
/// <param name="fixture">The string-id host fixture.</param>
[Collection(nameof(StringIdCollection))]
public sealed class StringStreamIdTests(StringIdHostFixture fixture)
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static string NewKey() => $"gadget-{Guid.NewGuid():N}";

    [Fact]
    [Trait("business_value", "A string-keyed service can write Givens and assert what a command appended, with the same chain as Guid streams.")]
    public async Task String_keyed_stream_supports_givens_and_appended_assertions()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppended<GadgetRenamed>(key, e => Is(e.Name, "Gear"))
            .ThenNotAppended<GadgetCreated>(key)
            .ThenAppendedCount(key, 1)
            .ThenStreamHasEvent<GadgetCreated>(key)
            .ThenStreamHasNoEvent<GadgetRenamed>(key, e => Is(e.Name, "Cog"))
            .ThenStreamEventCount(key, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "The first Given for a string key starts the stream and later ones append, so history can be built up step by step.")]
    public async Task Second_given_for_a_string_key_appends()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream(key, k => [new GadgetCreated(k, "Sprocket")])
            .GivenStream(key, k => [new GadgetRenamed(k, "Gear")])
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .ThenStreamEventCount(key, 2)
            .ThenResult(s => s is not null && Is(s.Name, "Gear"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "Async projections keyed by string are caught up before a query, as they are for Guid streams.")]
    public async Task Async_projection_keyed_by_string_is_current_before_a_query()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .ThenResult(s => s is not null && Is(s.Id, key) && Is(s.Name, "Gear"))
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "An appended assertion on a string stream that didn't exist before the When reads it from version 1.")]
    public async Task Command_that_starts_a_string_stream_is_seen_by_appended_assertions()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppended<GadgetRenamed>(key)
            .ThenAppendedCount(key, 1)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "WhenHttp's appended assertions work on string-keyed streams.")]
    public async Task Http_request_appends_to_a_string_keyed_stream()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .WhenHttp(x => x.Post.Json(new RenameGadget(key, "Gear")).ToUrl("/gadgets/rename"))
            .ThenStatusCode(204)
            .ThenAppended<GadgetRenamed>(key, e => Is(e.Name, "Gear"))
            .ThenNotAppended<GadgetCreated>(key)
            .ThenAppendedCount(key, 1)
            .ThenStreamHasEvent<GadgetCreated>(key)
            .ThenStreamHasNoEvent<GadgetRenamed>(key, e => Is(e.Name, "Cog"))
            .ThenStreamEventCount(key, 2)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A query stage can assert on a string-keyed stream too.")]
    public async Task Query_stage_asserts_on_a_string_keyed_stream()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream(key, k => [new GadgetCreated(k, "Sprocket")])
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .ThenStreamHasEvent<GadgetCreated>(key)
            .ThenStreamHasNoEvent<GadgetRenamed>(key)
            .ThenStreamEventCount(key, 1)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A handler exception on a string-keyed stream is captured and claimable, like any other.")]
    public async Task Handler_exception_on_a_string_stream_is_claimable()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new FailGadget(key))
            .ThenThrows<WidgetException>()
            .ThenAppendedCount(key, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A failure on a string stream names the key in quotes, so it can't be confused with a Guid.")]
    public async Task Failure_message_quotes_the_string_key()
    {
        var key = NewKey();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppended<GadgetRenamed>(key, e => Is(e.Name, "Cog")));

        Assert.StartsWith($"step 3 ThenAppended<GadgetRenamed>(\"{key}\"): no GadgetRenamed matching the predicate appended by step 2 When(RenameGadget)", failure.Message, StringComparison.Ordinal);
        Assert.Contains("v2 GadgetRenamed {", failure.Message, StringComparison.Ordinal);
    }

    [Fact]
    [Trait("business_value", "Reusing a string key across tests fails at the Given with Marten's collision, not later with confusing data.")]
    public async Task Reused_string_key_fails_at_the_given()
    {
        var key = NewKey();
        await TestHarness.Setup(fixture.Host)
            .GivenStream(key, k => [new GadgetCreated(k, "Sprocket")])
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .RunAsync(Ct);

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream(key, k => [new GadgetCreated(k, "Again")])
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key)));

        Assert.StartsWith($"step 1 GivenStream(\"{key}\"): writing the Given failed with ExistingStreamIdCollisionException", failure.Message, StringComparison.Ordinal);
        Assert.IsType<ExistingStreamIdCollisionException>(failure.InnerException);
    }

    [Fact]
    [Trait("business_value", "A string-keyed stream written for one tenant is invisible to another.")]
    public async Task String_keyed_streams_are_isolated_per_tenant()
    {
        var key = NewKey();

        await TestHarness.Setup(fixture.Host)
            .ForTenant("acme")
            .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppendedCount(key, 1)
            .RunAsync(Ct);

        await TestHarness.Setup(fixture.Host)
            .ForTenant("globex")
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .ThenResult(s => s is null)
            .ThenStreamEventCount(key, 0)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A strongly typed id that wraps a string works in every stream method, and its events factory gets the typed id back.")]
    public async Task Typed_id_wrapping_a_string_works_in_every_stream_method()
    {
        var code = new GadgetCode(NewKey());

        await TestHarness.Setup(fixture.Host)
            .GivenStream<Gadget, GadgetCode>(code, c => [new GadgetCreated(c.Value, "Sprocket")])
            .GivenStream(code, c => [new GadgetRenamed(c.Value, "Gear")])
            .When(() => new RenameGadget(code.Value, "Cog"))
            .ThenAppended<GadgetRenamed, GadgetCode>(code, e => Is(e.Name, "Cog"))
            .ThenNotAppended<GadgetCreated, GadgetCode>(code)
            .ThenAppendedCount(code, 1)
            .ThenStreamHasEvent<GadgetCreated, GadgetCode>(code)
            .ThenStreamHasNoEvent<GadgetRenamed, GadgetCode>(code, e => Is(e.Name, "Bolt"))
            .ThenStreamEventCount(code, 3)
            .WhenQuery<GadgetSummary?>(() => new FindGadget(code.Value))
            .ThenResult(s => s is not null && Is(s.Name, "Cog"))
            .ThenStreamHasEvent<GadgetRenamed, GadgetCode>(code)
            .ThenStreamHasNoEvent<GadgetCreated, GadgetCode>(code, e => Is(e.Name, "Bolt"))
            .ThenStreamEventCount(code, 3)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A typed id and the string it wraps name the same stream, so a Given by one and a Then by the other agree.")]
    public async Task Typed_id_and_its_string_name_the_same_stream()
    {
        var code = new GadgetCode(NewKey());

        await TestHarness.Setup(fixture.Host)
            .GivenStream(code, c => [new GadgetCreated(c.Value, "Sprocket")])
            .GivenStream(code.Value, k => [new GadgetRenamed(k, "Gear")])
            .When(() => new RenameGadget(code.Value, "Cog"))
            .ThenAppendedCount(code, 1)
            .ThenAppendedCount(code.Value, 1)
            .ThenStreamEventCount(code.Value, 3)
            .RunAsync(Ct);
    }

    [Fact]
    [Trait("business_value", "A Guid id on a string-keyed store fails before anything is written, naming the id kind the store wants.")]
    public async Task Guid_id_on_a_string_store_fails_before_any_step()
    {
        var id = Guid.NewGuid();
        var key = NewKey();

        var failure = await FailsAsync(TestHarness.Setup(fixture.Host)
            .GivenStream(key, k => [new GadgetCreated(k, "Sprocket")])
            .When(() => new RenameGadget(key, "Gear"))
            .ThenAppendedCount(id, 1));

        Assert.Equal(
            $"step 3 ThenAppendedCount({id}, 1): the stream id {id} is a Guid, but the store uses string stream ids (StreamIdentity.AsString). Pass a string, or a typed id that wraps a string.",
            failure.Message);

        await TestHarness.Setup(fixture.Host)
            .WhenQuery<GadgetSummary?>(() => new FindGadget(key))
            .ThenResult(s => s is null)
            .ThenStreamEventCount(key, 0)
            .RunAsync(Ct);
    }
}
