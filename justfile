set shell := ["bash", "-eu", "-o", "pipefail", "-c"]

solution := "src/CritterStack.TestHarness.slnx"

# Show all available recipes.
default:
  just --list

# Restore dependencies for the whole solution.
restore:
  dotnet restore {{solution}}

# Build all projects in Release mode.
build: restore
  dotnet build {{solution}} --no-restore --configuration Release

# Run all tests in Release mode (requires Docker for Testcontainers).
test: build
  dotnet test --solution {{solution}} --no-build --configuration Release --report-xunit-trx --results-directory TestResults

# Pack the harness NuGet package (and symbols) into artifacts/.
pack: build
  dotnet pack src/CritterStack.TestHarness/CritterStack.TestHarness.csproj --no-build --configuration Release --output artifacts

# Pack the harness, then install it from a local feed into a throwaway consumer service and run its scenarios (requires Docker).
package-check: pack
  ./scripts/check-package.sh

# Build the Docusaurus docs site from docs/ (strict: broken links fail). Needs Node 20.
docs-site:
  cd website && npm ci && npm run build

# List product and architecture decision records with their status.
decisions:
  #!/usr/bin/env bash
  set -euo pipefail
  for log in docs/product-decision-log docs/architecture-decision-log; do
    echo "=== ${log#docs/} ==="
    for f in "$log"/[PA]DR-*.md; do
      title=$(head -1 "$f" | sed 's/^# //')
      status=$(grep -m1 -oP '^\- \*\*Status:\*\* \K\w+' "$f" || echo '?')
      printf "  %-11s %s\n" "[$status]" "$title"
    done
    echo ""
  done

# Check that relative links in README.md, AGENTS.md and docs/ resolve.
docs-check:
  #!/usr/bin/env bash
  set -euo pipefail
  broken=0
  while IFS= read -r f; do
    d=$(dirname "$f")
    # Ignore links inside fenced code blocks (templates use placeholder links), external links and anchors.
    while IFS= read -r l; do
      [[ -e "$d/$l" ]] || { echo "✗ $f -> $l"; broken=1; }
    done < <(awk '/^```/{c=!c; next} !c' "$f" | grep -oE '\]\([^)#]+' | sed 's/^](//' | grep -vE '^[a-z]+:' || true)
  done < <(find README.md AGENTS.md docs -name '*.md')
  [[ $broken -eq 0 ]] && echo "✓ docs links OK"
  exit $broken
