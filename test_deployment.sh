#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

cd "$SCRIPT_DIR"

echo "Building and starting containers..."
docker compose -f docker/docker-compose.yml up -d --build

echo "Waiting for service..."
for i in {1..30}; do
  if curl -fsS "http://localhost:8080/health" >/dev/null; then
    break
  fi
  sleep 1
  if [ "$i" -eq 30 ]; then
    echo "Health endpoint did not become ready in time."
    exit 1
  fi
done

echo "Checking health endpoint..."
curl -fsS "http://localhost:8080/health" | cat

echo "Checking Angular index.html..."
curl -fsS "http://localhost:8080/index.html" >/dev/null

echo "Deployment Verification Complete"
