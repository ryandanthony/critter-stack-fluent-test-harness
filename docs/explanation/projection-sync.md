# Projection sync

Marten's async projections update read models in the background. In a test that background work is a race: write some events, query a read model, and the projection may or may not have run yet. The harness removes the race by bringing projections up to date at the points where something might read them.

## Where sync runs

Before each `When` and each `WhenQuery`, and only if a Given or `When` has written something since the last sync.

- **Before `WhenQuery`**, because query handlers usually read projections.
- **Before `When`**, because some command handlers do too (to check a read model before acting).
- **Not before Thens.** Stream assertions read events directly, and `ThenPublished` reads Wolverine's tracked session; neither touches a projection. To check a read model after a command, follow it with a `WhenQuery`.
- **Not after the last step.** Nothing reads it.

The "only if something was written" rule means two queries in a row sync once, and a scenario that starts with a query and has no Givens doesn't sync at all.

## Why `CatchUp` has two paths

`CatchUp` is the default because it works on any host. Marten offers two ways to force projections up to date, and each only works on one kind of host:

- `ForceAllMartenDaemonActivityToCatchUpAsync` drives the host's own async daemon. On a host with no daemon it throws, because the daemon's coordinator isn't registered.
- `BuildProjectionDaemonAsync` + `CatchUpAsync` builds a temporary daemon. On a host that *does* run a daemon it fails, because two daemons fight over the same projection progress.

So the harness checks whether the host has a projection coordinator and picks the path that works. Test hosts often skip the daemon to start faster, and production-like test hosts often run it; neither needs different settings.

## Why `WaitForNonStale` exists too

`WaitForNonStale` waits for the host's daemon to catch up on its own instead of forcing it. Use it when you want tests to exercise the daemon the way production does. It only works with a daemon.

## Why the harness never wipes data

Marten can reset or clean the database between tests, but that truncates tables other tests (and other test classes running in parallel) are using. The harness leaves data alone and relies on each test using fresh stream ids.
