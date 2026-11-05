#!/usr/bin/env python3

import argparse
import hashlib
import json
from pathlib import Path
import ssl
import sys
import tempfile
import urllib.error
import urllib.request


CHUNK_SIZE = 1024 * 1024


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Download the checksum-pinned PDFBox command-line app.")
    parser.add_argument(
        "--tools",
        type=Path,
        default=script_directory / "tools.json",
        help="Tool metadata and checksum manifest.")
    parser.add_argument(
        "--output",
        type=Path,
        required=True,
        help="Destination for the verified PDFBox app jar.")
    parser.add_argument(
        "--timeout",
        type=int,
        default=60,
        help="Network timeout in seconds (default: 60).")
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    require(arguments.timeout > 0, "--timeout must be positive")
    metadata = read_pdfbox_metadata(arguments.tools)
    output = arguments.output.resolve()

    if output.exists():
        require(output.is_file(), "output is not a regular file: " + str(output))
        verify_checksum(output, metadata["sha512"])
        print("READY " + str(output))
        return

    output.parent.mkdir(parents=True, exist_ok=True)
    temporary = None
    try:
        with tempfile.NamedTemporaryFile(
                prefix="." + output.name + ".",
                suffix=".download",
                dir=output.parent,
                delete=False) as stream:
            temporary = Path(stream.name)
            download(metadata["url"], stream, arguments.timeout)
        verify_checksum(temporary, metadata["sha512"])
        temporary.replace(output)
        temporary = None
    finally:
        if temporary is not None:
            temporary.unlink(missing_ok=True)
    print("DOWNLOADED " + str(output))


def read_pdfbox_metadata(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read tool manifest: " + str(error))
    require(manifest.get("formatVersion") == 1,
            "unsupported tool manifest version")
    metadata = manifest.get("pdfbox")
    require(isinstance(metadata, dict), "PDFBox metadata is missing")
    for field in ("name", "version", "file", "url", "sha512", "license"):
        require(isinstance(metadata.get(field), str) and metadata[field],
                "PDFBox metadata field is missing: " + field)
    checksum = metadata["sha512"]
    require(len(checksum) == 128 and all(
        character in "0123456789abcdef" for character in checksum),
        "PDFBox SHA-512 is invalid")
    require(metadata["url"].startswith("https://archive.apache.org/"),
            "PDFBox URL must use the Apache HTTPS archive")
    return metadata


def download(url, stream, timeout):
    request = urllib.request.Request(
        url,
        headers={"User-Agent": "ttf-test-pdfbox-downloader/1"})
    try:
        with urllib.request.urlopen(
                request,
                timeout=timeout,
                context=ssl.create_default_context()) as response:
            while True:
                chunk = response.read(CHUNK_SIZE)
                if not chunk:
                    break
                stream.write(chunk)
    except (OSError, urllib.error.URLError) as error:
        fail("cannot download PDFBox: " + str(error))


def verify_checksum(path, expected):
    digest = hashlib.sha512()
    try:
        with path.open("rb") as stream:
            while True:
                chunk = stream.read(CHUNK_SIZE)
                if not chunk:
                    break
                digest.update(chunk)
    except OSError as error:
        fail("cannot read PDFBox jar: " + str(error))
    require(digest.hexdigest() == expected,
            "PDFBox SHA-512 mismatch: " + str(path))


def require(condition, message):
    if not condition:
        fail(message)


def fail(message):
    print("ERROR: " + message, file=sys.stderr)
    raise SystemExit(1)


if __name__ == "__main__":
    main()
