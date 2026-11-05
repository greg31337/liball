#!/usr/bin/env bash

set -euo pipefail
export LC_ALL=C

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_dir/.." && pwd)
source_map="$repository_root/data/source-downloads.txt"
checksum_manifest="$repository_root/data/source-checksums.txt"
apply_update=false
stage_dir=""
backup_dir=""
install_started=false
install_complete=false
manifest_candidate=""

usage()
{
    printf '%s\n' "Usage: tools/update-currency-data.sh [--check|--apply] [--source-map <path>]"
    printf '%s\n' "  --check       Download, validate, and print the semantic diff without replacing data (default)."
    printf '%s\n' "  --apply       Replace specs and the checksum manifest after every validation succeeds."
    printf '%s\n' "  --source-map  Use an alternate reviewed HTTPS source map."
}

fail()
{
    printf 'currency-data update failed: %s\n' "$1" >&2
    exit 1
}

safe_remove_tree()
{
    local target=$1
    case "$target" in
        "$repository_root"/.currency-data-stage.*|"$repository_root"/.currency-data-backup.*)
            rm -rf -- "$target"
            ;;
        *)
            printf 'Refusing to remove unexpected temporary path: %s\n' "$target" >&2
            return 1
            ;;
    esac
}

rollback_install()
{
    if [[ -n "$backup_dir" && -d "$backup_dir/specs" ]]; then
        if [[ -d "$repository_root/specs" ]]; then
            mv -- "$repository_root/specs" "$stage_dir/failed-specs"
        fi
        mv -- "$backup_dir/specs" "$repository_root/specs"
    fi

    if [[ -n "$backup_dir" && -f "$backup_dir/source-checksums.txt" ]]; then
        local rollback_manifest="$repository_root/data/.source-checksums.rollback.$$"
        cp -- "$backup_dir/source-checksums.txt" "$rollback_manifest"
        chmod 0644 "$rollback_manifest"
        mv -f -- "$rollback_manifest" "$checksum_manifest"
    fi
}

cleanup()
{
    local status=$?
    trap - EXIT
    if [[ "$install_started" == true && "$install_complete" == false ]]; then
        printf '%s\n' "Update installation failed; restoring the previous pinned snapshot." >&2
        rollback_install || status=1
    fi

    if [[ -n "$backup_dir" && -d "$backup_dir" ]]; then
        safe_remove_tree "$backup_dir" || status=1
    fi
    if [[ -n "$manifest_candidate" && -f "$manifest_candidate" ]]; then
        case "$manifest_candidate" in
            "$repository_root"/data/.source-checksums.new.*)
                rm -f -- "$manifest_candidate" || status=1
                ;;
            *)
                printf 'Refusing to remove unexpected manifest candidate: %s\n' "$manifest_candidate" >&2
                status=1
                ;;
        esac
    fi
    if [[ -n "$stage_dir" && -d "$stage_dir" ]]; then
        safe_remove_tree "$stage_dir" || status=1
    fi
    exit "$status"
}

trap cleanup EXIT

while [[ $# -gt 0 ]]; do
    case "$1" in
        --check)
            apply_update=false
            shift
            ;;
        --apply)
            apply_update=true
            shift
            ;;
        --source-map)
            [[ $# -ge 2 ]] || fail "--source-map requires a path."
            source_map=$2
            shift 2
            ;;
        --help|-h)
            usage
            exit 0
            ;;
        *)
            usage >&2
            fail "unknown argument '$1'."
            ;;
    esac
done

for command_name in awk cat chmod cmp cp curl date dirname dotnet find grep head mkdir mktemp mv od rm sed sort tail tr wc; do
    command -v "$command_name" >/dev/null 2>&1 || fail "required command '$command_name' was not found."
done

[[ -f "$source_map" ]] || fail "source map '$source_map' does not exist."
[[ -f "$checksum_manifest" ]] || fail "checksum manifest '$checksum_manifest' does not exist."
[[ -d "$repository_root/specs" ]] || fail "the pinned specs directory does not exist."

stage_dir=$(mktemp -d "$repository_root/.currency-data-stage.XXXXXX")
stage_root="$stage_dir/repository"
mkdir -p "$stage_root/specs" "$stage_root/data"
cp -R -- "$repository_root/data/overrides" "$stage_root/data/overrides"

