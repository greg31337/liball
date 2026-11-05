#!/usr/bin/env python3
"""Offline, deterministic rendering and verification for bundled data."""

import argparse
import csv
import hashlib
import json
import re
import sys
import tempfile
import xml.etree.ElementTree as element_tree
from pathlib import Path


REPOSITORY = Path(__file__).resolve().parents[2]
DATA_DIRECTORY = REPOSITORY / "src" / "Data"
DEFAULT_SNAPSHOT_DIRECTORY = REPOSITORY / "specs"
MANIFEST_PATH = DATA_DIRECTORY / "manifest.json"
GENERATED_PATHS = {
    "currencies": "standards/currencies.json",
    "iban-formats": "standards/iban-formats.json",
    "invariant": "invariant/core.json",
    "language-tags": "standards/language-tags.json",
    "media-types": "standards/media-types.json",
    "ports": "standards/ports.json",
    "safe-ranges": "standards/safe-ranges.json",
    "tlds": "standards/tlds.json",
    "time-zones": "standards/time-zones.json",
}


def sha256(path):
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        while True:
            block = stream.read(1024 * 1024)
            if not block:
                break
            digest.update(block)
    return digest.hexdigest()


def json_bytes(value, compact=False):
    if compact:
        rendered = json.dumps(value, ensure_ascii=False, separators=(",", ":"))
    else:
        rendered = json.dumps(value, ensure_ascii=False, indent=2)
    return (rendered + "\n").encode("utf-8")


def compact_safe_literals(rendered):
    replacements = {
        '[\n    "example.com",\n    "example.net",\n    "example.org"\n  ]': '["example.com", "example.net", "example.org"]',
        '[\n    "192.0.2.0/24",\n    "198.51.100.0/24",\n    "203.0.113.0/24"\n  ]': '["192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24"]',
        '[\n    "2001:db8::/32"\n  ]': '["2001:db8::/32"]',
        '{\n      "exchange": "555",\n      "first": "0100",\n      "last": "0199"\n    }': '{"exchange": "555", "first": "0100", "last": "0199"}',
        '[\n        "example.com",\n        "example.net",\n        "example.org"\n      ]': '["example.com", "example.net", "example.org"]',
        '[\n        "192.0.2.0/24",\n        "198.51.100.0/24",\n        "203.0.113.0/24"\n      ]': '["192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24"]',
        '{\n        "exchange": "555",\n        "first": "0100",\n        "last": "0199"\n      }': '{"exchange": "555", "first": "0100", "last": "0199"}',
    }
    for expanded, compacted in replacements.items():
        rendered = rendered.replace(expanded, compacted)
    return rendered


def source_path(snapshot_directory, name):
    path = snapshot_directory / name
    if not path.is_file():
        raise ValueError("required reviewed snapshot is missing: " + str(path))
    return path


def parse_language_record(lines):
    values = {}
    previous = None
    for line in lines:
        if line.startswith(" ") and previous is not None:
            values[previous][-1] = values[previous][-1] + " " + line.strip()
            continue
        if ":" not in line:
            continue
        name, value = line.split(":", 1)
        previous = name
        values.setdefault(name, []).append(value.strip())
    return values


def build_language_tags(snapshot_directory, retrieved_on, reviewer):
    del retrieved_on
    del reviewer
    path = source_path(snapshot_directory, "iana-language-subtag-registry.txt")
    sections = path.read_text(encoding="utf-8").split("%%")
    header = parse_language_record(sections[0].splitlines())
    records = []
    for section in sections[1:]:
        fields = parse_language_record(section.strip().splitlines())
        if "Type" not in fields:
            continue
        value = fields.get("Subtag", fields.get("Tag", [""]))[0]
        records.append({
            "type": fields["Type"][0],
            "value": value,
            "preferredValue": fields.get("Preferred-Value", [""])[0],
            "prefixes": fields.get("Prefix", []),
            "suppressScript": fields.get("Suppress-Script", [""])[0],
            "macrolanguage": fields.get("Macrolanguage", [""])[0],
            "scope": fields.get("Scope", [""])[0],
            "deprecated": fields.get("Deprecated", [""])[0],
        })
    result = {
        "fileDate": header["File-Date"][0],
        "source": "IANA Language Subtag Registry",
        "license": "CC0-1.0",
        "records": records,
    }
    return json_bytes(result, compact=True)


