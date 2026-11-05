#!/usr/bin/env python3

import argparse
import hashlib
import json
import os
from pathlib import Path
import shutil
import subprocess
import sys
import tempfile


FONT_EXTENSIONS = (".otc", ".otf", ".ttc", ".ttf", ".woff", ".woff2")
FONT_SIGNATURES = (b"\x00\x01\x00\x00", b"OTTO", b"ttcf", b"wOFF", b"wOF2")
MAX_MESSAGE_BYTES = 64 * 1024


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Validate generated fonts with OpenType Sanitizer.")
    parser.add_argument(
        "--ots",
        help="ots-sanitize executable; defaults to OTS_SANITIZE or PATH lookup.")
    parser.add_argument(
        "--validator-manifest",
        type=Path,
        default=script_directory / "validator.json",
        help="Pinned OTS version and source metadata.")
    parser.add_argument(
        "--timeout",
        type=int,
        default=30,
        help="Per-font timeout in seconds (default: 30).")
    parser.add_argument(
        "--report",
        type=Path,
        help="Write a deterministic JSON report to this path.")
    parser.add_argument(
        "--regressions",
        type=Path,
        default=script_directory.parents[1] / "Regression" / "Fonts" /
        "manifest.json",
        help="Checksum-pinned regression manifest.")
    parser.add_argument(
        "--include-regressions",
        action="store_true",
        help="Validate expectations from the regression manifest.")
    parser.add_argument(
        "--allow-missing",
        action="store_true",
        help="Exit successfully with an explicit skip when OTS is unavailable.")
    parser.add_argument(
        "--allow-version-mismatch",
        action="store_true",
        help="Use an OTS version other than the pinned validation oracle.")
    parser.add_argument(
        "paths",
        nargs="*",
        type=Path,
        metavar="FONT_OR_DIRECTORY",
        help="Generated font files or directories; directories are recursive.")
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    require(arguments.timeout > 0, "--timeout must be positive")
    validator = read_validator_manifest(arguments.validator_manifest)
    cases = collect_generated_cases(arguments.paths)
    if arguments.include_regressions:
        cases.extend(read_regression_cases(arguments.regressions))
    require(cases, "no generated fonts or regression fixtures were selected")
    ensure_unique_cases(cases)

    executable = find_ots(arguments.ots)
    if executable is None:
        message = "OpenType Sanitizer was not found"
        if arguments.allow_missing:
            print("SKIP: " + message)
            return
        fail(message + "; install ots-sanitize or pass --ots")

    tool_version = read_tool_version(executable, arguments.timeout)
    if not arguments.allow_version_mismatch:
        require(validator["version"] in tool_version,
                "OpenType Sanitizer version differs from the pin: expected " +
                validator["version"] + ", found " + tool_version)
    results = []
    unexpected = 0
    for case in cases:
        result = validate_case(
            executable,
            case,
            arguments.timeout)
        results.append(result)
        if result["matchesExpectation"]:
            print("PASS " + case["id"] + " " + result["result"])
        else:
            print("FAIL " + case["id"] + " expected " +
                  case["expectedResult"] + " but was " + result["result"])
            unexpected += 1

    report = {
        "formatVersion": 1,
        "validator": validator["name"],
        "validatorCommit": validator["commit"],
        "executable": Path(executable).name,
        "version": tool_version,
        "results": results
    }
    if arguments.report is not None:
        write_report(arguments.report, report)
    if unexpected:
        fail(str(unexpected) + " sanitizer expectation(s) failed")


