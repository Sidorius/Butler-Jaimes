#!/usr/bin/env bash
# Phase 0 acceptance check: is the other device and its services reachable?
# Run on the Pi:     ./check-connectivity.sh <server-ip>
# Run on the server: ./check-connectivity.sh <pi-ip> --server
set -uo pipefail

host="${1:?Usage: $0 <other-device-ip> [--server]}"
mode="${2:-}"
failed=0

check() { # check <label> <command...>
  local label="$1"; shift
  if "$@" >/dev/null 2>&1; then echo "OK    $label"; else echo "FAIL  $label"; failed=1; fi
}

tcp() { timeout 3 bash -c "exec 3<>/dev/tcp/$1/$2"; }

check "ping $host" ping -c 2 -W 2 "$host"

if [ "$mode" != "--server" ]; then
  check "MQTT   $host:1883" tcp "$host" 1883
  check "HA     $host:8123" tcp "$host" 8123
  check "Mirror $host:5000" tcp "$host" 5000
fi

exit "$failed"
