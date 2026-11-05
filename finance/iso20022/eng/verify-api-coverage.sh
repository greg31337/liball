#!/usr/bin/env sh
set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
results_directory=$(mktemp -d "${TMPDIR:-/tmp}/iso20022-api-coverage.XXXXXX")
trap 'rm -rf "$results_directory"' EXIT HUP INT TERM

dotnet test "$repository_root/tests/iso20022.Tests.csproj" \
    --no-restore \
    --settings "$repository_root/coverage.runsettings" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$results_directory" \
    --logger "console;verbosity=minimal" \
    --verbosity quiet

coverage_file=$(find "$results_directory" -name coverage.cobertura.xml -print -quit)
if [ -z "$coverage_file" ]; then
    echo "API coverage report was not produced." >&2
    exit 1
fi

python3 "$repository_root/eng/verify_api_coverage.py" \
    "$coverage_file" \
    "$repository_root/eng/api-coverage-exclusions.tsv"
