#!/bin/sh

set -eu

repository_root=$(CDPATH= cd -- "$(dirname -- "$0")/.." && pwd)
results_root=$(mktemp -d "${TMPDIR:-/tmp}/barcode-public-api-coverage.XXXXXX")

cleanup()
{
    rm -rf -- "$results_root"
}

trap cleanup EXIT HUP INT TERM

dotnet test "$repository_root/barcode.slnx" \
    --no-restore \
    --collect:"XPlat Code Coverage" \
    --settings "$repository_root/tests/public-api-coverage.runsettings" \
    --results-directory "$results_root" \
    --logger "console;verbosity=minimal"

coverage_file=$(find "$results_root" -type f \
    -name coverage.cobertura.xml -print -quit)

if [ -z "$coverage_file" ]
then
    echo "Public API coverage report was not generated." >&2
    exit 1
fi

awk '
    /<class / {
        class_count++
        if ($0 !~ /line-rate="1"/ || $0 !~ /branch-rate="1"/) {
            print "Incomplete public API coverage: " $0 > "/dev/stderr"
            incomplete = 1
        }
    }
    END {
        if (class_count == 0) {
            print "No public API classes were present in the coverage report." > "/dev/stderr"
            exit 1
        }

        if (incomplete) {
            exit 1
        }

        print "Public API coverage is 100% line and 100% branch across " \
            class_count " instrumented classes."
    }
' "$coverage_file"