map_version=$(sed -n '1s/^download-manifest-version\t//p' "$source_map")
map_cldr_release=$(sed -n '2s/^cldr-release\t//p' "$source_map")
manifest_cldr_release=$(sed -n '3s/^cldr-release\t//p' "$checksum_manifest")
[[ "$map_version" == "1" ]] || fail "the source map version must be '1'."
[[ -n "$map_cldr_release" ]] || fail "the source map is missing its CLDR release."
[[ "$map_cldr_release" == "$manifest_cldr_release" ]] \
    || fail "the source map and checksum manifest declare different CLDR releases."

map_paths="$stage_dir/source-map-paths.txt"
manifest_paths="$stage_dir/manifest-paths.txt"
actual_paths="$stage_dir/actual-spec-paths.txt"
awk -F '\t' '$1 == "file" { print $2 }' "$source_map" > "$map_paths"
awk -F '\t' '$1 == "file" { print $2 }' "$checksum_manifest" > "$manifest_paths"
find "$repository_root/specs" -type f -print \
    | sed "s#^$repository_root/##" \
    | LC_ALL=C sort > "$actual_paths"
LC_ALL=C sort -cu "$map_paths" \
    || fail "source-map paths must be in unique canonical order."
cmp -s "$map_paths" "$manifest_paths" \
    || fail "source-map paths do not exactly match the checksum manifest."
cmp -s "$manifest_paths" "$actual_paths" \
    || fail "the specs directory does not exactly match the checksum manifest."

