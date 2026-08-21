#!/usr/bin/env bash
# Fails if spec/ differs from a fresh vendoring at the SHA in spec/SPEC_VERSION.
# Guards against hand-edits to vendored ground truth.
set -euo pipefail

REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

mkdir -p "$WORK/scripts" "$WORK/spec"
cp "$REPO_ROOT/scripts/sync-spec.sh" "$WORK/scripts/"
cp "$REPO_ROOT/spec/SPEC_VERSION" "$WORK/spec/"

"$WORK/scripts/sync-spec.sh" >/dev/null

if diff -r --brief -x .DS_Store "$REPO_ROOT/spec" "$WORK/spec" ; then
  echo "spec/ matches upstream at $(grep -E '^sha:' "$REPO_ROOT/spec/SPEC_VERSION" | awk '{print $2}')"
else
  echo "error: spec/ differs from upstream. Re-run scripts/sync-spec.sh; never hand-edit spec/." >&2
  exit 1
fi
