#!/usr/bin/env bash
# Decides whether the current main commit ships a release, and with which version (ADR-0017).
#
#   scripts/release-plan.sh [force]
#
# Releases when any commit since the last v tag is releasable under Conventional Commits:
# feat, fix or perf, or any type with "!" (breaking). docs, chore, ci, test, refactor, style and
# build commits alone never release. A "BREAKING CHANGE:" footer without "!" isn't counted, because
# GitVersion 6.8 ignores it for the major bump. "force" (true) releases anyway, for example to ship
# a README-only change. A commit that already carries a v tag never releases again.
#
# Prints release=, version= and reason= lines, and appends them to $GITHUB_OUTPUT when it is set.
set -euo pipefail

force="${1:-false}"

last_tag="$(git describe --tags --abbrev=0 --match 'v[0-9]*' 2>/dev/null || true)"
range="HEAD"
if [[ -n "$last_tag" ]]; then
  range="$last_tag..HEAD"
fi

subjects="$(git log "$range" --format='%s')"
releasable="$(grep -cE '^(feat|fix|perf)(\([^)]*\))?!?:|^[a-z]+(\([^)]*\))?!:' <<<"$subjects" || true)"

# The same GitVersion as GitVersion.MsBuild (pinned in dotnet-tools.json), so this
# version is exactly what the build stamps on the package once the commit is tagged.
version="$(dotnet tool run dotnet-gitversion /showvariable MajorMinorPatch)"

release=false
if [[ -n "$(git tag --points-at HEAD --list 'v[0-9]*')" ]]; then
  reason="HEAD is already tagged $(git tag --points-at HEAD --list 'v[0-9]*' | head -1)"
elif [[ "$force" == "true" ]]; then
  release=true
  reason="forced by workflow_dispatch"
elif (( releasable > 0 )); then
  release=true
  reason="$releasable releasable commit(s) since ${last_tag:-the first commit}"
else
  reason="no feat, fix, perf or breaking commits since ${last_tag:-the first commit}"
fi

printf 'release=%s\nversion=%s\nreason=%s\n' "$release" "$version" "$reason"
if [[ -n "${GITHUB_OUTPUT:-}" ]]; then
  printf 'release=%s\nversion=%s\nreason=%s\n' "$release" "$version" "$reason" >>"$GITHUB_OUTPUT"
fi