def build_time_zones(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "iana-time-zones-zone1970.tab")
    records = []
    annotation = None
    for line in path.read_text(encoding="utf-8").splitlines():
        match = re.match(r"# From .+ \((\d{4}-\d{2}-\d{2})\):", line)
        if match:
            annotation = match.group(1)
        if not line or line.startswith("#"):
            continue
        columns = line.split("\t")
        record = {
            "countryCodes": columns[0].split(","),
            "coordinates": columns[1],
            "zone": columns[2],
        }
        if len(columns) > 3 and columns[3]:
            record["comment"] = columns[3]
        records.append(record)
    if annotation is None:
        raise ValueError("zone1970.tab does not contain a dated upstream annotation")
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "IANA Time Zone Database zone1970.tab",
            "url": "https://data.iana.org/time-zones/tzdb/zone1970.tab",
            "version": "upstream annotation " + annotation,
            "retrievedOn": retrieved_on,
            "license": "public-domain",
            "snapshot": "specs/iana-time-zones-zone1970.tab",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def element_text(element):
    return "".join(element.itertext()).strip()


def build_media_types(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "iana-media-types.xml")
    root = element_tree.parse(str(path)).getroot()
    namespace = "{http://www.iana.org/assignments}"
    known_types = {
        "application", "audio", "font", "haptics", "image", "message",
        "model", "multipart", "text", "video",
    }
    records = []
    for registry in root.findall(namespace + "registry"):
        media_type = registry.get("id")
        if media_type not in known_types:
            continue
        for source_record in registry.findall(namespace + "record"):
            name_element = source_record.find(namespace + "name")
            if name_element is None:
                continue
            subtype = name_element.text or ""
            if not subtype:
                continue
            record = {
                "type": media_type,
                "subtype": subtype,
                "mediaType": media_type + "/" + subtype,
            }
            references = []
            for reference in source_record.findall(namespace + "xref"):
                if reference.get("type") == "rfc" and reference.get("data"):
                    references.append(reference.get("data"))
            if references:
                record["references"] = sorted(references)
            records.append(record)
    records.sort(key=lambda value: (value["type"], value["subtype"]))
    updated = root.find(namespace + "updated")
    if updated is None or not element_text(updated):
        raise ValueError("media-types.xml does not contain an updated date")
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "IANA Media Types registry",
            "url": "https://www.iana.org/assignments/media-types/media-types.xml",
            "version": "updated " + element_text(updated),
            "retrievedOn": retrieved_on,
            "license": "CC0-1.0",
            "snapshot": "specs/iana-media-types.xml",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def build_currencies(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "iso-4217-current-currencies.xml")
    root = element_tree.parse(str(path)).getroot()
    published = root.get("Pblshd", "")
    if root.tag != "ISO_4217" or not re.fullmatch(r"\d{4}-\d{2}-\d{2}", published):
        raise ValueError("ISO 4217 snapshot does not contain a valid publication date")
    table = root.find("CcyTbl")
    if table is None:
        raise ValueError("ISO 4217 snapshot does not contain a currency table")
    by_code = {}
    for item in table.findall("CcyNtry"):
        entity = element_text(item.find("CtryNm")) if item.find("CtryNm") is not None else ""
        name = element_text(item.find("CcyNm")) if item.find("CcyNm") is not None else ""
        code = element_text(item.find("Ccy")) if item.find("Ccy") is not None else ""
        numeric = element_text(item.find("CcyNbr")) if item.find("CcyNbr") is not None else ""
        minor_text = element_text(item.find("CcyMnrUnts")) if item.find("CcyMnrUnts") is not None else ""
        if not entity or not name:
            raise ValueError("ISO 4217 snapshot contains an unnamed entity or currency")
        if not code:
            if numeric or minor_text:
                raise ValueError("ISO 4217 entry without a code contains numeric metadata")
            continue
        if not re.fullmatch(r"[A-Z]{3}", code):
            raise ValueError("ISO 4217 snapshot contains an invalid alphabetic code: " + code)
        if not re.fullmatch(r"\d{3}", numeric):
            raise ValueError("ISO 4217 snapshot contains an invalid numeric code for " + code)
        if minor_text == "N.A.":
            minor_units = None
        elif re.fullmatch(r"\d", minor_text):
            minor_units = int(minor_text)
        else:
            raise ValueError("ISO 4217 snapshot contains invalid minor units for " + code)
        if code not in by_code:
            by_code[code] = {
                "code": code,
                "numericCode": numeric,
                "minorUnits": minor_units,
                "name": name,
                "entities": [],
            }
        record = by_code[code]
        if (record["numericCode"] != numeric or
                record["minorUnits"] != minor_units or
                record["name"] != name):
            raise ValueError("ISO 4217 snapshot contains conflicting records for " + code)
        if entity not in record["entities"]:
            record["entities"].append(entity)
    records = [by_code[code] for code in sorted(by_code)]
    if not records:
        raise ValueError("ISO 4217 snapshot contains no coded currency records")
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "SIX ISO 4217 List One",
            "url": "https://www.six-group.com/en/products-services/financial-information/market-reference-data/data-standards.html",
            "version": "published " + published,
            "retrievedOn": retrieved_on,
            "license": "factual-registry-extract",
            "snapshot": "specs/iso-4217-current-currencies.xml",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def iban_position(positions, name, bban_length, country):
    value = positions.get(name)
    if value is None:
        return None
    if (not isinstance(value, list) or len(value) != 2 or
            not all(isinstance(item, int) for item in value)):
        raise ValueError("IBAN snapshot contains an invalid " + name + " position for " + country)
    if value[0] < 0 or value[1] < value[0] or value[1] > bban_length:
        raise ValueError("IBAN snapshot contains an out-of-range " + name + " position for " + country)
    return value


