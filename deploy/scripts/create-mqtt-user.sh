#!/usr/bin/env bash
# Creates deploy/mosquitto/passwd with one user. The password is asked
# interactively, so it never lands in shell history or in Git.
# Usage: ./create-mqtt-user.sh <username>
set -euo pipefail

user="${1:?Usage: $0 <username>}"
target="$(cd "$(dirname "$0")/.." && pwd)/mosquitto/passwd"

touch "$target"
# Run mosquitto_passwd inside the broker image so nothing needs to be installed on the host.
docker run --rm -it -v "$target:/passwd" eclipse-mosquitto:2 mosquitto_passwd -c /passwd "$user"
echo "Written: $target"
