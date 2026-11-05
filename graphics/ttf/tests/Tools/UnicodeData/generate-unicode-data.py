#!/usr/bin/env python3

import argparse
from array import array
import hashlib
import json
import os
from pathlib import Path
import re
import struct
import sys
import tempfile
import zipfile


MAGIC = b"TTFUCD\0\0"
HEADER_FORMAT = ">8sHBBBB32sH"
SECTION_FORMAT = ">4sII"
HEADER_SIZE = struct.calcsize(HEADER_FORMAT)
SECTION_SIZE = struct.calcsize(SECTION_FORMAT)
MAX_CODE_POINT = 0x10FFFF
ENUMERATED_SECTION = 1
MAPPING_SECTION = 2
SEQUENCE_SECTION = 3


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    repository_root = script_directory.parents[2]
    parser = argparse.ArgumentParser(
        description="Generate deterministic compact UnicodeData.bin files.")
    parser.add_argument(
        "--manifest",
        type=Path,
        default=script_directory / "inputs.json")
    parser.add_argument(
        "--archive",
        type=Path,
        default=repository_root / "specs/unicode/UCD_17.0.0.zip")
    parser.add_argument("--output", type=Path, required=True)
    parser.add_argument(
        "--section",
        action="append",
        default=[],
        metavar="TAG=PATH",
        help="Add a precompiled binary section; may be repeated.")
    parser.add_argument(
        "--check",
        action="store_true",
        help="Verify that OUTPUT is byte-identical instead of writing it.")
    parser.add_argument(
        "--print-fingerprint",
        action="store_true",
        help="Print the verified runtime-input fingerprint.")
    return parser.parse_args()