def build_iban_formats(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "schwifty-2026.7.3-iban-registry.json")
    source = json.loads(path.read_text(encoding="utf-8"))
    if not isinstance(source, dict) or not source:
        raise ValueError("schwifty IBAN snapshot contains no country records")
    records = []
    for country in sorted(source):
        item = source[country]
        if not re.fullmatch(r"[A-Z]{2}", country) or item.get("country") != country:
            raise ValueError("IBAN snapshot contains an invalid country code: " + country)
        bban_pattern = item.get("bban_spec", "")
        components = re.findall(r"(\d+)!([anc])", bban_pattern)
        if not components or "".join(count + "!" + kind for count, kind in components) != bban_pattern:
            raise ValueError("IBAN snapshot contains an invalid BBAN pattern for " + country)
        bban_length = item.get("bban_length")
        iban_length = item.get("iban_length")
        if (not isinstance(bban_length, int) or
                sum(int(count) for count, kind in components) != bban_length):
            raise ValueError("IBAN snapshot contains an invalid BBAN length for " + country)
        if (not isinstance(iban_length, int) or iban_length != bban_length + 4 or
                iban_length < 5 or iban_length > 34):
            raise ValueError("IBAN snapshot contains an invalid IBAN length for " + country)
        iban_spec = item.get("iban_spec", "")
        iban_prefix = iban_spec[:2]
        if (not re.fullmatch(r"[A-Z]{2}", iban_prefix) or
                iban_spec != iban_prefix + "2!n" + bban_pattern):
            raise ValueError("IBAN snapshot contains an inconsistent IBAN pattern for " + country)
        positions = item.get("positions", {})
        if not isinstance(positions, dict):
            raise ValueError("IBAN snapshot contains invalid component positions for " + country)
        bank = iban_position(positions, "bank_code", bban_length, country)
        branch = iban_position(positions, "branch_code", bban_length, country)
        account = iban_position(positions, "account_code", bban_length, country)
        record = {
            "countryCode": country,
            "ibanPrefix": iban_prefix,
            "ibanLength": iban_length,
            "bbanPattern": bban_pattern,
            "bbanLength": bban_length,
            "sepa": item.get("in_sepa_zone") is True,
            "bankCodeStart": bank[0] if bank is not None else 0,
            "bankCodeLength": bank[1] - bank[0] if bank is not None else 0,
            "branchCodeStart": branch[0] if branch is not None else 0,
            "branchCodeLength": branch[1] - branch[0] if branch is not None else 0,
            "accountCodeStart": account[0] if account is not None else 0,
            "accountCodeLength": account[1] - account[0] if account is not None else 0,
        }
        records.append(record)
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "schwifty IBAN registry transcription",
            "url": "https://github.com/mdomke/schwifty/tree/2026.7.3/schwifty/iban_registry",
            "version": "schwifty 2026.7.3",
            "upstream": "SWIFT ISO 13616 IBAN Registry",
            "retrievedOn": retrieved_on,
            "license": "MIT",
            "snapshot": "specs/schwifty-2026.7.3-iban-registry.json",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def port_state(service, description):
    if description == "Reserved":
        return "reserved"
    if description == "Unassigned":
        return "unassigned"
    if service:
        return "assigned"
    return "unspecified"


