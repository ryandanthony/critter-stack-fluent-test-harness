#!/usr/bin/env bash
# Launches JetBrains Rider's MCP server (stdio bridge) for this repo without any machine-specific paths.
# Used by .mcp.json. Rider must be running with this solution open and Settings > Tools > MCP Server enabled.
#
# Overrides (all optional):
#   RIDER_BIN                    path to the Rider launcher
#   IJ_MCP_SERVER_PORT           port of Rider's built-in MCP server (otherwise auto-discovered)
#   IJ_MCP_SERVER_PROJECT_PATH   project path Rider has open (defaults to <repo>/src)
set -euo pipefail

repo="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
export IJ_MCP_SERVER_PROJECT_PATH="${IJ_MCP_SERVER_PROJECT_PATH:-$repo/src}"

find_rider() {
  if [[ -n "${RIDER_BIN:-}" ]]; then echo "$RIDER_BIN"; return; fi
  if command -v rider >/dev/null 2>&1; then command -v rider; return; fi
  local c
  for c in \
    "$HOME/.local/share/JetBrains/Toolbox/scripts/rider" \
    "$HOME/.local/share/JetBrains/Toolbox/apps/rider/bin/rider" \
    "$HOME/.local/share/JetBrains/Toolbox/apps/rider/bin/rider.sh" \
    /opt/rider/bin/rider.sh \
    /snap/bin/rider \
    "$HOME/Library/Application Support/JetBrains/Toolbox/scripts/rider" \
    "$HOME/Applications/Rider.app/Contents/MacOS/rider" \
    /Applications/Rider.app/Contents/MacOS/rider; do
    if [[ -x "$c" ]]; then echo "$c"; return; fi
  done
  return 1
}

# Localhost ports owned by a running Rider frontend process.
rider_ports() {
  if command -v ss >/dev/null 2>&1; then
    ss -ltnpH 2>/dev/null | grep -E '"rider"|"java"' | awk '{print $4}' | sed -E 's/.*:([0-9]+)$/\1/'
  elif command -v lsof >/dev/null 2>&1; then
    lsof -nP -iTCP -sTCP:LISTEN 2>/dev/null | awk 'tolower($1) ~ /rider|java/ {print $9}' | sed -E 's/.*:([0-9]+)$/\1/'
  fi
}

# Rider's MCP server answers GET /sse with an event stream (the built-in web server on 63342 redirects instead).
is_mcp_port() {
  local ct
  ct="$(curl -s -o /dev/null -m 1 -w '%{content_type}' "http://127.0.0.1:$1/sse" || true)"
  [[ "$ct" == text/event-stream* ]]
}

find_port() {
  if [[ -n "${IJ_MCP_SERVER_PORT:-}" ]]; then echo "$IJ_MCP_SERVER_PORT"; return; fi
  local p
  for p in $(rider_ports | sort -un) 64342 64482; do
    if is_mcp_port "$p"; then echo "$p"; return; fi
  done
  return 1
}

rider="$(find_rider)" || { echo "rider-mcp: Rider launcher not found; set RIDER_BIN" >&2; exit 1; }
IJ_MCP_SERVER_PORT="$(find_port)" || {
  echo "rider-mcp: Rider MCP server not found; start Rider, enable Settings > Tools > MCP Server, or set IJ_MCP_SERVER_PORT" >&2
  exit 1
}
export IJ_MCP_SERVER_PORT

exec "$rider" stdioMcpServer
