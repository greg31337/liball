#!/usr/bin/env python3
"""Generate deterministic policy-compatible C# from the pinned GS1 dictionary."""

from __future__ import annotations

import argparse
import hashlib
import re
import sys
from dataclasses import dataclass
from pathlib import Path


SNAPSHOT_DATE = "2026-08-26"
SNAPSHOT_SHA256 = "1ea02f4e3b81d3092e095987600e01c7fd55310f4da5181f5a070262ff6ffc94"
DEFAULT_SOURCE = "specs/GS1-Syntax-Dictionary.txt"
DEFAULT_OUTPUT = "src/Internal/Gs1/Gs1AiTable.cs"
ATTRIBUTE_PREFIXES = ("req=", "ex=", "dlpkey")
COMPONENT_PATTERN = re.compile(r"^([NXYZ])(?:(\d+)|\.\.(\d+))$")
AI_PATTERN = re.compile(r"^\d{2,4}(?:-\d{2,4})?$")
ASSOCIATION_PATTERN = re.compile(r"^\d{2,4}|\d{1,3}n{1,3}$")
LINTER_PATTERN = re.compile(r"^[a-z][a-z0-9]*$")


class TransformError(Exception):
    """Raised when the pinned source cannot be transformed losslessly."""


@dataclass(frozen=True)
class Component:
    character_set: str
    minimum_length: int
    maximum_length: int
    optional: bool
    linters: tuple[str, ...]


@dataclass(frozen=True)
class SourceEntry:
    ai_start: str
    ai_end: str
    wildcard_pattern: str
    predefined_length: bool
    digital_link_attribute: bool
    components: tuple[Component, ...]
    required_groups: tuple[tuple[str, ...], ...]
    exclusions: tuple[str, ...]
    digital_link_primary_key: bool
    qualifier_sequences: tuple[tuple[str, ...], ...]
    title: str
    source_line: int


@dataclass(frozen=True)
class ExpandedEntry:
    ai: str
    source: SourceEntry
    definition_index: int


@dataclass
class PrefixTrieNode:
    children: list[int]
    definition_index: int = -1


def repository_root() -> Path:
    return Path(__file__).resolve().parents[2]