def build_ports(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "iana-service-names-port-numbers.csv")
    records = []
    with path.open("r", encoding="utf-8-sig", newline="") as stream:
        for row in csv.DictReader(stream):
            service = row["Service Name"].strip()
            port = row["Port Number"].strip()
            if not service and not port:
                continue
            record = {
                "service": service,
                "port": port,
                "transport": row["Transport Protocol"].strip(),
                "state": port_state(service, row["Description"].strip()),
            }
            modified = row["Modification Date"].strip()
            if modified:
                record["modified"] = modified
            records.append(record)
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "IANA Service Name and Transport Protocol Port Number Registry",
            "url": "https://www.iana.org/assignments/service-names-port-numbers/service-names-port-numbers.csv",
            "version": "snapshot retrieved " + retrieved_on,
            "retrievedOn": retrieved_on,
            "license": "CC0-1.0",
            "snapshot": "specs/iana-service-names-port-numbers.csv",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def build_tlds(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    path = source_path(snapshot_directory, "iana-tlds-alpha-by-domain.txt")
    lines = path.read_text(encoding="ascii").splitlines()
    if not lines:
        raise ValueError("IANA TLD snapshot is empty")
    version = re.fullmatch(r"# Version (\d+), Last Updated .+", lines[0])
    if version is None:
        raise ValueError("IANA TLD snapshot does not contain a version header")
    records = []
    seen = set()
    for line in lines[1:]:
        value = line.strip()
        if not value or value.startswith("#"):
            continue
        if not re.fullmatch(r"[A-Z0-9](?:[A-Z0-9-]{0,61}[A-Z0-9])?", value):
            raise ValueError("IANA TLD snapshot contains an invalid label: " + value)
        normalized = value.lower()
        if normalized in seen:
            raise ValueError("IANA TLD snapshot contains a duplicate label: " + normalized)
        seen.add(normalized)
        records.append(normalized)
    records.sort()
    if not records:
        raise ValueError("IANA TLD snapshot contains no records")
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "source": {
            "name": "IANA Root Zone TLD list",
            "url": "https://data.iana.org/TLD/tlds-alpha-by-domain.txt",
            "version": "serial " + version.group(1),
            "retrievedOn": retrieved_on,
            "license": "factual-registry-extract",
            "snapshot": "specs/iana-tlds-alpha-by-domain.txt",
            "snapshotSha256": sha256(path),
        },
        "records": records,
    }
    return json_bytes(result)


def safe_sources(snapshot_directory, retrieved_on):
    definitions = [
        ("rfc-2606", "IETF RFC 2606 reserved DNS names", "https://www.rfc-editor.org/rfc/rfc2606", "RFC 2606, June 1999", "IETF-Trust", "rfc2606-reserved-dns-names.txt"),
        ("rfc-5737", "IETF RFC 5737 IPv4 documentation address blocks", "https://www.rfc-editor.org/rfc/rfc5737", "RFC 5737, January 2010", "IETF-Trust", "rfc5737-ipv4-documentation-ranges.txt"),
        ("rfc-3849", "IETF RFC 3849 IPv6 documentation prefix", "https://www.rfc-editor.org/rfc/rfc3849", "RFC 3849, July 2004", "IETF-Trust", "rfc3849-ipv6-documentation-prefix.txt"),
        ("nanpa-555", "NANPA 555 line-number reservation", "https://www.nanpa.com/numbering/555-line-numbers", "page retrieved " + retrieved_on, "factual-reservation", "nanpa-555-reserved-numbers.html"),
    ]
    sources = []
    for identifier, name, url, version, license_name, filename in definitions:
        path = source_path(snapshot_directory, filename)
        sources.append({
            "id": identifier,
            "name": name,
            "url": url,
            "version": version,
            "retrievedOn": retrieved_on,
            "license": license_name,
            "snapshot": "specs/" + filename,
            "snapshotSha256": sha256(path),
        })
    return sources


