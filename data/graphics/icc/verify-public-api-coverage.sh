#!/usr/bin/env sh
set -eu

repository_directory="$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)"
coverage_parent="$repository_directory/tests/TestResults"
mkdir -p "$coverage_parent"
results_directory="$(mktemp -d "$coverage_parent/PublicApiCoverage.XXXXXX")"

dotnet test "$repository_directory/tests/icc.Tests.csproj" \
    --settings "$repository_directory/public-api-coverage.runsettings" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$results_directory" \
    "$@"

coverage_file="$(find "$results_directory" -type f \
    -name coverage.cobertura.xml -print -quit)"
if [ -z "$coverage_file" ]; then
    echo "Public API coverage report was not produced." >&2
    exit 1
fi

line_rate="$(sed -n 's/^<coverage line-rate="\([^"]*\)".*/\1/p' \
    "$coverage_file")"
branch_rate="$(sed -n 's/^<coverage .* branch-rate="\([^"]*\)".*/\1/p' \
    "$coverage_file")"
if [ -z "$line_rate" ] || [ -z "$branch_rate" ]; then
    echo "Public API coverage rates could not be read from $coverage_file." >&2
    exit 1
fi

if ! awk -v lines="$line_rate" -v branches="$branch_rate" \
    'BEGIN { exit !(lines == 1 && branches == 1) }'; then
    echo "Public API coverage gate failed: line=$line_rate branch=$branch_rate; both must be 1." >&2
    echo "Coverage report: $coverage_file" >&2
    exit 1
fi

echo "Public API coverage gate passed: line=100% branch=100%."
echo "Coverage report: $coverage_file"
