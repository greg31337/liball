#!/usr/bin/env bash

set -euo pipefail

script_dir=$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)
repository_dir=$(cd "$script_dir/.." && pwd)
coverage_dir=$(mktemp -d "${TMPDIR:-/tmp}/universal-html-api-coverage.XXXXXX")
line_minimum=${PUBLIC_API_LINE_COVERAGE_MINIMUM:-90}
branch_minimum=${PUBLIC_API_BRANCH_COVERAGE_MINIMUM:-75}

cleanup()
{
    rm -rf "$coverage_dir"
}

trap cleanup EXIT

dotnet test "$repository_dir/tests/html.Tests.csproj" \
    --collect:"XPlat Code Coverage" \
    --results-directory "$coverage_dir"

coverage_file=$(find "$coverage_dir" -name coverage.cobertura.xml \
    -print -quit)
if [[ -z "$coverage_file" ]]
then
    echo "The coverage collector did not produce a Cobertura report." >&2
    exit 1
fi

PUBLIC_API_LINE_COVERAGE_MINIMUM="$line_minimum" \
PUBLIC_API_BRANCH_COVERAGE_MINIMUM="$branch_minimum" \
perl - "$coverage_file" <<'PERL'
use strict;
use warnings;

my $path = shift @ARGV;
open my $input, '<', $path or die "Cannot open $path: $!\n";

my $file = '';
my %lines;
while (my $text = <$input>)
{
    if ($text =~ /<class\b[^>]*filename="(Api\/[^"]+)"/)
    {
        $file = $1;
    }
    elsif ($text =~ /<\/class>/)
    {
        $file = '';
    }

    next if $file eq '';
    next unless $text =~ /<line number="(\d+)" hits="(\d+)" branch="([^"]+)"(?: condition-coverage="([^"]+)")?/;

    my ($number, $hits, $branch, $condition) = ($1, $2, $3, $4);
    my $key = "$file:$number";
    $lines{$key}{hits} = $hits
        if !defined($lines{$key}{hits}) || $hits > $lines{$key}{hits};

    if (lc($branch) eq 'true' && defined($condition)
        && $condition =~ /\((\d+)\/(\d+)\)/)
    {
        my ($covered, $total) = ($1, $2);
        $lines{$key}{branches_covered} = $covered
            if !defined($lines{$key}{branches_covered})
                || $covered > $lines{$key}{branches_covered};
        $lines{$key}{branches_total} = $total
            if !defined($lines{$key}{branches_total})
                || $total > $lines{$key}{branches_total};
    }
}
close $input;

die "The Cobertura report contains no src/Api sequence points.\n"
    if scalar(keys %lines) == 0;

my $lines_total = scalar keys %lines;
my $lines_covered = 0;
my $branches_total = 0;
my $branches_covered = 0;
for my $entry (values %lines)
{
    $lines_covered++ if $entry->{hits} > 0;
    $branches_total += $entry->{branches_total} || 0;
    $branches_covered += $entry->{branches_covered} || 0;
}

my $line_rate = 100 * $lines_covered / $lines_total;
my $branch_rate = $branches_total == 0
    ? 100 : 100 * $branches_covered / $branches_total;
my $line_minimum = $ENV{PUBLIC_API_LINE_COVERAGE_MINIMUM};
my $branch_minimum = $ENV{PUBLIC_API_BRANCH_COVERAGE_MINIMUM};

printf "src/Api statement coverage: %d/%d (%.2f%%; minimum %.2f%%)\n",
    $lines_covered, $lines_total, $line_rate, $line_minimum;
printf "src/Api branch coverage: %d/%d (%.2f%%; minimum %.2f%%)\n",
    $branches_covered, $branches_total, $branch_rate, $branch_minimum;

if ($line_rate + 0.00001 < $line_minimum
    || $branch_rate + 0.00001 < $branch_minimum)
{
    print STDERR "Public API coverage is below the required gate.\n";
    exit 1;
}
PERL
