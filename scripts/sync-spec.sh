#!/usr/bin/env bash
# Re-vendors spec/ from a2ui-project/a2ui at a pinned commit.
#
# Usage:
#   scripts/sync-spec.sh              # re-vendor at the SHA recorded in spec/SPEC_VERSION
#   scripts/sync-spec.sh <sha>        # re-vendor at <sha> and rewrite spec/SPEC_VERSION
#
# spec/ is ground truth for this repository and is never hand-edited.
set -euo pipefail

UPSTREAM="https://github.com/a2ui-project/a2ui.git"
REPO_ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
SPEC_DIR="$REPO_ROOT/spec"
VERSION_FILE="$SPEC_DIR/SPEC_VERSION"

SHA="${1:-}"
if [[ -z "$SHA" ]]; then
  if [[ ! -f "$VERSION_FILE" ]]; then
    echo "error: no SHA given and $VERSION_FILE does not exist" >&2
    exit 1
  fi
  SHA="$(grep -E '^sha:' "$VERSION_FILE" | awk '{print $2}')"
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "Fetching $UPSTREAM @ $SHA ..."
git -c advice.detachedHead=false clone --quiet --filter=blob:none --no-checkout "$UPSTREAM" "$WORK/a2ui"
git -C "$WORK/a2ui" -c advice.detachedHead=false checkout --quiet "$SHA"

RESOLVED="$(git -C "$WORK/a2ui" rev-parse HEAD)"
COMMIT_DATE="$(git -C "$WORK/a2ui" log -1 --format=%cI)"

rm -rf "$SPEC_DIR/v0_9_1" "$SPEC_DIR/v1_0" "$SPEC_DIR/conformance"
mkdir -p "$SPEC_DIR"

copy() { # copy <src-relative> <dest-relative>
  local src="$WORK/a2ui/$1" dest="$SPEC_DIR/$2"
  if [[ ! -e "$src" ]]; then
    echo "error: upstream path missing at $SHA: $1" >&2
    exit 1
  fi
  mkdir -p "$(dirname "$dest")"
  cp -R "$src" "$dest"
}

copy specification/v0_9_1/json      v0_9_1/json
copy specification/v0_9_1/catalogs  v0_9_1/catalogs
copy specification/v0_9_1/docs      v0_9_1/docs
copy specification/v1_0/json        v1_0/json
copy specification/v1_0/catalogs    v1_0/catalogs
copy specification/v1_0/docs        v1_0/docs
copy specification/v1_0/extensions  v1_0/extensions
copy conformance                    conformance

# Python packaging and test harness of the upstream conformance suite are not used here.
rm -rf "$SPEC_DIR/conformance/tests" "$SPEC_DIR/conformance/pyproject.toml"

cat > "$VERSION_FILE" <<EOM
# Vendored from $UPSTREAM. Do not hand-edit spec/; run scripts/sync-spec.sh.
repo: a2ui-project/a2ui
sha: $RESOLVED
date: $COMMIT_DATE
EOM

echo "Vendored spec/ at $RESOLVED ($COMMIT_DATE)"