def read_validator_manifest(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read validator manifest: " + str(error))
    require(manifest.get("formatVersion") == 1,
            "unsupported validator manifest version")
    for field in ("name", "version", "release", "commit", "source",
                  "license"):
        require(isinstance(manifest.get(field), str) and manifest[field],
                "validator manifest field is missing: " + field)
    require(len(manifest["commit"]) == 40 and all(
        character in "0123456789abcdef" for character in manifest["commit"]),
        "validator commit is invalid")
    return manifest


def collect_generated_cases(paths):
    files = []
    for value in paths:
        path = value.resolve()
        require(path.exists(), "input does not exist: " + str(value))
        if path.is_file():
            require(path.suffix.lower() in FONT_EXTENSIONS,
                    "input is not a supported font: " + str(value))
            files.append(path)
        elif path.is_dir():
            for candidate in path.rglob("*"):
                if candidate.is_file() and \
                        candidate.suffix.lower() in FONT_EXTENSIONS:
                    files.append(candidate.resolve())
        else:
            fail("input is not a regular file or directory: " + str(value))

    unique = sorted(set(files), key=lambda value: str(value))
    return [{
        "id": "generated:" + path.name,
        "path": path,
        "expectedResult": "accept",
        "provenance": "generated font input"
    } for path in unique]


def read_regression_cases(manifest_path):
    try:
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read regression manifest: " + str(error))
    require(manifest.get("formatVersion") == 1,
            "unsupported regression manifest version")
    fixtures = manifest.get("fixtures")
    require(isinstance(fixtures, list),
            "regression fixtures must be an array")
    cases = []
    for fixture in fixtures:
        verify_regression_entry(fixture)
        relative = Path(fixture["file"])
        path = (manifest_path.parent / relative).resolve()
        require(path.parent == manifest_path.parent.resolve(),
                "regression fixture must be beside its manifest")
        verify_file(path, fixture["size"], fixture["sha256"])
        cases.append({
            "id": "regression:" + fixture["id"],
            "path": path,
            "expectedResult": fixture["expectedResult"],
            "provenance": fixture["provenance"]
        })
    return cases


def verify_regression_entry(fixture):
    require(isinstance(fixture, dict),
            "regression fixture must be an object")
    for field in ("id", "file", "sha256", "expectedResult",
                  "provenance", "minimization"):
        require(isinstance(fixture.get(field), str) and fixture[field],
                "regression field is missing: " + field)
    require(Path(fixture["file"]).name == fixture["file"],
            "regression file must be one filename")
    require(fixture["expectedResult"] in ("accept", "reject"),
            "regression expectedResult must be accept or reject")
    require(isinstance(fixture.get("size"), int) and fixture["size"] > 0,
            "regression size must be positive")
    require(is_sha256(fixture["sha256"]),
            "regression SHA-256 is invalid")


def ensure_unique_cases(cases):
    identifiers = set()
    paths = set()
    for case in cases:
        require(case["id"] not in identifiers,
                "duplicate validation case ID: " + case["id"])
        require(case["path"] not in paths,
                "font selected more than once: " + str(case["path"]))
        identifiers.add(case["id"])
        paths.add(case["path"])


def find_ots(explicit):
    candidate = explicit or os.environ.get("OTS_SANITIZE")
    if candidate:
        return shutil.which(candidate)
    return shutil.which("ots-sanitize")


def read_tool_version(executable, timeout):
    try:
        process = subprocess.run(
            [executable, "--version"],
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout,
            check=False,
            env=stable_environment())
    except (OSError, subprocess.TimeoutExpired) as error:
        fail("cannot execute OpenType Sanitizer: " + str(error))
    text = decode_message(process.stdout).strip()
    return text or "version unavailable"


def validate_case(executable, case, timeout):
    input_path = case["path"]
    with tempfile.TemporaryDirectory(prefix="ttf-ots-") as directory:
        output_path = Path(directory) / ("sanitized" + input_path.suffix)
        try:
            process = subprocess.run(
                [executable, str(input_path), str(output_path)],
                stdin=subprocess.DEVNULL,
                stdout=subprocess.PIPE,
                stderr=subprocess.STDOUT,
                timeout=timeout,
                check=False,
                env=stable_environment())
            accepted = process.returncode == 0 and \
                is_sanitized_font(output_path)
            result = "accept" if accepted else "reject"
            message = normalize_message(
                decode_message(process.stdout),
                input_path,
                output_path)
            exit_code = process.returncode
        except subprocess.TimeoutExpired as error:
            result = "timeout"
            message = normalize_message(
                decode_message(error.stdout),
                input_path,
                output_path)
            exit_code = None
        except OSError as error:
            result = "tool-error"
            message = str(error)
            exit_code = None

    return {
        "id": case["id"],
        "file": input_path.name,
        "expectedResult": case["expectedResult"],
        "result": result,
        "matchesExpectation": result == case["expectedResult"],
        "exitCode": exit_code,
        "message": message,
        "provenance": case["provenance"]
    }


def is_sanitized_font(path):
    try:
        with path.open("rb") as stream:
            return stream.read(4) in FONT_SIGNATURES
    except OSError:
        return False


def stable_environment():
    environment = os.environ.copy()
    environment["LC_ALL"] = "C"
    environment["LANG"] = "C"
    return environment


def decode_message(value):
    if value is None:
        return ""
    return value[:MAX_MESSAGE_BYTES].decode("utf-8", errors="replace")


def normalize_message(message, input_path, output_path):
    normalized = message.replace(str(input_path), "<input>")
    normalized = normalized.replace(str(output_path), "<output>")
    return normalized.strip()


def verify_file(path, expected_size, expected_hash):
    try:
        require(path.is_file(), "regression fixture is missing: " + str(path))
        require(path.stat().st_size == expected_size,
                "regression fixture size differs: " + str(path))
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
    except OSError as error:
        fail("cannot read regression fixture: " + str(error))
    require(digest == expected_hash,
            "regression fixture SHA-256 differs: " + str(path))


def write_report(path, report):
    data = (json.dumps(report, indent=2, sort_keys=True) + "\n").encode("utf-8")
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix="." + path.name + ".",
        dir=path.parent)
    try:
        with os.fdopen(descriptor, "wb") as output:
            output.write(data)
        os.replace(temporary_name, path)
    except BaseException:
        try:
            Path(temporary_name).unlink(missing_ok=True)
        except OSError:
            pass
        raise


def is_sha256(value):
    return isinstance(value, str) and len(value) == 64 and all(
        character in "0123456789abcdef" for character in value)


def require(condition, message):
    if not condition:
        raise ValidationError(message)


def fail(message):
    print("error: " + message, file=sys.stderr)
    raise SystemExit(1)


class ValidationError(Exception):
    pass


if __name__ == "__main__":
    try:
        main()
    except ValidationError as error:
        fail(str(error))
