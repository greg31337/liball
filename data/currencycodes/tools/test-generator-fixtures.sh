#!/usr/bin/env bash

set -euo pipefail
export LC_ALL=C

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_root=$(cd "$script_dir/.." && pwd)
temporary_root=""
fixture_count=0

fail()
{
    printf 'generator fixture test failed: %s\n' "$1" >&2
    exit 1
}

cleanup()
{
    local status=$?
    trap - EXIT
    if [[ -n "$temporary_root" && -d "$temporary_root" ]]; then
        case "$temporary_root" in
            /tmp/currency-codes-generator-fixtures.*)
                rm -rf -- "$temporary_root"
                ;;
            *)
                printf 'Refusing to remove unexpected fixture path: %s\n' "$temporary_root" >&2
                status=1
                ;;
        esac
    fi
    exit "$status"
}

trap cleanup EXIT

for command_name in awk cat cp dirname dotnet grep mktemp printf rm; do
    command -v "$command_name" >/dev/null 2>&1 \
        || fail "required command '$command_name' was not found."
done

temporary_root=$(mktemp -d /tmp/currency-codes-generator-fixtures.XXXXXX)
generator_project="$repository_root/tools/CurrencyCodes.DataGenerator/CurrencyCodes.DataGenerator.csproj"
dotnet build "$generator_project" --configuration Release --nologo >/dev/null
generator_dll="$repository_root/tools/CurrencyCodes.DataGenerator/bin/Release/net10.0/CurrencyCodes.DataGenerator.dll"
[[ -f "$generator_dll" ]] || fail "the generator build did not produce its expected assembly."
cldr_release=$(awk -F '\t' '$1 == "cldr-release" { print $2 }' "$repository_root/data/source-checksums.txt")
[[ -n "$cldr_release" ]] || fail "the checksum manifest does not declare a CLDR release."
cldr_data="$repository_root/specs/cldr-$cldr_release-supplementalData.xml"
cldr_metadata="$repository_root/specs/cldr-$cldr_release-supplementalMetadata.xml"

run_success()
{
    local fixture_name=$1
    local output_path=$2
    shift 2
    if ! "$@" > "$output_path" 2> "$output_path.stderr"; then
        cat "$output_path.stderr" >&2
        fail "fixture '$fixture_name' unexpectedly failed."
    fi
}

expect_failure()
{
    local fixture_name=$1
    local expected_message=$2
    shift 2
    local output_path="$temporary_root/$fixture_name.output"
    if "$@" > "$output_path" 2> "$output_path.stderr"; then
        fail "fixture '$fixture_name' unexpectedly succeeded."
    fi
    grep -Fq "$expected_message" "$output_path.stderr" \
        || {
            cat "$output_path.stderr" >&2
            fail "fixture '$fixture_name' did not report '$expected_message'."
        }
    fixture_count=$((fixture_count + 1))
    printf 'fixture-passed\t%s\n' "$fixture_name"
}

assert_line()
{
    local fixture_name=$1
    local expected_line=$2
    local output_path=$3
    grep -Fqx "$expected_line" "$output_path" \
        || fail "fixture '$fixture_name' is missing output '$expected_line'."
}

pass_fixture()
{
    fixture_count=$((fixture_count + 1))
    printf 'fixture-passed\t%s\n' "$1"
}

malformed_xml="$temporary_root/malformed-list-one.xml"
printf '%s\n' \
    '<?xml version="1.0" encoding="UTF-8"?>' \
    '<ISO_4217 Pblshd="2026-01-01">' \
    '  <CcyTbl>' \
    '    <CcyNtry>' \
    '      <CtryNm>FIXTURE</CtryNm>' > "$malformed_xml"
expect_failure \
    malformed-xml \
    'Unexpected end of file' \
    dotnet "$generator_dll" read-list-one "$malformed_xml"

optional_xml="$temporary_root/optional-list-one.xml"
printf '%s\n' \
    '<?xml version="1.0" encoding="UTF-8"?>' \
    '<ISO_4217 Pblshd="2026-01-01">' \
    '  <CcyTbl>' \
    '    <CcyNtry>' \
    '      <CtryNm>FIXTURE NO CURRENCY</CtryNm>' \
    '      <CcyNm>No Universal Currency</CcyNm>' \
    '    </CcyNtry>' \
    '    <CcyNtry>' \
    '      <CtryNm>FIXTURE UNKNOWN MINOR</CtryNm>' \
    '      <CcyNm>Fixture Unit</CcyNm>' \
    '      <Ccy>ZZZ</Ccy>' \
    '      <CcyNbr>999</CcyNbr>' \
    '      <CcyMnrUnts>N.A.</CcyMnrUnts>' \
    '    </CcyNtry>' \
    '  </CcyTbl>' \
    '</ISO_4217>' > "$optional_xml"
optional_output="$temporary_root/optional-list-one.output"
run_success absent-optional-facts "$optional_output" \
    dotnet "$generator_dll" read-list-one "$optional_xml"
assert_line absent-optional-facts $'rows\t2' "$optional_output"
assert_line absent-optional-facts $'alphabetic-codes\t1' "$optional_output"
assert_line absent-optional-facts $'numeric-codes\t1' "$optional_output"
assert_line absent-optional-facts $'known-minor-units\t0' "$optional_output"
pass_fixture absent-optional-facts

