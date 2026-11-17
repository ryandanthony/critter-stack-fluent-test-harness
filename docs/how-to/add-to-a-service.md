# How to add the harness to a service

You have a service that uses Marten and Wolverine and already has an Alba test fixture. These steps add the harness to its test project.

## 1. Add the package

Reference `CritterStack.TestHarness` from your test project. With central package management, put the version in `Directory.Packages.props`:

```xml
<PackageVersion Include="CritterStack.TestHarness" Version="x.y.z" />
```

and the reference, without a version, in the test project:

```xml
<PackageReference Include="CritterStack.TestHarness" />
```

Replace `x.y.z` with the latest version on [nuget.org](https://www.nuget.org/packages/CritterStack.TestHarness). Without central package management, put the `Version` on the `PackageReference` instead.

## 2. Check the Critter Stack versions

The package depends on Alba, Marten and WolverineFx with a minimum version and no upper bound, so newer releases (including Alba 9) are fine and your service's versions win. Only if your repo pins any of them lower does NuGet report a downgrade. In that case raise your pins to at least these:

```xml
<PackageVersion Include="Alba" Version="8.5.3" />
<PackageVersion Include="Marten" Version="9.44.0" />
<PackageVersion Include="WolverineFx" Version="6.44.0" />
```

See [compatibility](../reference/compatibility.md) for the tested versions. `just package-check` in this repo installs the freshly packed package into a sample service with exactly these steps and runs its scenarios. CI runs it on every push at these minimums, and again against the newest stable releases.

## 3. Check the host

Your test host needs:

- Marten registered with `AddMarten(...).IntegrateWithWolverine()`;
- Wolverine registered with `UseWolverine(...)`, finding your handlers;
- `WolverineFx.RuntimeCompilation` referenced, if Wolverine runs in the default `TypeLoadMode.Dynamic`. If your service only references it in Debug (to ship Release without Roslyn), run the tests in Debug too, or the host fails to start with "no IAssemblyGenerator (Roslyn) is registered";
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
