# How to test HTTP endpoints

`WhenHttp` sends a request through your Alba host and lets you assert on the status, the JSON body, the events the endpoint appended and the messages it published, in the same chain as Givens, commands and queries. Use it for Wolverine.Http endpoints, minimal APIs and MVC controllers alike: anything Alba can call.

`Setup` must be given an `IAlbaHost`, which every Alba fixture's host is.

## Assert what an endpoint did

The request is any Alba scenario configuration. Projections are synced first, like before a `When`:

<!-- Test: HttpHowToSamples.TestHttpEndpoints_post -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenHttp(x => x.Post.Json(new RenameWidget(id, "Gear")).ToUrl("/widgets/rename"))
    .ThenStatusCode(204)
    .ThenAppended<WidgetRenamed>(id, e => e.Name == "Gear");
```

`ThenAppended`, `ThenNotAppended`, `ThenAppendedCount` and `ThenPublished` work exactly as after a `When`. The request runs under Wolverine tracking, so messages the endpoint cascades are waited for.

## Assert on the response body

`ThenResponse<T>` reads the body as JSON with the host's serializer:

<!-- Test: HttpHowToSamples.TestHttpEndpoints_response -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenHttp(x => x.Post.Json(new PublishWidget(id)).ToUrl("/widgets/publish"))
    .ThenResponse<WidgetReceipt>(r => r.Status == "published")
    .ThenPublished<WidgetPublishedNotice>(n => n.Id == id);
```

## Expect an error status

Without a `ThenStatusCode`, a status outside 200 to 299 fails the `WhenHttp` at once and shows the body. To test an error response, say which status you expect:

<!-- Test: HttpHowToSamples.TestHttpEndpoints_error_status -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .WhenHttp(x => x.Get.Url($"/widgets/{Guid.NewGuid()}"))
    .ThenStatusCode(404);
```

Use `ThenStatusCode`, not Alba's `StatusCodeShouldBe`: the harness turns Alba's status check off so it can apply this rule.

## Endpoints that throw

If the endpoint throws instead of answering, the exception is captured like a handler's. Claim it with `ThenThrows` first (see [test expected exceptions](test-expected-exceptions.md)):

<!-- Test: HttpHowToSamples.TestHttpEndpoints_throws -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenHttp(x => x.Post.Json(new FailWidget(id)).ToUrl("/widgets/fail"))
    .ThenThrows<WidgetException>()
    .ThenNotAppended<WidgetRenamed>(id);
```

Endpoints that turn errors into a `ProblemDetails` response answer with a status instead, so use `ThenStatusCode` for those.

## Send the tenant

Under `ForTenant`, tell the harness how your service reads the tenant from a request. For a header:

<!-- Test: HttpHowToSamples.TestHttpEndpoints_tenant -->
```csharp
await TestHarness
    .Setup(fixture.Host, o => o.HttpTenant = (x, tenant) => x.WithRequestHeader("tenant", tenant))
    .ForTenant("tenant-a")
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenHttp(x => x.Post.Json(new RecordTenant(id)).ToUrl("/widgets/tenant"))
    .ThenAppended<TenantRecorded>(id, e => e.TenantId == "tenant-a");
```

For a claim, use `x.WithClaim(new Claim("tenant", tenant))`. A `ForTenant` scenario with a `WhenHttp` and no `HttpTenant` fails at once, so a request can never run as the wrong tenant by accident.
