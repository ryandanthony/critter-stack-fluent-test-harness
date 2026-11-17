# How to use string and strongly typed stream ids

Every stream method takes the id your service uses: a `Guid`, a `string` or a strongly typed id that wraps one of them. Pick the form that matches your store's `Events.StreamIdentity`.

## String-keyed streams

If your store sets `options.Events.StreamIdentity = StreamIdentity.AsString`, pass `string` keys. The events factory gets the key back:

<!-- Test: StreamIdHowToSamples.UseStreamIds_string_keys -->
```csharp
var key = $"gadget-{Guid.NewGuid():N}";

await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Gadget>(key, k => [new GadgetCreated(k, "Sprocket")])
    .When(() => new RenameGadget(key, "Gear"))
    .ThenAppended<GadgetRenamed>(key, e => e.Name == "Gear")
    .ThenStreamEventCount(key, 2);
```

Make keys unique per test, as you would Guids. The harness never clears the database, and a Given that starts a stream with a key already in use fails with `ExistingStreamIdCollisionException`.

## Strongly typed ids

If your aggregates and events use an id type such as `record struct ParcelId(Guid Value)`, or a Vogen or StronglyTypedId type, pass it as it is. The harness unwraps it with Marten's own value type rules, and the events factory gets the typed id:

<!-- Test: HowToSamples.UseStreamIds_typed_ids -->
```csharp
var id = new ParcelId(Guid.NewGuid());

await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Parcel, ParcelId>(id, p => [new ParcelCreated(p, "box")])
    .When(() => new ShipParcel(id))
    .ThenAppended<ParcelShipped, ParcelId>(id, e => e.Id == id)
    .ThenAppendedCount(id, 1);
```

Where the method names an event or aggregate type, give the id type as well (`ThenAppended<ParcelShipped, ParcelId>`). C# can't infer one type argument and take the other from you. The count methods infer it: `ThenAppendedCount(id, 1)`, `ThenStreamEventCount(id, 2)`.

A typed id must wrap a `Guid` or a `string` in a single public property, and that kind has to match the store's stream identity. A typed id and the value it wraps name the same stream, so `ThenAppendedCount(id.Value, 1)` works too.

Events and read models that hold a typed id also need it to round-trip through the store's JSON serializer. Vogen and StronglyTypedId generate converters for this. A hand-written wrapper with only a private constructor (and a static `From`) and no converter is stored as `{"Value":null}`, so predicates on the id fail and the failure message shows the null.

## When the id is the wrong kind

A `string` on a Guid store, or a `Guid` on a string-keyed store, fails before any step runs:

```
step 2 GivenStream("order-17"): the stream id "order-17" is a string, but the store uses Guid stream ids (StreamIdentity.AsGuid). Pass a Guid, or a typed id that wraps a Guid.
```

An id type the harness can't unwrap (for example a wrapper around an `int`) fails with `ArgumentException` on the line that passes it. See [stream ids](../reference/api.md#stream-ids) in the API reference.
