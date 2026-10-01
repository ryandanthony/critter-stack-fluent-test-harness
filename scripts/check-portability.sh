#!/usr/bin/env bash
# Proves the harness's portability rule: copy the harness project into a throwaway "service" that does NOT inherit
# this repo's build props (as docs/how-to/add-to-a-service.md describes), add only package versions, and run that
# service's own scenarios against the copied harness. Needs Docker. Exits non-zero on any failure.
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
work="$(mktemp -d)"
trap 'rm -rf -- "$work"' EXIT

# Step 1: copy the harness project (sources only).
mkdir -p "$work/CritterStack.TestHarness"
(cd "$repo/src/CritterStack.TestHarness" && find . -type f \( -name '*.cs' -o -name '*.csproj' \) -not -path './bin/*' -not -path './obj/*' \
  | while read -r f; do mkdir -p "$work/CritterStack.TestHarness/$(dirname "$f")"; cp "$f" "$work/CritterStack.TestHarness/$f"; done)

# Step 2: the service's own package versions and SDK pin. Nothing else from this repo.
cp "$repo/samples/ConsumerService/Directory.Packages.props.template" "$work/Directory.Packages.props"
cp "$repo/global.json" "$work/global.json"
cat > "$work/nuget.config" <<'EOF'
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" protocolVersion="3" />
  </packageSources>
</configuration>
EOF

# Steps 3-4: the service's test project and scenarios.
mkdir -p "$work/Svc.Tests"
cp "$repo/samples/ConsumerService/Svc.Tests/Svc.Tests.csproj.template" "$work/Svc.Tests/Svc.Tests.csproj"
cp "$repo/samples/ConsumerService/Svc.Tests/OrderTests.cs" "$work/Svc.Tests/OrderTests.cs"

cd "$work"
echo "Building the copied harness in $work (no Directory.Build.props, nuget.org only)..."
dotnet build Svc.Tests/Svc.Tests.csproj -c Release
echo "Running the consuming service's scenarios..."
dotnet test --project Svc.Tests/Svc.Tests.csproj -c Release --no-build