def build_safe_ranges(snapshot_directory, retrieved_on, reviewer):
    del reviewer
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "sources": safe_sources(snapshot_directory, retrieved_on),
        "dnsDocumentationDomains": ["example.com", "example.net", "example.org"],
        "ipv4DocumentationPrefixes": ["192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24"],
        "ipv6DocumentationPrefixes": ["2001:db8::/32"],
        "nanpaFictionalSubscriberRanges": [
            {"exchange": "555", "first": "0100", "last": "0199"},
        ],
    }
    rendered = json_bytes(result).decode("utf-8")
    return compact_safe_literals(rendered).encode("utf-8")


def build_invariant(snapshot_directory, retrieved_on, reviewer):
    sources = safe_sources(snapshot_directory, retrieved_on)
    provenance = []
    for source in sources:
        source_version = source["version"]
        if source["id"] == "nanpa-555":
            source_version = "NANPA " + source_version
        provenance.append({
            "id": source["id"],
            "source": source["name"],
            "sourceVersion": source_version,
            "license": source["license"],
            "reviewer": reviewer,
        })
    result = {
        "schemaVersion": 1,
        "dataVersion": "1",
        "locale": "invariant",
        "fallbacks": [],
        "reviewStatus": "standards-reviewed",
        "provenance": provenance,
        "fields": [
            {
                "name": "internet.domain.documentation",
                "provenance": "rfc-2606",
                "value": ["example.com", "example.net", "example.org"],
            },
            {
                "name": "internet.ipv4.documentation-prefixes",
                "provenance": "rfc-5737",
                "value": ["192.0.2.0/24", "198.51.100.0/24", "203.0.113.0/24"],
            },
            {
                "name": "internet.ipv6.documentation-prefix",
                "provenance": "rfc-3849",
                "value": "2001:db8::/32",
            },
            {
                "name": "phone.nanpa.fictional-subscriber-range",
                "provenance": "nanpa-555",
                "value": {"exchange": "555", "first": "0100", "last": "0199"},
            },
        ],
    }
    rendered = json_bytes(result).decode("utf-8")
    return compact_safe_literals(rendered).encode("utf-8")


BUILDERS = {
    "currencies": build_currencies,
    "iban-formats": build_iban_formats,
    "invariant": build_invariant,
    "language-tags": build_language_tags,
    "media-types": build_media_types,
    "ports": build_ports,
    "safe-ranges": build_safe_ranges,
    "tlds": build_tlds,
    "time-zones": build_time_zones,
}


def manifest():
    with MANIFEST_PATH.open("r", encoding="utf-8") as stream:
        return json.load(stream)


def manifest_records():
    return {record["path"]: record for record in manifest()["resources"]}


def verify_manifest():
    errors = []
    records = manifest_records()
    shipped = {
        str(path.relative_to(DATA_DIRECTORY))
        for path in DATA_DIRECTORY.rglob("*.json")
        if path != MANIFEST_PATH
    }
    recorded = set(records)
    for missing in sorted(shipped - recorded):
        errors.append("shipped JSON has no manifest record: " + missing)
    for missing in sorted(recorded - shipped):
        errors.append("manifest resource is missing: " + missing)
    for relative, record in sorted(records.items()):
        resource = DATA_DIRECTORY / relative
        if resource.is_file() and sha256(resource) != record.get("resourceSha256"):
            errors.append("resource hash mismatch: " + relative)
        snapshots = []
        if "sourceSnapshot" in record:
            snapshots.append({
                "path": record["sourceSnapshot"],
                "sha256": record.get("sourceSnapshotSha256"),
            })
        snapshots.extend(record.get("sourceSnapshots", []))
        for snapshot in snapshots:
            snapshot_path = REPOSITORY / snapshot["path"]
            if not snapshot_path.is_file():
                errors.append("manifest snapshot is missing: " + snapshot["path"])
            elif sha256(snapshot_path) != snapshot.get("sha256"):
                errors.append("snapshot hash mismatch: " + snapshot["path"])
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print("manifest and snapshot hashes verified for " + str(len(records)) + " resources")
    return 0