partial_date_xml="$temporary_root/partial-date-list-three.xml"
printf '%s\n' \
    '<?xml version="1.0" encoding="UTF-8"?>' \
    '<ISO_4217 Pblshd="2026-01-01">' \
    '  <HstrcCcyTbl>' \
    '    <HstrcCcyNtry><CtryNm>FIXTURE YEAR</CtryNm><CcyNm>Year Unit</CcyNm><Ccy>AAA</Ccy><WthdrwlDt>2001</WthdrwlDt></HstrcCcyNtry>' \
    '    <HstrcCcyNtry><CtryNm>FIXTURE MONTH</CtryNm><CcyNm>Month Unit</CcyNm><Ccy>AAB</Ccy><WthdrwlDt>2001-02</WthdrwlDt></HstrcCcyNtry>' \
    '    <HstrcCcyNtry><CtryNm>FIXTURE DAY</CtryNm><CcyNm>Day Unit</CcyNm><Ccy>AAC</Ccy><WthdrwlDt>2001-02-03</WthdrwlDt></HstrcCcyNtry>' \
    '    <HstrcCcyNtry><CtryNm>FIXTURE UNKNOWN</CtryNm><CcyNm>Unknown Unit</CcyNm><Ccy>AAD</Ccy><WthdrwlDt>UNKNOWN</WthdrwlDt></HstrcCcyNtry>' \
    '  </HstrcCcyTbl>' \
    '</ISO_4217>' > "$partial_date_xml"
partial_date_output="$temporary_root/partial-date.output"
run_success partial-dates "$partial_date_output" \
    dotnet "$generator_dll" read-list-three "$partial_date_xml"
assert_line partial-dates $'withdrawal-year\t1' "$partial_date_output"
assert_line partial-dates $'withdrawal-month\t1' "$partial_date_output"
assert_line partial-dates $'withdrawal-day\t1' "$partial_date_output"
assert_line partial-dates $'withdrawal-unknown\t1' "$partial_date_output"
pass_fixture partial-dates

fraction_xml="$temporary_root/default-fraction.xml"
printf '%s\n' \
    '<?xml version="1.0" encoding="UTF-8"?>' \
    '<supplementalData>' \
    '  <currencyData>' \
    '    <fractions>' \
    '      <info iso4217="DEFAULT" digits="3" rounding="0" cashDigits="2" cashRounding="5"/>' \
    '    </fractions>' \
    '  </currencyData>' \
    '</supplementalData>' > "$fraction_xml"
fraction_output="$temporary_root/default-fraction.output"
run_success cldr-default-inheritance "$fraction_output" \
    dotnet "$generator_dll" resolve-cldr-fraction "$fraction_xml" ZZZ
assert_line cldr-default-inheritance $'digits\t3\tinherited' "$fraction_output"
assert_line cldr-default-inheritance $'rounding\t0\tinherited' "$fraction_output"
assert_line cldr-default-inheritance $'cash-digits\t2\tinherited' "$fraction_output"
assert_line cldr-default-inheritance $'cash-rounding\t5\tinherited' "$fraction_output"
pass_fixture cldr-default-inheritance

duplicate_list_three="$temporary_root/duplicate-list-three.xml"
awk '
    /<\/HstrcCcyTbl>/ {
        print "\t\t<HstrcCcyNtry>"
        print "\t\t\t<CtryNm>AFGHANISTAN</CtryNm>"
        print "\t\t\t<CcyNm>Afghani</CcyNm>"
        print "\t\t\t<Ccy>AFA</Ccy>"
        print "\t\t\t<CcyNbr>004</CcyNbr>"
        print "\t\t\t<WthdrwlDt>2003-01</WthdrwlDt>"
        print "\t\t</HstrcCcyNtry>"
    }
    { print }
' "$repository_root/specs/iso-4217-list-three.xml" > "$duplicate_list_three"
expect_failure \
    duplicate-rows \
    'List Three repeats historical designation' \
    dotnet "$generator_dll" validate-data \
        "$repository_root/specs/iso-4217-list-one.xml" \
        "$duplicate_list_three" \
        "$cldr_data" \
        "$cldr_metadata" \
        "$repository_root/specs/un-m49-overview.html" \
        "$repository_root/data/overrides/territory-name-map.csv" \
        "$repository_root/data/overrides/source-conflicts.csv"

stale_overlay="$temporary_root/stale-territory-name-map.csv"
cp "$repository_root/data/overrides/territory-name-map.csv" "$stale_overlay"
printf '%s\n' \
    'list-one,FIXTURE STALE,Fixture Stale,OtherSpecialEntity,,,,fixture-stale-test' \
    >> "$stale_overlay"
expect_failure \
    stale-overrides \
    'Stale territory-name-map rows: list-one:FIXTURE STALE.' \
    dotnet "$generator_dll" reconcile-territories \
        "$repository_root/specs/iso-4217-list-one.xml" \
        "$repository_root/specs/iso-4217-list-three.xml" \
        "$cldr_data" \
        "$cldr_metadata" \
        "$repository_root/specs/un-m49-overview.html" \
        "$stale_overlay"

conflicting_list_one="$temporary_root/unreviewed-conflict-list-one.xml"
awk '
    BEGIN { replaced = 0 }
    {
        if (replaced == 0 && $0 ~ /<CcyNbr>971<\/CcyNbr>/) {
            sub(/971/, "970")
            replaced = 1
        }
        print
    }
    END {
        if (replaced == 0) {
            exit 2
        }
    }
' "$repository_root/specs/iso-4217-list-one.xml" > "$conflicting_list_one"
expect_failure \
    unreviewed-conflicts \
    'Unreviewed identity conflicts: AFN:970:971.' \
    dotnet "$generator_dll" reconcile-cross-source \
        "$conflicting_list_one" \
        "$repository_root/specs/iso-4217-list-three.xml" \
        "$cldr_data" \
        "$repository_root/data/overrides/source-conflicts.csv"

printf 'generator-fixtures\t%s\n' "$fixture_count"
printf '%s\n' "Generator fixture tests passed."
