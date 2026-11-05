#!/usr/bin/env bash
set -euo pipefail

examples_dir="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
cd "$examples_dir"

dotnet build Examples.slnx --configuration Release --nologo --disable-build-servers -m:1

count=0
for project in "$examples_dir"/*/*.csproj; do
    name="${project##*/}"
    name="${name%.csproj}"
    printf '\n========== %s ==========\n' "$name"
    dotnet run --project "$project" --configuration Release --no-build
    count=$((count + 1))
done

printf '\nAll %s examples completed successfully.\n' "$count"