map_entry_count=0
previous_path=""
while IFS=$'\t' read -r record target_path source_url content_kind extra; do
    map_entry_count=$((map_entry_count + 1))
    [[ "$record" == "file" && -n "$target_path" && -n "$source_url" && -n "$content_kind" && -z "${extra:-}" ]] \
        || fail "invalid source-map entry $((map_entry_count + 2))."
    [[ "$target_path" == specs/* && "$target_path" != *".."* && "$target_path" != *\\* ]] \
        || fail "unsafe source-map target '$target_path'."
    [[ "$source_url" == https://* ]] || fail "source URL for '$target_path' is not HTTPS."
    case "$content_kind" in
        doc|html|pdf|text|xlsx|xml)
            ;;
        *)
            fail "unknown content kind '$content_kind' for '$target_path'."
            ;;
    esac
    if [[ -n "$previous_path" && ! "$previous_path" < "$target_path" ]]; then
        fail "source-map entries are not in unique canonical path order."
    fi
    previous_path=$target_path
done < <(tail -n +3 "$source_map")
[[ "$map_entry_count" -gt 0 ]] || fail "the source map contains no files."

content_type_allowed()
{
    local kind=$1
    local media_type=$2
    case "$kind:$media_type" in
        xml:application/xml|xml:text/xml|xml:text/plain|xml:application/octet-stream)
            return 0
            ;;
        html:text/html|html:application/xhtml+xml)
            return 0
            ;;
        text:text/plain|text:application/octet-stream)
            return 0
            ;;
        pdf:application/pdf|pdf:application/octet-stream)
            return 0
            ;;
        xlsx:application/vnd.openxmlformats-officedocument.spreadsheetml.sheet|xlsx:application/zip|xlsx:application/octet-stream)
            return 0
            ;;
        doc:application/msword|doc:application/vnd.ms-office|doc:application/octet-stream)
            return 0
            ;;
        *)
            return 1
            ;;
    esac
}

contains_nul_byte()
{
    od -An -v -tx1 "$1" | grep -Eq '(^|[[:space:]])00([[:space:]]|$)'
}

validate_magic()
{
    local path=$1
    local kind=$2
    local leading_hex
    leading_hex=$(od -An -tx1 -N8 "$path" | tr -d ' \n')
    case "$kind" in
        pdf)
            [[ "$leading_hex" == 25504446* ]] || fail "'$path' is not a PDF file."
            tail -c 2048 "$path" | grep -a -q '%%EOF' || fail "'$path' has no PDF end marker."
            ;;
        xlsx)
            [[ "$leading_hex" == 504b0304* ]] || fail "'$path' is not an XLSX ZIP container."
            command -v unzip >/dev/null 2>&1 || fail "required command 'unzip' was not found."
            unzip -tqq "$path" >/dev/null || fail "'$path' is a corrupt XLSX ZIP container."
            ;;
        doc)
            [[ "$leading_hex" == d0cf11e0a1b11ae1* ]] || fail "'$path' is not an OLE Word document."
            ;;
        xml)
            contains_nul_byte "$path" && fail "'$path' contains a NUL byte."
            head -c 256 "$path" | grep -aFq '<?xml' || fail "'$path' has no XML declaration."
            ;;
        html)
            contains_nul_byte "$path" && fail "'$path' contains a NUL byte."
            head -c 2048 "$path" | grep -a -Eiq '<!doctype[[:space:]]+html|<html' \
                || fail "'$path' has no HTML document marker."
            ;;
        text)
            contains_nul_byte "$path" && fail "'$path' contains a NUL byte."
            ;;
    esac
    return 0
}

printf '%s\n' "Downloading reviewed currency-data sources into $stage_dir"
while IFS=$'\t' read -r record target_path source_url content_kind extra; do
    destination="$stage_root/$target_path"
    mkdir -p "$(dirname "$destination")"
    curl_metadata=$(curl \
        --fail \
        --silent \
        --show-error \
        --location \
        --proto '=https' \
        --proto-redir '=https' \
        --tlsv1.2 \
        --connect-timeout 20 \
        --max-time 180 \
        --retry 2 \
        --retry-delay 1 \
        --output "$destination" \
        --write-out $'%{content_type}\n%{url_effective}' \
        "$source_url") || fail "HTTPS download failed for '$target_path'."
    response_type=${curl_metadata%%$'\n'*}
    effective_url=${curl_metadata#*$'\n'}
    response_type=${response_type%%;*}
    response_type=$(printf '%s' "$response_type" | tr '[:upper:]' '[:lower:]')
    [[ "$effective_url" == https://* ]] || fail "download for '$target_path' redirected outside HTTPS."
    [[ -s "$destination" ]] || fail "download for '$target_path' is empty."
    content_type_allowed "$content_kind" "$response_type" \
        || fail "unexpected content type '$response_type' for '$target_path'."
    validate_magic "$destination" "$content_kind"
    chmod 0644 "$destination"
    printf 'downloaded\t%s\t%s\n' "$target_path" "$response_type"
done < <(tail -n +3 "$source_map")

generator_project="$repository_root/tools/CurrencyCodes.DataGenerator/CurrencyCodes.DataGenerator.csproj"
dotnet build "$generator_project" --configuration Release --nologo >/dev/null
generator_dll="$repository_root/tools/CurrencyCodes.DataGenerator/bin/Release/net10.0/CurrencyCodes.DataGenerator.dll"
[[ -f "$generator_dll" ]] || fail "the generator build did not produce its expected assembly."

list_one_output=$(dotnet "$generator_dll" read-list-one "$stage_root/specs/iso-4217-list-one.xml")
list_three_output=$(dotnet "$generator_dll" read-list-three "$stage_root/specs/iso-4217-list-three.xml")
list_one_date=$(printf '%s\n' "$list_one_output" | awk -F '\t' '$1 == "publication-date" { print $2 }')
list_three_date=$(printf '%s\n' "$list_three_output" | awk -F '\t' '$1 == "publication-date" { print $2 }')
[[ -n "$list_one_date" && "$list_one_date" == "$list_three_date" ]] \
    || fail "downloaded ISO lists do not have the same publication date."

sha256_file()
{
    if command -v shasum >/dev/null 2>&1; then
        shasum -a 256 "$1" | awk '{ print $1 }'
    elif command -v sha256sum >/dev/null 2>&1; then
        sha256sum "$1" | awk '{ print $1 }'
    else
        fail "neither shasum nor sha256sum is available."
    fi
}

new_manifest="$stage_root/data/source-checksums.txt"
{
    printf 'manifest-version\t1\n'
    printf 'iso-publication-date\t%s\n' "$list_one_date"
    printf 'cldr-release\t%s\n' "$map_cldr_release"
    printf 'retrieval-date\t%s\n' "$(date -u +%Y-%m-%d)"
    while IFS=$'\t' read -r record target_path source_url content_kind extra; do
        source_file="$stage_root/$target_path"
        byte_length=$(wc -c < "$source_file" | tr -d ' ')
        digest=$(sha256_file "$source_file")
        printf 'file\t%s\t%s\t%s\n' "$target_path" "$byte_length" "$digest"
    done < <(tail -n +3 "$source_map")
} > "$new_manifest"
chmod 0644 "$new_manifest"

new_list_one="$stage_root/specs/iso-4217-list-one.xml"
new_list_three="$stage_root/specs/iso-4217-list-three.xml"
new_cldr_data="$stage_root/specs/cldr-$map_cldr_release-supplementalData.xml"
new_cldr_metadata="$stage_root/specs/cldr-$map_cldr_release-supplementalMetadata.xml"
new_un_m49="$stage_root/specs/un-m49-overview.html"
new_territory_overlay="$stage_root/data/overrides/territory-name-map.csv"
new_conflicts="$stage_root/data/overrides/source-conflicts.csv"

dotnet "$generator_dll" validate-data \
    "$new_list_one" \
    "$new_list_three" \
    "$new_cldr_data" \
    "$new_cldr_metadata" \
    "$new_un_m49" \
    "$new_territory_overlay" \
    "$new_conflicts" > "$stage_dir/validation.txt"

dotnet "$generator_dll" semantic-diff \
    "$repository_root/specs/iso-4217-list-one.xml" \
    "$repository_root/specs/iso-4217-list-three.xml" \
    "$repository_root/specs/cldr-$map_cldr_release-supplementalData.xml" \
    "$repository_root/specs/cldr-$map_cldr_release-supplementalMetadata.xml" \
    "$repository_root/specs/un-m49-overview.html" \
    "$repository_root/data/overrides/territory-name-map.csv" \
    "$repository_root/data/overrides/source-conflicts.csv" \
    "$new_list_one" \
    "$new_list_three" \
    "$new_cldr_data" \
    "$new_cldr_metadata" \
    "$new_un_m49" \
    "$new_territory_overlay" \
    "$new_conflicts" > "$stage_dir/semantic-diff.tsv"

dotnet "$generator_dll" emit-version-data \
    "$new_manifest" \
    "$stage_root" \
    "$stage_dir/GeneratedDataVersions.cs" > "$stage_dir/version-validation.txt"

dotnet "$generator_dll" emit-canonical-tables \
    "$new_list_one" \
    "$new_list_three" \
    "$new_cldr_data" \
    "$new_cldr_metadata" \
    "$new_un_m49" \
    "$new_territory_overlay" \
    "$new_conflicts" \
    "$stage_dir/GeneratedAggregationTables.cs" > "$stage_dir/emission-validation.txt"

printf '\n%s\n' "Semantic data diff:"
cat "$stage_dir/semantic-diff.tsv"
printf '\nvalidated-iso-publication-date\t%s\n' "$list_one_date"
printf 'validated-cldr-release\t%s\n' "$map_cldr_release"
printf 'checksum-manifest-sha256\t%s\n' "$(sha256_file "$new_manifest")"

if [[ "$apply_update" == false ]]; then
    printf '%s\n' "Check completed successfully; pinned files were not changed. Re-run with --apply to install."
    exit 0
fi

backup_dir=$(mktemp -d "$repository_root/.currency-data-backup.XXXXXX")
cp -- "$checksum_manifest" "$backup_dir/source-checksums.txt"
install_started=true
mv -- "$repository_root/specs" "$backup_dir/specs"
mv -- "$stage_root/specs" "$repository_root/specs"
manifest_candidate="$repository_root/data/.source-checksums.new.$$"
cp -- "$new_manifest" "$manifest_candidate"
chmod 0644 "$manifest_candidate"
mv -f -- "$manifest_candidate" "$checksum_manifest"
manifest_candidate=""
install_complete=true
safe_remove_tree "$backup_dir"
backup_dir=""
printf '%s\n' "Pinned currency-data sources and checksum manifest were replaced successfully."
