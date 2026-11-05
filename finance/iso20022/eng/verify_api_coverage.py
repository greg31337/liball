#!/usr/bin/env python3
import re
import sys
import xml.etree.ElementTree as ET

if len(sys.argv) != 3:
    raise SystemExit("usage: verify_api_coverage.py COVERAGE_XML EXCLUSIONS_TSV")

coverage_path = sys.argv[1]
exclusions_path = sys.argv[2]
exclusions = []
with open(exclusions_path, encoding="utf-8") as stream:
    for number, raw in enumerate(stream, 1):
        line = raw.rstrip("\n")
        if not line or line.startswith("#"):
            continue
        fields = line.split("\t", 4)
        if len(fields) != 5:
            raise SystemExit(f"{exclusions_path}:{number}: expected five tab-separated fields")
        path, first_text, last_text, kind, reason = fields
        if kind not in ("line", "branch"):
            raise SystemExit(f"{exclusions_path}:{number}: kind must be line or branch")
        if not reason.startswith(("runtime: ", "platform: ")):
            raise SystemExit(f"{exclusions_path}:{number}: reason must identify runtime or platform")
        try:
            first = int(first_text)
            last = int(last_text)
        except ValueError:
            raise SystemExit(f"{exclusions_path}:{number}: line bounds must be integers")
        if first <= 0 or last < first:
            raise SystemExit(f"{exclusions_path}:{number}: invalid line bounds")
        exclusions.append((path, first, last, kind, reason, number))


def relative_path(value):
    marker = "src/Api/"
    index = value.find(marker)
    return value[index:] if index >= 0 else value


def excluded(path, line, kind):
    for candidate, first, last, exclusion_kind, _, _ in exclusions:
        if candidate == path and first <= line <= last and exclusion_kind == kind:
            return True
    return False


def condition_counts(value):
    match = re.search(r"\((\d+)/(\d+)\)", value or "")
    return (int(match.group(1)), int(match.group(2))) if match else None

root = ET.parse(coverage_path).getroot()
points = {}
for class_node in root.findall(".//class"):
    path = relative_path(class_node.get("filename", ""))
    if not path.startswith("src/Api/"):
        continue
    for line_node in class_node.findall("./lines/line"):
        line = int(line_node.get("number", "0"))
        hits = int(line_node.get("hits", "0"))
        branch = condition_counts(line_node.get("condition-coverage"))
        points[(path, line)] = (hits, branch)

if not points:
    raise SystemExit("coverage report contains no src/Api sequence points")

known = set()
for path, first, last, kind, _, number in exclusions:
    matches = [line for candidate, line in points if candidate == path and first <= line <= last]
    if not matches:
        raise SystemExit(
            f"{exclusions_path}:{number}: exclusion does not match an src/Api sequence point")
    known.add(number)

failures = []
covered_lines = 0
required_lines = 0
covered_branches = 0
required_branches = 0
excluded_lines = 0
excluded_branch_lines = 0
for (path, line), (hits, branch) in sorted(points.items()):
    line_is_excluded = excluded(path, line, "line")
    if line_is_excluded:
        excluded_lines += 1
    else:
        required_lines += 1
        if hits > 0:
            covered_lines += 1
        else:
            failures.append(f"uncovered line {path}:{line}")
    if branch is not None:
        branch_covered, branch_total = branch
        branch_is_excluded = line_is_excluded or excluded(path, line, "branch")
        if branch_is_excluded:
            excluded_branch_lines += 1
        else:
            required_branches += branch_total
            covered_branches += branch_covered
            if branch_covered != branch_total:
                failures.append(
                    f"partial branch {path}:{line} ({branch_covered}/{branch_total})")

if failures:
    print("Reachable src/Api coverage is incomplete:", file=sys.stderr)
    for failure in failures[:100]:
        print("  " + failure, file=sys.stderr)
    if len(failures) > 100:
        print(f"  ... and {len(failures) - 100} more", file=sys.stderr)
    print("Add behavioral tests or a reviewed runtime/platform exclusion.", file=sys.stderr)
    raise SystemExit(1)

print(
    "Reachable src/Api coverage is complete "
    f"(lines={covered_lines}/{required_lines}, branches={covered_branches}/{required_branches}; "
    f"reviewed exclusions: lines={excluded_lines}, branch-lines={excluded_branch_lines}).")
