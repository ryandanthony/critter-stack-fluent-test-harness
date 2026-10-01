# How to add the harness to a service

You have a service that uses Marten and Wolverine and already has an Alba test fixture. These steps add the harness to its test project.

## 1. Copy the project

Copy `src/CritterStack.TestHarness/` into your repo and add a project reference from your test project:

```xml
<ProjectReference Include="..\CritterStack.TestHarness\CritterStack.TestHarness.csproj" />
```

The project has no build imports of its own. Its `Directory.Build.props` settings (CodeStandards, GitVersion) stay in this repo.

## 2. Add package versions

The harness references three packages. If your repo uses central package management, make sure `Directory.Packages.props` has a version for each:

```xml
<PackageVersion Include="Alba" Version="8.5.3" />
<PackageVersion Include="Marten" Version="9.44.0" />
<PackageVersion Include="WolverineFx" Version="6.44.0" />
```

See [compatibility](../reference/compatibility.md) for the tested versions.

## 3. Check the host

Your test host needs:

- Marten registered with `AddMarten(...).IntegrateWithWolverine()`;
- Wolverine registered with `UseWolverine(...)`, finding your handlers;
- `WolverineFx.RuntimeCompilation` referenced, if Wolverine runs in the default `TypeLoadMode.Dynamic`;
- `DisableAllExternalWolverineTransports()`, unless you want tracking to wait on real brokers (see [assert published messages](assert-published-messages.md)).

## 4. Write a first scenario

Pass your fixture's host to `TestHarness.Setup`:

<!-- Test: HowToSamples.AddToAService_first_scenario -->
```csharp
await TestHarness
    .Setup(fixture.Host)
    .GivenStream<Widget>(id, i => [new WidgetCreated(i, "Sprocket")])
    .When(() => new RenameWidget(id, "Gear"))
    .ThenAppended<WidgetRenamed>(id);
```

Use a new `Guid` for every stream in every test. The harness never clears the database.

## 5. Pick a projection sync mode

If your service has async projections, see [choose a projection sync mode](choose-projection-sync.md). The default, `CatchUp`, works on any host.
