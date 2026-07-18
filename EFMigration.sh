#!/usr/bin/env bash
set -euo pipefail

echo "Running Kromic Flow EF Core database migrations..."
dotnet KromicFlow.Api.dll --migrate-only
echo "Kromic Flow EF Core database migrations completed."