def read_manifest(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read input manifest: " + str(error))

    require(manifest.get("formatVersion") == 1,
            "unsupported UnicodeData.bin format version")
    version = manifest.get("unicodeVersion")
    require(isinstance(version, list) and len(version) == 3,
            "unicodeVersion must contain three integers")
    for value in version:
        require(isinstance(value, int) and 0 <= value <= 255,
                "unicodeVersion values must be bytes")
    archive = manifest.get("archive")
    require(isinstance(archive, dict), "archive metadata is missing")
    require(is_sha256(archive.get("sha256")),
            "archive SHA-256 is invalid")
    inputs = manifest.get("inputs")
    require(isinstance(inputs, list) and inputs,
            "at least one pinned input is required")
    return manifest


def verify_inputs(manifest, archive_path):
    archive_bytes = read_bytes(archive_path, "Unicode archive")
    actual_archive_hash = hashlib.sha256(archive_bytes).hexdigest()
    expected_archive_hash = manifest["archive"]["sha256"]
    require(actual_archive_hash == expected_archive_hash,
            "Unicode archive SHA-256 does not match the manifest")
    verified = {}

    try:
        with zipfile.ZipFile(archive_path, "r") as archive:
            for entry in manifest["inputs"]:
                verify_input_entry(entry)
                path = entry["path"]
                require(path not in verified,
                        "duplicate input path in manifest: " + path)
                try:
                    data = archive.read(path)
                except KeyError:
                    fail("Unicode archive member is missing: " + path)
                require(len(data) == entry["size"],
                        "Unicode archive member size differs: " + path)
                actual_hash = hashlib.sha256(data).hexdigest()
                require(actual_hash == entry["sha256"],
                        "Unicode archive member SHA-256 differs: " + path)
                verified[path] = data
    except zipfile.BadZipFile as error:
        fail("Unicode archive is not a valid ZIP file: " + str(error))

    return verified


def verify_input_entry(entry):
    require(isinstance(entry, dict), "input entry must be an object")
    path = entry.get("path")
    require(isinstance(path, str) and path and not path.startswith("/"),
            "input path is invalid")
    require(".." not in Path(path).parts, "input path cannot contain '..'")
    require(entry.get("role") in ("runtime", "conformance"),
            "input role must be runtime or conformance: " + path)
    require(isinstance(entry.get("purpose"), str) and entry["purpose"],
            "input purpose is missing: " + path)
    require(isinstance(entry.get("size"), int) and entry["size"] >= 0,
            "input size is invalid: " + path)
    require(is_sha256(entry.get("sha256")),
            "input SHA-256 is invalid: " + path)


def calculate_fingerprint(manifest, verified):
    digest = hashlib.sha256()
    digest.update(b"ttf UnicodeData inputs\0")
    digest.update(bytes(manifest["unicodeVersion"]))
    runtime_entries = [
        entry for entry in manifest["inputs"]
        if entry["role"] == "runtime"
    ]
    runtime_entries.sort(key=lambda entry: entry["path"])

    for entry in runtime_entries:
        path_bytes = entry["path"].encode("utf-8")
        digest.update(struct.pack(">H", len(path_bytes)))
        digest.update(path_bytes)
        digest.update(verified[entry["path"]])

    return digest.digest()


def read_sections(section_arguments):
    sections = {}

    for value in section_arguments:
        tag, separator, path_value = value.partition("=")
        require(separator == "=" and path_value,
                "section must use TAG=PATH syntax")
        require(len(tag) == 4 and tag.isascii() and tag.isalnum() and
                tag.upper() == tag,
                "section tag must be four uppercase ASCII letters or digits")
        require(tag not in sections, "duplicate section tag: " + tag)
        sections[tag] = read_bytes(Path(path_value), "section " + tag)

    return sections


def compile_runtime_sections(verified):
    script_aliases = read_value_aliases(
        verified["PropertyValueAliases.txt"], "sc")
    joining_aliases = read_value_aliases(
        verified["PropertyValueAliases.txt"], "jt")
    bidi_aliases = read_value_aliases(
        verified["PropertyValueAliases.txt"], "bc")
    sections = {}
    sections["SCPT"] = compile_enumerated(
        verified["Scripts.txt"],
        None,
        lambda value: normalize_alias(value, script_aliases))
    sections["SCEX"] = compile_enumerated(
        verified["ScriptExtensions.txt"],
        None,
        lambda value: normalize_script_set(value, script_aliases),
        "")
    sections["BIDI"] = compile_enumerated(
        verified["extracted/DerivedBidiClass.txt"],
        None,
        lambda value: normalize_alias(value, bidi_aliases))
    sections["EAWD"] = compile_enumerated(
        verified["EastAsianWidth.txt"])
    sections["GCAT"] = compile_enumerated(
        verified["extracted/DerivedGeneralCategory.txt"],
        None,
        None,
        "Cn")
    sections["JONT"] = compile_enumerated(
        verified["extracted/DerivedJoiningType.txt"],
        None,
        lambda value: normalize_alias(value, joining_aliases))
    sections["JONG"] = compile_enumerated(
        verified["extracted/DerivedJoiningGroup.txt"])
    sections["BRKT"] = compile_mapping(
        verified["BidiBrackets.txt"], True)
    sections["MIRR"] = compile_mapping(
        verified["BidiMirroring.txt"], False)
    sections["GRPH"] = compile_enumerated(
        verified["auxiliary/GraphemeBreakProperty.txt"])
    sections["LINE"] = compile_enumerated(verified["LineBreak.txt"])
    sections["VERT"] = compile_enumerated(
        verified["VerticalOrientation.txt"])
    sections["INSC"] = compile_enumerated(
        verified["IndicSyllabicCategory.txt"])
    sections["INPC"] = compile_enumerated(
        verified["IndicPositionalCategory.txt"])
    sections["INCB"] = compile_enumerated(
        verified["DerivedCoreProperties.txt"], "InCB")
    sections["HSTY"] = compile_enumerated(
        verified["HangulSyllableType.txt"])
    combining_classes, decompositions = compile_unicode_data(
        verified["UnicodeData.txt"])
    sections["CCCL"] = combining_classes
    sections["DECO"] = decompositions
    normalization = verified["DerivedNormalizationProps.txt"]
    sections["NFCQ"] = compile_enumerated(
        normalization, "NFC_QC", normalize_quick_check)
    sections["NFDQ"] = compile_enumerated(
        normalization, "NFD_QC", normalize_quick_check)
    sections["NFKQ"] = compile_enumerated(
        normalization, "NFKC_QC", normalize_quick_check)
    sections["NFKD"] = compile_enumerated(
        normalization, "NFKD_QC", normalize_quick_check)
    sections["CEXC"] = compile_binary_property(
        normalization, "Full_Composition_Exclusion")
    sections["DIGN"] = compile_binary_property(
        verified["DerivedCoreProperties.txt"],
        "Default_Ignorable_Code_Point")
    sections["VSLC"] = compile_binary_property(
        verified["PropList.txt"], "Variation_Selector")
    sections["MCMK"] = compile_binary_property(
        verified["PropList.txt"], "Modifier_Combining_Mark")
    emoji = verified["emoji/emoji-data.txt"]
    sections["EMOJ"] = compile_binary_property(emoji, "Emoji")
    sections["EMPR"] = compile_binary_property(
        emoji, "Emoji_Presentation")
    sections["EMMD"] = compile_binary_property(
        emoji, "Emoji_Modifier")
    sections["EMMB"] = compile_binary_property(
        emoji, "Emoji_Modifier_Base")
    sections["EMCO"] = compile_binary_property(
        emoji, "Emoji_Component")
    sections["EXTP"] = compile_binary_property(
        emoji, "Extended_Pictographic")
    return sections


def read_value_aliases(data, property_name):
    aliases = {}
    for line in decode_text(data).splitlines():
        content = line.split("#", 1)[0].strip()
        if not content:
            continue
        fields = [field.strip() for field in content.split(";")]
        if fields[0].lower() != property_name.lower() or len(fields) < 3:
            continue
        short_name = fields[1]
        for value in fields[1:]:
            if value:
                aliases[value] = short_name
    return aliases


def normalize_alias(value, aliases):
    return aliases.get(value, value)


def normalize_script_set(value, aliases):
    if value == "<script>" or value == "":
        return ""
    normalized = [normalize_alias(item, aliases) for item in value.split()]
    normalized.sort()
    return " ".join(normalized)


def normalize_quick_check(value):
    values = {
        "Yes": "Y", "Y": "Y",
        "No": "N", "N": "N",
        "Maybe": "M", "M": "M",
    }
    require(value in values, "unknown normalization quick-check value")
    return values[value]


def compile_binary_property(data, property_name):
    return compile_enumerated(
        data,
        property_name,
        normalize_binary_value,
        "N",
        True)


def normalize_binary_value(value):
    return "N" if value in ("", "N", "No") else "Y"


def compile_enumerated(
        data,
        property_name=None,
        transform=None,
        implicit_default=None,
        binary_property=False):
    if transform is None:
        transform = lambda value: value
    assignments = []
    missing = []
    text = decode_text(data)

    for line in text.splitlines():
        missing_fields = parse_missing_fields(line)
        if missing_fields is not None:
            parsed = parse_property_fields(
                missing_fields, property_name, binary_property)
            if parsed is not None:
                start, end = parse_code_point_range(missing_fields[0])
                missing.append((start, end, transform(parsed)))
            continue
        content = line.split("#", 1)[0].strip()
        if not content:
            continue
        fields = [field.strip() for field in content.split(";")]
        parsed = parse_property_fields(fields, property_name, binary_property)
        if parsed is None:
            continue
        start, end = parse_code_point_range(fields[0])
        assignments.append((start, end, transform(parsed)))

    if implicit_default is not None:
        missing.insert(0, (0, MAX_CODE_POINT, transform(implicit_default)))
    require(missing or implicit_default is not None,
            "enumerated property has no @missing default")
    values = sorted({value for _, _, value in missing + assignments})
    require(len(values) <= 65535, "too many enumerated property values")
    value_ids = {value: index for index, value in enumerate(values)}
    code_points = array("H", [0]) * (MAX_CODE_POINT + 1)

    for start, end, value in missing:
        assign_range(code_points, start, end, value_ids[value])
    for start, end, value in assignments:
        assign_range(code_points, start, end, value_ids[value])

    default_value = most_common_value(code_points, len(values))
    ranges = compress_ranges(code_points, default_value)
    return encode_enumerated(values, default_value, ranges)


def parse_missing_fields(line):
    match = re.search(r"@missing:\s*([^#]+)", line)
    if match is None:
        return None
    return [field.strip() for field in match.group(1).split(";")]


def parse_property_fields(fields, property_name, binary_property):
    if len(fields) < 2:
        return None
    if property_name is None:
        return fields[1]
    if fields[1] != property_name:
        return None
    if binary_property:
        return "Yes"
    return fields[2] if len(fields) > 2 and fields[2] else "Yes"


def assign_range(code_points, start, end, value):
    require(0 <= start <= end <= MAX_CODE_POINT,
            "Unicode property range is invalid")
    code_points[start:end + 1] = array("H", [value]) * (end - start + 1)


def most_common_value(code_points, value_count):
    counts = [0] * value_count
    for value in code_points:
        counts[value] += 1
    return max(range(value_count), key=lambda value: counts[value])


def compress_ranges(code_points, default_value):
    ranges = []
    start = 0
    value = code_points[0]
    code_point = 1

    while code_point <= MAX_CODE_POINT:
        candidate = code_points[code_point]
        if candidate != value:
            if value != default_value:
                ranges.append((start, code_point - 1, value))
            start = code_point
            value = candidate
        code_point += 1
    if value != default_value:
        ranges.append((start, MAX_CODE_POINT, value))
    return ranges


def encode_enumerated(values, default_value, ranges):
    encoded_names = [value.encode("utf-8") for value in values]
    name_offsets = [0]
    names = bytearray()
    for value in encoded_names:
        names.extend(value)
        name_offsets.append(len(names))
    output = bytearray(struct.pack(
        ">BBHHHII",
        ENUMERATED_SECTION,
        0,
        len(values),
        default_value,
        0,
        len(ranges),
        len(names)))
    for offset in name_offsets:
        output.extend(struct.pack(">I", offset))
    output.extend(names)
    while len(output) % 4 != 0:
        output.append(0)
    for start, end, value in ranges:
        output.extend(struct.pack(">IIHH", start, end, value, 0))
    return bytes(output)


def compile_mapping(data, has_auxiliary):
    records = []
    for line in decode_text(data).splitlines():
        content = line.split("#", 1)[0].strip()
        if not content:
            continue
        fields = [field.strip() for field in content.split(";")]
        require(len(fields) >= 2, "Unicode mapping record is malformed")
        key = int(fields[0], 16)
        value = int(fields[1], 16)
        auxiliary = 0
        if has_auxiliary:
            require(len(fields) >= 3 and fields[2] in ("o", "c"),
                    "Unicode bracket type is invalid")
            auxiliary = 1 if fields[2] == "o" else 2
        records.append((key, value, auxiliary))
    records.sort()
    output = bytearray(struct.pack(">BBHI", MAPPING_SECTION, 0, 0,
                                   len(records)))
    previous = -1
    for key, value, auxiliary in records:
        require(key > previous and value <= MAX_CODE_POINT,
                "Unicode mapping keys are not unique and ordered")
        output.extend(struct.pack(">III", key, value, auxiliary))
        previous = key
    return bytes(output)


def compile_unicode_data(data):
    combining = array("H", [0]) * (MAX_CODE_POINT + 1)
    decompositions = []
    for line in decode_text(data).splitlines():
        if not line:
            continue
        fields = line.split(";")
        require(len(fields) == 15, "UnicodeData.txt record is malformed")
        code_point = int(fields[0], 16)
        combining[code_point] = int(fields[3])
        decomposition = fields[5].strip()
        if decomposition and not decomposition.startswith("<"):
            values = [int(value, 16) for value in decomposition.split()]
            decompositions.append((code_point, values))
    combining_values = [str(value) for value in sorted(set(combining))]
    value_ids = {int(value): index for index, value in enumerate(combining_values)}
    encoded_combining = array("H", [0]) * (MAX_CODE_POINT + 1)
    index = 0
    while index <= MAX_CODE_POINT:
        encoded_combining[index] = value_ids[combining[index]]
        index += 1
    default_value = value_ids[0]
    combining_section = encode_enumerated(
        combining_values,
        default_value,
        compress_ranges(encoded_combining, default_value))
    return combining_section, encode_sequences(decompositions)


def encode_sequences(records):
    records.sort()
    pool = []
    encoded_records = []
    previous = -1
    for key, values in records:
        require(key > previous and len(values) <= 65535,
                "Unicode sequence records are invalid")
        encoded_records.append((key, len(pool), len(values)))
        pool.extend(values)
        previous = key
    output = bytearray(struct.pack(
        ">BBHII", SEQUENCE_SECTION, 0, 0, len(records), len(pool)))
    for key, offset, length in encoded_records:
        output.extend(struct.pack(">IIHH", key, offset, length, 0))
    for value in pool:
        output.extend(struct.pack(">I", value))
    return bytes(output)


def parse_code_point_range(value):
    values = value.strip().split("..")
    start = int(values[0], 16)
    end = int(values[1], 16) if len(values) == 2 else start
    return start, end


def decode_text(data):
    try:
        return data.decode("utf-8")
    except UnicodeDecodeError as error:
        fail("Unicode input is not UTF-8: " + str(error))


def build_binary(manifest, fingerprint, sections):
    sorted_sections = sorted(sections.items())
    require(len(sorted_sections) <= 65535, "too many binary sections")
    directory_end = HEADER_SIZE + SECTION_SIZE * len(sorted_sections)
    payload_offset = align_four(directory_end)
    entries = []
    payload = bytearray()

    for tag, data in sorted_sections:
        offset = payload_offset + len(payload)
        require(offset <= 0xFFFFFFFF and len(data) <= 0xFFFFFFFF,
                "UnicodeData.bin exceeds 32-bit section bounds")
        entries.append((tag.encode("ascii"), offset, len(data)))
        payload.extend(data)
        while len(payload) % 4 != 0:
            payload.append(0)

    version = manifest["unicodeVersion"]
    output = bytearray(struct.pack(
        HEADER_FORMAT,
        MAGIC,
        manifest["formatVersion"],
        version[0],
        version[1],
        version[2],
        0,
        fingerprint,
        len(entries)))
    for entry in entries:
        output.extend(struct.pack(SECTION_FORMAT, *entry))
    while len(output) < payload_offset:
        output.append(0)
    output.extend(payload)
    return bytes(output)


def write_or_check(path, data, check):
    if check:
        existing = read_bytes(path, "existing output")
        require(existing == data, "UnicodeData.bin is not reproducible")
        return

    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix=path.name + ".",
        dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as temporary:
            temporary.write(data)
        os.replace(temporary_name, path)
    except BaseException:
        try:
            os.unlink(temporary_name)
        except FileNotFoundError:
            pass
        raise


def read_bytes(path, description):
    try:
        return path.read_bytes()
    except OSError as error:
        fail("cannot read " + description + ": " + str(error))


def align_four(value):
    return (value + 3) & ~3


def is_sha256(value):
    if not isinstance(value, str) or len(value) != 64:
        return False
    return all(character in "0123456789abcdef" for character in value)


def require(condition, message):
    if not condition:
        fail(message)


def fail(message):
    print("generate-unicode-data: " + message, file=sys.stderr)
    raise SystemExit(1)


def main():
    arguments = parse_arguments()
    manifest = read_manifest(arguments.manifest)
    verified = verify_inputs(manifest, arguments.archive)
    fingerprint = calculate_fingerprint(manifest, verified)
    sections = compile_runtime_sections(verified)
    supplied_sections = read_sections(arguments.section)
    for tag, data in supplied_sections.items():
        require(tag not in sections, "section tag is already compiled: " + tag)
        sections[tag] = data
    output = build_binary(manifest, fingerprint, sections)
    write_or_check(arguments.output, output, arguments.check)

    if arguments.print_fingerprint:
        print(fingerprint.hex())


if __name__ == "__main__":
    main()
