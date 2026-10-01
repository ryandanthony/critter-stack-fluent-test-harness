# How to test multi-tenant handlers

Your host must use Marten tenancy (for example conjoined: `Events.TenancyStyle = TenancyStyle.Conjoined` and `Policies.AllDocumentsAreMultiTenanted()`).

## Run a scenario as one tenant

Call `ForTenant` straight after `Setup`. Every Given, command, query and assertion then runs as that tenant:

<!-- Test: HowToSamples.TestMultiTenantHandlers_one_tenant -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .ForTenant("acme")
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new RenameWidget(id, "Gear"))
    .ThenAppended<WidgetRenamed>(id)
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Name == "Gear");
```

## Prove tenants are isolated

A scenario has one tenant. Use a second scenario to look from another tenant:

<!-- Test: HowToSamples.TestMultiTenantHandlers_isolation -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .ForTenant("acme")
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .WhenQuery<WidgetSummary>(() => new GetWidgetById(id))
    .ThenResult(s => s.Id == id);

await TestHarness
    .Setup(fixture.Host)
    .ForTenant("globex")
    .WhenQuery<WidgetSummary?>(() => new FindWidget(id))
    .ThenResult(s => s is null)
    .ThenStreamEventCount(id, 0);
```

Without `ForTenant`, the scenario uses Marten's default tenant.