def source_digest(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def split_title(line: str) -> tuple[str, str]:
    if "#" not in line:
        return line.rstrip(), ""
    body, title = line.split("#", 1)
    return body.rstrip(), title.strip()


def is_attribute(token: str) -> bool:
    return token.startswith(ATTRIBUTE_PREFIXES)


def parse_component(token: str, line_number: int) -> Component:
    optional = token.startswith("[")
    normalized = token
    if optional:
        normalized = normalized[1:]
        closing = normalized.find("]")
        if closing < 0:
            raise TransformError(
                f"line {line_number}: optional component is missing ']'"
            )
        normalized = normalized[:closing] + normalized[closing + 1 :]
    elif "]" in normalized:
        raise TransformError(
            f"line {line_number}: component has an unmatched ']'"
        )

    parts = normalized.split(",")
    match = COMPONENT_PATTERN.fullmatch(parts[0])
    if match is None:
        raise TransformError(
            f"line {line_number}: invalid component type {parts[0]!r}"
        )

    if match.group(2) is not None:
        minimum_length = int(match.group(2))
        maximum_length = minimum_length
    else:
        minimum_length = 1
        maximum_length = int(match.group(3))
    if maximum_length < minimum_length:
        raise TransformError(
            f"line {line_number}: component length is invalid"
        )

    linters: list[str] = []
    for linter in parts[1:]:
        if LINTER_PATTERN.fullmatch(linter) is None:
            raise TransformError(
                f"line {line_number}: invalid linter name {linter!r}"
            )
        linters.append(linter)
    return Component(
        character_set=match.group(1),
        minimum_length=minimum_length,
        maximum_length=maximum_length,
        optional=optional,
        linters=tuple(linters),
    )


def validate_association_pattern(pattern: str, line_number: int) -> str:
    if ASSOCIATION_PATTERN.fullmatch(pattern) is None:
        raise TransformError(
            f"line {line_number}: invalid association pattern {pattern!r}"
        )
    return pattern


def parse_required_groups(
    value: str, line_number: int
) -> tuple[tuple[str, ...], ...]:
    groups: list[tuple[str, ...]] = []
    for alternative in value.split(","):
        members = tuple(
            validate_association_pattern(item, line_number)
            for item in alternative.split("+")
        )
        if not members:
            raise TransformError(
                f"line {line_number}: required association is empty"
            )
        groups.append(members)
    return tuple(groups)


def parse_exclusions(value: str, line_number: int) -> tuple[str, ...]:
    return tuple(
        validate_association_pattern(item, line_number)
        for item in value.split(",")
    )


def parse_qualifier_sequences(
    value: str, line_number: int
) -> tuple[tuple[str, ...], ...]:
    sequences: list[tuple[str, ...]] = []
    for alternative in value.split("|"):
        sequence = tuple(
            validate_association_pattern(item, line_number)
            for item in alternative.split(",")
        )
        if not sequence:
            raise TransformError(
                f"line {line_number}: qualifier sequence is empty"
            )
        sequences.append(sequence)
    return tuple(sequences)


def parse_ai_range(token: str, line_number: int) -> tuple[str, str, str]:
    if AI_PATTERN.fullmatch(token) is None:
        raise TransformError(f"line {line_number}: invalid AI or AI range {token!r}")
    if "-" not in token:
        return token, token, token

    start, end = token.split("-", 1)
    if len(start) != len(end) or int(start) > int(end):
        raise TransformError(f"line {line_number}: invalid AI range {token!r}")
    differing = [index for index in range(len(start)) if start[index] != end[index]]
    if not differing:
        raise TransformError(f"line {line_number}: empty AI range {token!r}")
    first_difference = differing[0]
    if start[:first_difference] != end[:first_difference]:
        raise TransformError(f"line {line_number}: non-prefix AI range {token!r}")
    wildcard = start[:first_difference] + ("n" * (len(start) - first_difference))
    return start, end, wildcard


def parse_entry(line: str, line_number: int) -> SourceEntry:
    body, title = split_title(line)
    tokens = body.split()
    if not tokens:
        raise TransformError(f"line {line_number}: dictionary entry is empty")
    ai_start, ai_end, wildcard = parse_ai_range(tokens[0], line_number)
    cursor = 1
    flags = ""
    if cursor < len(tokens) and set(tokens[cursor]).issubset({"*", "?"}):
        flags = tokens[cursor]
        cursor += 1
    if len(flags) != len(set(flags)):
        raise TransformError(f"line {line_number}: repeated entry flag")

    component_tokens: list[str] = []
    while cursor < len(tokens) and not is_attribute(tokens[cursor]):
        component_tokens.append(tokens[cursor])
        cursor += 1
    if not component_tokens:
        raise TransformError(f"line {line_number}: entry has no components")
    components = tuple(
        parse_component(token, line_number) for token in component_tokens
    )
    variable_components = sum(
        component.minimum_length != component.maximum_length
        for component in components
    )
    if variable_components > 1:
        raise TransformError(
            f"line {line_number}: entry has multiple variable-length components"
        )
    if variable_components == 1 and components[-1].minimum_length == components[-1].maximum_length:
        raise TransformError(
            f"line {line_number}: variable-length component is not final"
        )
    optional_seen = False
    for component in components:
        if component.optional:
            optional_seen = True
        elif optional_seen:
            raise TransformError(
                f"line {line_number}: mandatory component follows an optional component"
            )

    required_groups: list[tuple[str, ...]] = []
    exclusions: list[str] = []
    digital_link_primary_key = False
    qualifier_sequences: list[tuple[str, ...]] = []
    while cursor < len(tokens):
        token = tokens[cursor]
        if token.startswith("req="):
            required_groups.extend(
                parse_required_groups(token.removeprefix("req="), line_number)
            )
        elif token.startswith("ex="):
            exclusions.extend(
                parse_exclusions(token.removeprefix("ex="), line_number)
            )
        elif token == "dlpkey":
            digital_link_primary_key = True
        elif token.startswith("dlpkey="):
            digital_link_primary_key = True
            qualifier_sequences.extend(
                parse_qualifier_sequences(
                    token.removeprefix("dlpkey="), line_number
                )
            )
        else:
            raise TransformError(
                f"line {line_number}: unknown attribute {token!r}"
            )
        cursor += 1

    return SourceEntry(
        ai_start=ai_start,
        ai_end=ai_end,
        wildcard_pattern=wildcard,
        predefined_length="*" in flags,
        digital_link_attribute="?" in flags,
        components=components,
        required_groups=tuple(required_groups),
        exclusions=tuple(exclusions),
        digital_link_primary_key=digital_link_primary_key,
        qualifier_sequences=tuple(qualifier_sequences),
        title=title,
        source_line=line_number,
    )


def parse_dictionary(text: str) -> list[SourceEntry]:
    entries: list[SourceEntry] = []
    for line_number, line in enumerate(text.splitlines(), start=1):
        stripped = line.strip()
        if not stripped or stripped.startswith("#"):
            continue
        entries.append(parse_entry(line, line_number))
    if not entries:
        raise TransformError("dictionary contains no entries")
    return entries


def expand_entries(entries: list[SourceEntry]) -> list[ExpandedEntry]:
    expanded: list[ExpandedEntry] = []
    seen: dict[str, int] = {}
    for definition_index, entry in enumerate(entries):
        width = len(entry.ai_start)
        start = int(entry.ai_start)
        end = int(entry.ai_end)
        for value in range(start, end + 1):
            ai = f"{value:0{width}d}"
            if ai in seen:
                raise TransformError(
                    f"line {entry.source_line}: AI {ai} duplicates line {seen[ai]}"
                )
            seen[ai] = entry.source_line
            expanded.append(
                ExpandedEntry(
                    ai=ai,
                    source=entry,
                    definition_index=definition_index,
                )
            )
    expanded.sort(key=lambda item: item.ai)
    return expanded


def build_prefix_trie(expanded: list[ExpandedEntry]) -> list[PrefixTrieNode]:
    nodes = [PrefixTrieNode(children=[-1] * 10)]
    for entry in expanded:
        node_index = 0
        for character in entry.ai:
            digit = ord(character) - ord("0")
            child_index = nodes[node_index].children[digit]
            if child_index < 0:
                child_index = len(nodes)
                nodes[node_index].children[digit] = child_index
                nodes.append(PrefixTrieNode(children=[-1] * 10))
            node_index = child_index
        if nodes[node_index].definition_index >= 0:
            raise TransformError(f"AI {entry.ai} appears twice in the prefix trie")
        nodes[node_index].definition_index = entry.definition_index
    return nodes


def csharp_string(value: str) -> str:
    escaped = (
        value.replace("\\", "\\\\")
        .replace('"', '\\"')
        .replace("\r", "\\r")
        .replace("\n", "\\n")
        .replace("\t", "\\t")
    )
    return f'"{escaped}"'


def csharp_bool(value: bool) -> str:
    return "true" if value else "false"


def emit_string_array(values: tuple[str, ...], indent: str) -> list[str]:
    if not values:
        return [f"{indent}new string[0]"]
    lines = [f"{indent}new string[]", f"{indent}{{"]
    for value in values:
        lines.append(f"{indent}    {csharp_string(value)},")
    lines.append(f"{indent}}}")
    return lines


def emit_jagged_string_array(
    values: tuple[tuple[str, ...], ...], indent: str
) -> list[str]:
    if not values:
        return [f"{indent}new string[0][]"]
    lines = [f"{indent}new string[][]", f"{indent}{{"]
    for value in values:
        inner = ", ".join(csharp_string(item) for item in value)
        lines.append(f"{indent}    new string[] {{ {inner} }},")
    lines.append(f"{indent}}}")
    return lines


def emit_components(
    components: tuple[Component, ...], indent: str
) -> list[str]:
    lines = [
        f"{indent}new Gs1AiComponentDefinition[]",
        f"{indent}{{",
    ]
    for component in components:
        lines.extend(
            [
                f"{indent}    new Gs1AiComponentDefinition",
                f"{indent}    {{",
                f"{indent}        CharacterSet = {csharp_string(component.character_set)},",
                f"{indent}        MinimumLength = {component.minimum_length},",
                f"{indent}        MaximumLength = {component.maximum_length},",
                f"{indent}        Optional = {csharp_bool(component.optional)},",
                f"{indent}        Linters =",
            ]
        )
        linter_lines = emit_string_array(component.linters, indent + "            ")
        linter_lines[-1] += ","
        lines.extend(linter_lines)
        lines.append(f"{indent}    }},")
    lines.append(f"{indent}}}")
    return lines


def emit_assignment(
    lines: list[str], name: str, value_lines: list[str], indent: str
) -> None:
    lines.append(f"{indent}{name} =")
    value_lines[-1] += ","
    lines.extend(value_lines)


def emit_integer_array(values: list[int], indent: str) -> list[str]:
    if not values:
        return [f"{indent}new int[0]"]
    lines = [f"{indent}new int[]", f"{indent}{{"]
    offset = 0
    while offset < len(values):
        chunk = values[offset : offset + 10]
        text = ", ".join(str(value) for value in chunk)
        lines.append(f"{indent}    {text},")
        offset += len(chunk)
    lines.append(f"{indent}}}")
    return lines


def generate_csharp(
    source_entries: list[SourceEntry], expanded_entries: list[ExpandedEntry]
) -> str:
    trie = build_prefix_trie(expanded_entries)
    lines = [
        "namespace barcode;",
        "",
        "internal class Gs1AiComponentDefinition",
        "{",
        '    internal string CharacterSet = "";',
        "    internal int MinimumLength;",
        "    internal int MaximumLength;",
        "    internal bool Optional;",
        "    internal string[] Linters = new string[0];",
        "}",
        "",
        "internal class Gs1AiDefinition",
        "{",
        '    internal string AiStart = "";',
        '    internal string AiEnd = "";',
        '    internal string WildcardPattern = "";',
        "    internal bool PredefinedLength;",
        "    internal bool DigitalLinkDataAttribute;",
        "    internal Gs1AiComponentDefinition[] Components =",
        "        new Gs1AiComponentDefinition[0];",
        "    internal string[][] RequiredAssociations = new string[0][];",
        "    internal string[] Exclusions = new string[0];",
        "    internal bool DigitalLinkPrimaryKey;",
        "    internal string[][] DigitalLinkQualifierSequences =",
        "        new string[0][];",
        '    internal string Title = "";',
        "    internal int SourceLine;",
        "}",
        "",
        "internal static class Gs1AiTable",
        "{",
        f"    internal const string SnapshotDate = {csharp_string(SNAPSHOT_DATE)};",
        f"    internal const string SnapshotSha256 = {csharp_string(SNAPSHOT_SHA256)};",
        f"    internal const int SourceEntryCount = {len(source_entries)};",
        f"    internal const int ExpandedEntryCount = {len(expanded_entries)};",
        f"    internal const int TrieNodeCount = {len(trie)};",
        "    internal const int TrieRadix = 10;",
        "    internal static readonly Gs1AiDefinition[] Definitions =",
        "        new Gs1AiDefinition[]",
        "        {",
    ]
    for source in source_entries:
        indent = "            "
        field_indent = "                "
        lines.extend(
            [
                f"{indent}new Gs1AiDefinition",
                f"{indent}{{",
                f"{field_indent}AiStart = {csharp_string(source.ai_start)},",
                f"{field_indent}AiEnd = {csharp_string(source.ai_end)},",
                f"{field_indent}WildcardPattern = {csharp_string(source.wildcard_pattern)},",
                f"{field_indent}PredefinedLength = {csharp_bool(source.predefined_length)},",
                f"{field_indent}DigitalLinkDataAttribute = {csharp_bool(source.digital_link_attribute)},",
            ]
        )
        emit_assignment(
            lines,
            "Components",
            emit_components(source.components, field_indent + "    "),
            field_indent,
        )
        emit_assignment(
            lines,
            "RequiredAssociations",
            emit_jagged_string_array(
                source.required_groups, field_indent + "    "
            ),
            field_indent,
        )
        emit_assignment(
            lines,
            "Exclusions",
            emit_string_array(source.exclusions, field_indent + "    "),
            field_indent,
        )
        lines.extend(
            [
                f"{field_indent}DigitalLinkPrimaryKey = {csharp_bool(source.digital_link_primary_key)},",
            ]
        )
        emit_assignment(
            lines,
            "DigitalLinkQualifierSequences",
            emit_jagged_string_array(
                source.qualifier_sequences, field_indent + "    "
            ),
            field_indent,
        )
        lines.extend(
            [
                f"{field_indent}Title = {csharp_string(source.title)},",
                f"{field_indent}SourceLine = {source.source_line},",
                f"{indent}}},",
            ]
        )
    lines.append("        };")
    definition_indices = [node.definition_index for node in trie]
    child_indices: list[int] = []
    for node in trie:
        child_indices.extend(node.children)
    emit_assignment(
        lines,
        "internal static readonly int[] TrieDefinitionIndices",
        emit_integer_array(definition_indices, "        "),
        "    ",
    )
    lines[-1] = lines[-1][:-1] + ";"
    emit_assignment(
        lines,
        "internal static readonly int[] TrieChildIndices",
        emit_integer_array(child_indices, "        "),
        "    ",
    )
    lines[-1] = lines[-1][:-1] + ";"
    lines.extend(["}", ""])
    return "\n".join(lines)


def load_and_transform(source: Path, expected_sha256: str) -> tuple[str, str]:
    data = source.read_bytes()
    digest = source_digest(data)
    if digest != expected_sha256:
        raise TransformError(
            f"source SHA-256 is {digest}, expected {expected_sha256}; "
            "update the pinned snapshot and generator together"
        )
    try:
        text = data.decode("utf-8")
    except UnicodeDecodeError as error:
        raise TransformError("dictionary is not valid UTF-8") from error
    source_entries = parse_dictionary(text)
    expanded_entries = expand_entries(source_entries)
    trie = build_prefix_trie(expanded_entries)
    generated = generate_csharp(source_entries, expanded_entries)
    summary = (
        f"source_entries={len(source_entries)} "
        f"expanded_entries={len(expanded_entries)} "
        f"trie_nodes={len(trie)} "
        f"sha256={digest}"
    )
    return generated, summary


def write_output(output: Path, generated: str) -> None:
    output.parent.mkdir(parents=True, exist_ok=True)
    with output.open("w", encoding="utf-8", newline="\n") as stream:
        stream.write(generated)


def check_output(output: Path, generated: str) -> None:
    if not output.exists():
        raise TransformError(f"generated output does not exist: {output}")
    existing = output.read_text(encoding="utf-8")
    if existing != generated:
        raise TransformError(
            f"generated output is stale: rerun with --output {output}"
        )


def parse_arguments(arguments: list[str]) -> argparse.Namespace:
    root = repository_root()
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument(
        "--source",
        type=Path,
        default=root / DEFAULT_SOURCE,
        help="checked-in GS1 syntax dictionary",
    )
    parser.add_argument(
        "--output",
        type=Path,
        default=None,
        help=f"C# output path (future canonical path: {DEFAULT_OUTPUT})",
    )
    parser.add_argument(
        "--check",
        action="store_true",
        help="compare generated content with --output without writing",
    )
    parser.add_argument(
        "--expected-sha256",
        default=SNAPSHOT_SHA256,
        help="required source digest; update deliberately with a new snapshot",
    )
    return parser.parse_args(arguments)


def main(arguments: list[str]) -> int:
    options = parse_arguments(arguments)
    if options.check and options.output is None:
        print("error: --check requires --output", file=sys.stderr)
        return 2
    try:
        generated, summary = load_and_transform(
            options.source.resolve(), options.expected_sha256
        )
        if options.check:
            check_output(options.output.resolve(), generated)
        elif options.output is not None:
            write_output(options.output.resolve(), generated)
        print(summary)
        return 0
    except (OSError, TransformError) as error:
        print(f"error: {error}", file=sys.stderr)
        return 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv[1:]))