def selected_datasets(name):
    if name == "all":
        return sorted(BUILDERS)
    return [name]


def ensure_staging_output(output_directory):
    resolved = output_directory.resolve()
    data_resolved = DATA_DIRECTORY.resolve()
    if resolved == data_resolved or data_resolved in resolved.parents:
        raise ValueError("render output must be a staging directory outside src/Data")


def render(name, snapshot_directory, output_directory, retrieved_on, reviewer):
    ensure_staging_output(output_directory)
    datasets = selected_datasets(name)
    if "invariant" in datasets and not reviewer:
        raise ValueError("invariant rendering requires --reviewer")
    for dataset in datasets:
        relative = GENERATED_PATHS[dataset]
        destination = output_directory / relative
        if destination.exists():
            raise ValueError("refusing to overwrite staging candidate: " + str(destination))
        destination.parent.mkdir(parents=True, exist_ok=True)
        destination.write_bytes(BUILDERS[dataset](snapshot_directory, retrieved_on, reviewer))
        print(str(destination))


def check_generated():
    records = manifest_records()
    errors = []
    with tempfile.TemporaryDirectory(prefix="fake-data-refresh-") as temporary:
        output = Path(temporary)
        for dataset in sorted(BUILDERS):
            relative = GENERATED_PATHS[dataset]
            record = records.get(relative)
            if record is None:
                errors.append("generated path has no manifest record: " + relative)
                continue
            rendered = BUILDERS[dataset](
                DEFAULT_SNAPSHOT_DIRECTORY,
                record["retrievedOn"],
                record["reviewer"],
            )
            committed = (DATA_DIRECTORY / relative).read_bytes()
            if rendered != committed:
                candidate = output / relative
                candidate.parent.mkdir(parents=True, exist_ok=True)
                candidate.write_bytes(rendered)
                errors.append("generated resource differs: " + relative)
    if errors:
        for error in errors:
            print(error, file=sys.stderr)
        return 1
    print("deterministic rendering verified for " + str(len(BUILDERS)) + " resources")
    return 0


def valid_date(value):
    if not re.fullmatch(r"\d{4}-\d{2}-\d{2}", value):
        raise argparse.ArgumentTypeError("date must use YYYY-MM-DD")
    return value


def arguments():
    parser = argparse.ArgumentParser(description=__doc__)
    commands = parser.add_subparsers(dest="command", required=True)
    commands.add_parser("verify", help="verify manifest coverage and committed hashes")
    commands.add_parser("check-generated", help="re-render approved data and compare bytes")
    render_parser = commands.add_parser("render", help="render reviewed snapshots into staging")
    render_parser.add_argument("dataset", choices=["all"] + sorted(BUILDERS))
    render_parser.add_argument("--snapshot-directory", type=Path, default=DEFAULT_SNAPSHOT_DIRECTORY)
    render_parser.add_argument("--output-directory", type=Path, required=True)
    render_parser.add_argument("--retrieved-on", type=valid_date, required=True)
    render_parser.add_argument("--reviewer", help="required reviewer identifier for invariant data")
    return parser.parse_args()


def main():
    options = arguments()
    try:
        if options.command == "verify":
            return verify_manifest()
        if options.command == "check-generated":
            return check_generated()
        render(
            options.dataset,
            options.snapshot_directory,
            options.output_directory,
            options.retrieved_on,
            options.reviewer,
        )
        return 0
    except (KeyError, OSError, ValueError, element_tree.ParseError) as error:
        print("refresh failed: " + str(error), file=sys.stderr)
        return 1


if __name__ == "__main__":
    sys.exit(main())
