#!/usr/bin/env bash
# Prints the newest stable Marten, WolverineFx and Alba on nuget.org as MSBuild property assignments
# (MartenVersion=..., WolverineVersion=..., AlbaVersion=...), one per line, for `just test-latest` and the
# Compatibility workflow (ADR-0018). Every WolverineFx.* package the tests use ships at the WolverineFx version.
# With --github-env the lines are appended to $GITHUB_ENV instead. Exits non-zero if any lookup fails.
set -euo pipefail

latest() {
  local id="${1,,}"
  curl -fsSL "https://api.nuget.org/v3-flatcontainer/$id/index.json" |
    jq -r '[.versions[] | select(test("-") | not)] | last // empty'
}

lines=""
for pair in Marten:Marten WolverineFx:Wolverine Alba:Alba; do
  id="${pair%%:*}"
  prop="${pair##*:}Version"
  version="$(latest "$id")"
  [[ -n "$version" ]] || { echo "no stable $id version found on nuget.org" >&2; exit 1; }
  lines+="$prop=$version"$'\n'
done

if [[ "${1:-}" == "--github-env" ]]; then
  printf '%s' "$lines" >> "$GITHUB_ENV"
fi
printf '%s' "$lines"
