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


MAX_MESSAGE_BYTES = 64 * 1024
PDF_SIGNATURE = b"%PDF-"
TOOL_NAMES = ("pdfbox", "qpdf", "verapdf")


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Consume and validate PDFs with independent tools.")
    parser.add_argument(
        "--tools",
        type=Path,
        default=script_directory / "tools.json",
        help="Pinned and tested tool metadata.")
    parser.add_argument(
        "--use",
        default=",".join(TOOL_NAMES),
        help="Comma-separated tools: pdfbox,qpdf,verapdf (default: all).")
    parser.add_argument("--java", help="Java executable for PDFBox.")
    parser.add_argument(
        "--pdfbox-jar",
        type=Path,
        help="Pinned pdfbox-app jar; defaults to PDFBOX_APP_JAR.")
    parser.add_argument("--qpdf", help="qpdf executable; defaults to QPDF or PATH.")
    parser.add_argument(
        "--verapdf",
        help="veraPDF executable; defaults to VERAPDF or PATH.")
    parser.add_argument(
        "--verapdf-flavour",
        default="0",
        help="veraPDF profile code (default: 0, automatic detection).")
    parser.add_argument(
        "--verapdf-expectation",
        choices=("any", "compliant", "noncompliant"),
        default="any",
        help="Required veraPDF result (default: report either result).")
    parser.add_argument(
        "--allow-missing",
        action="store_true",
        help="Report unavailable requested tools as skips instead of failing.")
    parser.add_argument(
        "--allow-version-mismatch",
        action="store_true",
        help="Permit qpdf/veraPDF versions other than the tested versions.")
    parser.add_argument(
        "--allow-qpdf-warnings",
        action="store_true",
        help="Accept qpdf exit status 3 while retaining warning diagnostics.")
    parser.add_argument(
        "--timeout",
        type=int,
        default=60,
        help="Per-tool timeout in seconds (default: 60).")
    parser.add_argument(
        "--report",
        type=Path,
        help="Write a deterministic JSON report to this path.")
    parser.add_argument(
        "paths",
        nargs="+",
        type=Path,
        metavar="PDF_OR_DIRECTORY",
        help="PDF files or directories; directories are recursive.")
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    require(arguments.timeout > 0, "--timeout must be positive")
    selected = parse_tool_selection(arguments.use)
    manifest = read_manifest(arguments.tools)
    pdfs = collect_pdfs(arguments.paths)
    adapters = resolve_adapters(arguments, selected, manifest)

    results = []
    failures = 0
    for pdf in pdfs:
        pdf_result = {
            "file": file_identity(pdf),
            "tools": []
        }
        for name in selected:
            adapter = adapters.get(name)
            if adapter is None:
                result = {
                    "tool": name,
                    "status": "skipped",
                    "message": "tool is unavailable"
                }
            else:
                result = run_adapter(name, adapter, pdf, arguments)
            pdf_result["tools"].append(result)
            print(result["status"].upper() + " " + name + " " + pdf.name)
            if result["status"] == "failed":
                failures += 1
        results.append(pdf_result)

    report = {
        "formatVersion": 1,
        "selectedTools": list(selected),
        "results": results
    }
    if arguments.report is not None:
        write_report(arguments.report, report)
    if failures:
        fail(str(failures) + " independent PDF check(s) failed")


def parse_tool_selection(value):
    selected = []
    for part in value.split(","):
        name = part.strip().lower()
        require(name in TOOL_NAMES, "unknown tool in --use: " + name)
        require(name not in selected, "duplicate tool in --use: " + name)
        selected.append(name)
    require(selected, "--use must select at least one tool")
    return tuple(selected)


