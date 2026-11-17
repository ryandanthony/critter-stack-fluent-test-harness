#!/usr/bin/env bash
# Proves the published-package contract (ADR-0013): a throwaway "service" that does NOT inherit this repo's build
# props installs CritterStack.TestHarness from the .nupkg that `just pack` put in artifacts/, using only nuget.org
# plus that local folder, and runs its own scenarios against it. Run through `just package-check`, which packs first.
# Needs Docker. Exits non-zero on any failure.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
feed="$repo/artifacts"
work="$(mktemp -d)"
trap 'rm -rf -- "$work"' EXIT

# Step 1: the package under test. Exactly one, so the version is unambiguous.
shopt -s nullglob
pkgs=("$feed"/CritterStack.TestHarness.*[0-9].nupkg)
if [[ ${#pkgs[@]} -ne 1 ]]; then
  echo "expected exactly one CritterStack.TestHarness .nupkg in $feed, found ${#pkgs[@]}; run 'just pack' on a clean artifacts/" >&2
  exit 1
fi
version="$(basename "${pkgs[0]}" .nupkg)"
version="${version#CritterStack.TestHarness.}"
echo "Package under test: CritterStack.TestHarness $version"

# Step 2: the service's own package versions, SDK pin and feeds. Nothing else from this repo.
cp "$repo/samples/ConsumerService/Directory.Packages.props.template" "$work/Directory.Packages.props"
cp "$repo/global.json" "$work/global.json"
cat > "$work/nuget.config" <<EOF
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
    <add key="local" value="$feed" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="nuget.org"><package pattern="*" /></packageSource>
    <packageSource key="local"><package pattern="CritterStack.TestHarness" /></packageSource>
  </packageSourceMapping>
  <config>
    <!-- Isolated cache: never pick up a stale CritterStack.TestHarness of the same version from ~/.nuget. -->
    <add key="globalPackagesFolder" value="$work/packages" />
  </config>
</configuration>
EOF

# Steps 3-4: the service's test project and scenarios.
mkdir -p "$work/Svc.Tests"
cp "$repo/samples/ConsumerService/Svc.Tests/Svc.Tests.csproj.template" "$work/Svc.Tests/Svc.Tests.csproj"
cp "$repo/samples/ConsumerService/Svc.Tests/OrderTests.cs" "$work/Svc.Tests/OrderTests.cs"

cd "$work"
echo "Restoring the consumer in $work against CritterStack.TestHarness $version (no repo build props)..."
dotnet restore Svc.Tests/Svc.Tests.csproj -p:HarnessVersion="$version"
dotnet build Svc.Tests/Svc.Tests.csproj -c Release --no-restore -p:HarnessVersion="$version"
# The template defaults to the harness's floor versions; MartenVersion, WolverineVersion and AlbaVersion in the
# environment (just package-check-latest) move the consumer to newer releases (ADR-0018). Show what resolved.
echo "Consumer resolved:"
jq -r '.libraries | keys[] | select(test("^(Alba|Marten|WolverineFx|CritterStack\\.TestHarness)/"))' \
  Svc.Tests/obj/project.assets.json | sed 's/^/  /'
echo "Running the consuming service's scenarios..."
dotnet test --project Svc.Tests/Svc.Tests.csproj -c Release --no-build
