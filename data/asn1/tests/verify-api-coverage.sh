#!/bin/sh

set -eu

script_directory=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
repository_directory=$(CDPATH= cd -- "$script_directory/.." && pwd)
results_directory=$(mktemp -d "${TMPDIR:-/tmp}/asn1-api-coverage.XXXXXX")

cleanup()
{
    rm -rf "$results_directory"
}

trap cleanup EXIT HUP INT TERM

dotnet test "$repository_directory/asn1.slnx" \
    --no-restore \
    --collect:"XPlat Code Coverage" \
    --results-directory "$results_directory"

coverage_file=$(find "$results_directory" -type f -name coverage.cobertura.xml -print -quit)
if [ -z "$coverage_file" ]
then
    echo "Coverage collection did not produce coverage.cobertura.xml." >&2
    exit 1
fi

api_class_count=0
coverage_failure=0
while IFS= read -r class_line
do
    case "$class_line" in
        *'<class '*'filename="Api/'*)
            api_class_count=$((api_class_count + 1))
            file_name=$(printf '%s\n' "$class_line" \
                | sed -n 's/.*filename="\([^"]*\)".*/\1/p')
            line_rate=$(printf '%s\n' "$class_line" \
                | sed -n 's/.*line-rate="\([^"]*\)".*/\1/p')
            branch_rate=$(printf '%s\n' "$class_line" \
                | sed -n 's/.*branch-rate="\([^"]*\)".*/\1/p')
            if [ "$line_rate" != "1" ] || [ "$branch_rate" != "1" ]
            then
                echo "$file_name: line-rate=$line_rate branch-rate=$branch_rate" >&2
                coverage_failure=1
            fi
            ;;
    esac
done < "$coverage_file"

if [ "$api_class_count" -eq 0 ]
then
    echo "Coverage report contained no src/Api classes." >&2
    exit 1
fi

if [ "$coverage_failure" -ne 0 ]
then
    echo "src/Api line and branch coverage must both remain complete." >&2
    exit 1
fi

echo "Verified 100% line and branch coverage for $api_class_count src/Api classes."