def read_manifest(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read tool manifest: " + str(error))
    require(manifest.get("formatVersion") == 1,
            "unsupported tool manifest version")
    for tool in TOOL_NAMES:
        require(isinstance(manifest.get(tool), dict),
                "tool metadata is missing: " + tool)
    pdfbox = manifest["pdfbox"]
    require(isinstance(pdfbox.get("version"), str) and pdfbox["version"],
            "PDFBox version is missing")
    checksum = pdfbox.get("sha512")
    require(isinstance(checksum, str) and len(checksum) == 128 and all(
        character in "0123456789abcdef" for character in checksum),
        "PDFBox SHA-512 is invalid")
    for tool in ("qpdf", "verapdf"):
        version = manifest[tool].get("testedVersion")
        require(isinstance(version, str) and version,
                tool + " tested version is missing")
    return manifest


def collect_pdfs(paths):
    files = []
    for value in paths:
        path = value.resolve()
        require(path.exists(), "input does not exist: " + str(value))
        if path.is_file():
            require(path.suffix.lower() == ".pdf",
                    "input is not a PDF: " + str(value))
            files.append(path)
        elif path.is_dir():
            files.extend(candidate.resolve() for candidate in path.rglob("*")
                         if candidate.is_file() and
                         candidate.suffix.lower() == ".pdf")
        else:
            fail("input is not a regular file or directory: " + str(value))
    unique = sorted(set(files), key=lambda path: str(path))
    require(unique, "no PDFs were selected")
    for path in unique:
        verify_pdf(path)
    return unique


def verify_pdf(path):
    try:
        with path.open("rb") as stream:
            signature = stream.read(len(PDF_SIGNATURE))
    except OSError as error:
        fail("cannot read PDF: " + str(error))
    require(signature == PDF_SIGNATURE,
            "file has no PDF signature: " + str(path))


def resolve_adapters(arguments, selected, manifest):
    adapters = {}
    missing = []
    if "pdfbox" in selected:
        adapter = resolve_pdfbox(arguments, manifest["pdfbox"])
        if adapter is None:
            missing.append("pdfbox (set PDFBOX_APP_JAR and install Java)")
        else:
            adapters["pdfbox"] = adapter
    if "qpdf" in selected:
        executable = find_executable(arguments.qpdf, "QPDF", "qpdf")
        if executable is None:
            missing.append("qpdf")
        else:
            version = command_version(
                executable,
                ["--version"],
                arguments.timeout,
                arguments.allow_missing)
            if version is None:
                missing.append("qpdf (executable could not run)")
                executable = None
        if executable is not None:
            require_tested_version(
                "qpdf", version, manifest["qpdf"]["testedVersion"], arguments)
            adapters["qpdf"] = {"executable": executable, "version": version}
    if "verapdf" in selected:
        executable = find_executable(arguments.verapdf, "VERAPDF", "verapdf")
        if executable is None:
            missing.append("veraPDF")
        else:
            version = command_version(
                executable,
                ["--version"],
                arguments.timeout,
                arguments.allow_missing)
            if version is None:
                missing.append("veraPDF (executable could not run)")
                executable = None
        if executable is not None:
            require_tested_version(
                "veraPDF", version, manifest["verapdf"]["testedVersion"], arguments)
            adapters["verapdf"] = {"executable": executable, "version": version}
    if missing and not arguments.allow_missing:
        fail("requested tools are unavailable: " + ", ".join(missing))
    return adapters


def resolve_pdfbox(arguments, metadata):
    jar_value = arguments.pdfbox_jar
    if jar_value is None:
        environment_value = os.environ.get("PDFBOX_APP_JAR")
        jar_value = Path(environment_value) if environment_value else None
    if jar_value is None:
        return None
    jar = jar_value.resolve()
    require(jar.is_file(), "PDFBox jar does not exist: " + str(jar_value))
    require(sha512(jar) == metadata["sha512"],
            "PDFBox jar does not match the pinned SHA-512")
    java = find_executable(arguments.java, "JAVA", "java")
    if java is None:
        return None
    version = command_version(
        java, ["-version"], arguments.timeout, arguments.allow_missing)
    if version is None:
        return None
    pdfbox_version = command_version(
        java,
        ["-Djava.awt.headless=true", "-jar", str(jar),
         "export:text", "--version"],
        arguments.timeout,
        arguments.allow_missing)
    if pdfbox_version is None:
        return None
    require(metadata["version"] in pdfbox_version,
            "PDFBox version differs from the pin: expected " +
            metadata["version"] + ", found " + pdfbox_version)
    return {
        "java": java,
        "javaVersion": version,
        "jar": jar,
        "version": pdfbox_version
    }


def find_executable(explicit, environment_name, default):
    candidate = explicit or os.environ.get(environment_name) or default
    return shutil.which(candidate)


def command_version(executable, arguments, timeout, allow_failure=False):
    try:
        process = subprocess.run(
            [executable] + arguments,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            timeout=timeout,
            check=False,
            env=stable_environment())
    except (OSError, subprocess.TimeoutExpired) as error:
        if allow_failure:
            return None
        fail("cannot query tool version: " + str(error))
    message = decode_message(process.stdout).strip()
    if process.returncode != 0:
        if allow_failure:
            return None
        fail("tool version command failed: " + (message or executable))
    return message or "version unavailable"


def require_tested_version(name, actual, expected, arguments):
    if not arguments.allow_version_mismatch:
        require(expected in actual,
                name + " version differs from the tested version: expected " +
                expected + ", found " + actual)


def run_adapter(name, adapter, pdf, arguments):
    if name == "pdfbox":
        return run_pdfbox(adapter, pdf, arguments.timeout)
    if name == "qpdf":
        return run_qpdf(
            adapter, pdf, arguments.timeout, arguments.allow_qpdf_warnings)
    return run_verapdf(
        adapter,
        pdf,
        arguments.timeout,
        arguments.verapdf_flavour,
        arguments.verapdf_expectation)


def run_pdfbox(adapter, pdf, timeout):
    with tempfile.TemporaryDirectory(prefix="ttf-pdfbox-") as directory:
        output = Path(directory) / "extracted.txt"
        command = [
            adapter["java"],
            "-Djava.awt.headless=true",
            "-jar",
            str(adapter["jar"]),
            "export:text",
            "-encoding=UTF-8",
            "-i=" + str(pdf),
            "-o=" + str(output)
        ]
        process, message = run_command(command, timeout)
        message = normalize_paths(message, {
            str(pdf): "<pdf>",
            str(output): "<text>",
            str(adapter["jar"]): "<pdfbox>"
        })
        permission_denied = "You do not have permission to extract text" in message
        if process.returncode != 0 or not output.is_file() or permission_denied:
            return failed_result("pdfbox", adapter["version"], message)
        try:
            text_bytes = output.read_bytes()
            text_bytes.decode("utf-8")
        except (OSError, UnicodeDecodeError) as error:
            return failed_result("pdfbox", adapter["version"], str(error))
        return {
            "tool": "pdfbox",
            "status": "passed",
            "version": adapter["version"],
            "textBytes": len(text_bytes),
            "textSha256": hashlib.sha256(text_bytes).hexdigest(),
            "message": message
        }


def run_qpdf(adapter, pdf, timeout, allow_warnings):
    process, message = run_command(
        [adapter["executable"], "--check", str(pdf)], timeout)
    accepted = process.returncode == 0 or \
        (allow_warnings and process.returncode == 3)
    return {
        "tool": "qpdf",
        "status": "passed" if accepted else "failed",
        "version": adapter["version"],
        "exitCode": process.returncode,
        "warningsAccepted": process.returncode == 3 and allow_warnings,
        "message": normalize_paths(message, {str(pdf): "<pdf>"})
    }


def run_verapdf(adapter, pdf, timeout, flavour, expectation):
    process, message = run_command([
        adapter["executable"],
        "--format", "json",
        "--flavour", flavour,
        str(pdf)
    ], timeout, merge_output=False)
    message = normalize_paths(message, {str(pdf): "<pdf>"})
    if process.returncode != 0:
        return failed_result("verapdf", adapter["version"], message)
    try:
        report = json.loads(decode_message(process.stdout))
    except json.JSONDecodeError as error:
        return failed_result(
            "verapdf", adapter["version"], "invalid JSON report: " + str(error))
    values = find_values(report, "isCompliant")
    if len(values) != 1:
        return failed_result(
            "verapdf",
            adapter["version"],
            "expected one isCompliant value, found " + str(len(values)))
    compliant = normalize_boolean(values[0])
    if compliant is None:
        return failed_result(
            "verapdf", adapter["version"], "invalid isCompliant value")
    expected = expectation == "any" or \
        (expectation == "compliant" and compliant) or \
        (expectation == "noncompliant" and not compliant)
    return {
        "tool": "verapdf",
        "status": "passed" if expected else "failed",
        "version": adapter["version"],
        "flavour": flavour,
        "compliant": compliant,
        "expectation": expectation,
        "message": message
    }


def run_command(command, timeout, merge_output=True):
    stderr = subprocess.STDOUT if merge_output else subprocess.PIPE
    try:
        process = subprocess.run(
            command,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=stderr,
            timeout=timeout,
            check=False,
            env=stable_environment())
    except (OSError, subprocess.TimeoutExpired) as error:
        class FailedProcess:
            returncode = -1
            stdout = b""
            stderr = b""
        return FailedProcess(), str(error)
    output = process.stdout
    if not merge_output:
        output = (process.stderr or b"")
    return process, normalize_message(decode_message(output))


def failed_result(tool, version, message):
    return {
        "tool": tool,
        "status": "failed",
        "version": version,
        "message": normalize_message(message)
    }


def find_values(value, key):
    found = []
    if isinstance(value, dict):
        for name, child in value.items():
            if name == key:
                found.append(child)
            found.extend(find_values(child, key))
    elif isinstance(value, list):
        for child in value:
            found.extend(find_values(child, key))
    return found


def normalize_boolean(value):
    if isinstance(value, bool):
        return value
    if isinstance(value, str) and value.lower() in ("true", "false"):
        return value.lower() == "true"
    return None


def file_identity(path):
    return {
        "name": path.name,
        "size": path.stat().st_size,
        "sha256": sha256(path)
    }


def sha256(path):
    return file_digest(path, hashlib.sha256())


def sha512(path):
    return file_digest(path, hashlib.sha512())


def file_digest(path, digest):
    try:
        with path.open("rb") as stream:
            while True:
                chunk = stream.read(1024 * 1024)
                if not chunk:
                    break
                digest.update(chunk)
    except OSError as error:
        fail("cannot hash file: " + str(error))
    return digest.hexdigest()


def write_report(path, report):
    output = path.resolve()
    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = output.with_name("." + output.name + ".tmp")
    try:
        temporary.write_text(
            json.dumps(report, indent=2, sort_keys=True) + "\n",
            encoding="utf-8")
        temporary.replace(output)
    except OSError as error:
        temporary.unlink(missing_ok=True)
        fail("cannot write report: " + str(error))


def decode_message(value):
    return value[:MAX_MESSAGE_BYTES].decode("utf-8", errors="replace")


def normalize_message(value):
    return "\n".join(line.rstrip() for line in value.strip().splitlines())


def normalize_paths(value, replacements):
    normalized = value
    for path, replacement in replacements.items():
        normalized = normalized.replace(path, replacement)
    return normalize_message(normalized)


def stable_environment():
    environment = os.environ.copy()
    environment["LC_ALL"] = "C"
    environment["LANG"] = "C"
    environment["TZ"] = "UTC"
    return environment


def require(condition, message):
    if not condition:
        fail(message)


def fail(message):
    print("ERROR: " + message, file=sys.stderr)
    raise SystemExit(1)


if __name__ == "__main__":
    main()
