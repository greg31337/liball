#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_dir/.." && pwd)
temporary_root=""

fail()
{
    printf 'generator repeatability test failed: %s\n' "$1" >&2
    exit 1
}

cleanup()
{
    local status=$?
    trap - EXIT
    if [[ -n "$temporary_root" && -d "$temporary_root" ]]; then
        case "$temporary_root" in
            /tmp/currency-codes-generator-repeatability.*)
                rm -rf -- "$temporary_root"
                ;;
            *)
                printf 'Refusing to remove unexpected repeatability path: %s\n' "$temporary_root" >&2
                status=1
                ;;
        esac
    fi
    exit "$status"
}

trap cleanup EXIT

for command_name in cmp dirname dotnet find mkdir mktemp rm sed sort; do
    command -v "$command_name" >/dev/null 2>&1 \
        || fail "required command '$command_name' was not found."
done

temporary_root=$(mktemp -d /tmp/currency-codes-generator-repeatability.XXXXXX)
first_output="$temporary_root/first"
second_output="$temporary_root/second"
mkdir -p "$first_output" "$second_output"

generator_project="$repository_root/tools/CurrencyCodes.DataGenerator/CurrencyCodes.DataGenerator.csproj"
dotnet build "$generator_project" --configuration Release --nologo >/dev/null
generator_dll="$repository_root/tools/CurrencyCodes.DataGenerator/bin/Release/net10.0/CurrencyCodes.DataGenerator.dll"
[[ -f "$generator_dll" ]] || fail "the generator build did not produce its expected assembly."
cldr_release=$(sed -n '3s/^cldr-release\t//p' "$repository_root/data/source-checksums.txt")
[[ -n "$cldr_release" ]] || fail "the checksum manifest does not declare a CLDR release."

generate_snapshot()
{
    local output_directory=$1
    local language=$2
    local time_zone=$3
    unset LC_ALL

    LANG="$language" TZ="$time_zone" dotnet "$generator_dll" emit-canonical-tables \
        "$repository_root/specs/iso-4217-list-one.xml" \
        "$repository_root/specs/iso-4217-list-three.xml" \
        "$repository_root/specs/cldr-$cldr_release-supplementalData.xml" \
        "$repository_root/specs/cldr-$cldr_release-supplementalMetadata.xml" \
        "$repository_root/specs/un-m49-overview.html" \
        "$repository_root/data/overrides/territory-name-map.csv" \
        "$repository_root/data/overrides/source-conflicts.csv" \
        "$output_directory/GeneratedAggregationTables.cs" >/dev/null

    LANG="$language" TZ="$time_zone" dotnet "$generator_dll" emit-version-data \
        "$repository_root/data/source-checksums.txt" \
        "$repository_root" \
        "$output_directory/GeneratedDataVersions.cs" >/dev/null
}

generate_snapshot "$first_output" en_US.UTF-8 UTC
generate_snapshot "$second_output" tr_TR.UTF-8 Pacific/Auckland

first_files="$temporary_root/first-files.txt"
second_files="$temporary_root/second-files.txt"
(
    cd "$first_output"
    find . -type f -print | LC_ALL=C sort
) > "$first_files"
(
    cd "$second_output"
    find . -type f -print | LC_ALL=C sort
) > "$second_files"

[[ -s "$first_files" ]] || fail "generation produced no files."
cmp -s "$first_files" "$second_files" || fail "generation produced different file sets."

sha256_file()
{
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | sed 's/[[:space:]].*$//'
    elif command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | sed 's/[[:space:]].*$//'
    else
        fail "neither shasum nor sha256sum is available."
    fi
}

file_count=0
while IFS= read -r relative_path; do
    first_file="$first_output/${relative_path#./}"
    second_file="$second_output/${relative_path#./}"
    cmp -s "$first_file" "$second_file" \
        || fail "generated file '${relative_path#./}' is not byte-repeatable."
    digest=$(sha256_file "$first_file")
    printf 'repeatable\t%s\t%s\n' "${relative_path#./}" "$digest"
    file_count=$((file_count + 1))
done < "$first_files"

printf 'generated-files\t%s\n' "$file_count"
printf '%s\n' "Generator repeatability test passed."
