#!/usr/bin/env python3

import argparse
import hashlib
import json
import os
from pathlib import Path
import string
import sys
import tempfile
import urllib.error
import urllib.parse
import urllib.request
import zipfile


CHUNK_SIZE = 64 * 1024
FONT_SIGNATURES = (b"\x00\x01\x00\x00", b"OTTO", b"ttcf", b"wOFF", b"wOF2")


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Explicitly download content-pinned test fonts.")
    parser.add_argument(
        "--manifest",
        type=Path,
        default=script_directory / "sources.json")
    actions = parser.add_mutually_exclusive_group()
    actions.add_argument(
        "--list",
        action="store_true",
        help="Validate and list sources without network or filesystem writes.")
    actions.add_argument(
        "--verify",
        action="store_true",
        help="Verify selected files already present in CACHE without network access.")
    actions.add_argument(
        "--download",
        action="store_true",
        help="Explicitly allow network downloads for missing or invalid selected files.")
    parser.add_argument(
        "--cache",
        type=Path,
        help="Destination directory; required for --verify and --download.")
    parser.add_argument(
        "ids",
        nargs="*",
        metavar="ID",
        help="Fixture IDs to process; omit to select every fixture.")
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    manifest = read_manifest(arguments.manifest)
    fixtures = select_fixtures(manifest["fixtures"], arguments.ids)

    if not arguments.verify and not arguments.download:
        list_fixtures(fixtures)
        return

    require(arguments.cache is not None,
            "--cache is required with --verify or --download")
    cache = arguments.cache.resolve()

    if arguments.verify:
        verify_cache(cache, fixtures)
        return

    cache.mkdir(parents=True, exist_ok=True)
    for fixture in fixtures:
        download_fixture(cache, fixture)


def read_manifest(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        fail("cannot read test-font manifest: " + str(error))

    require(manifest.get("formatVersion") == 1,
            "unsupported test-font manifest version")
    fixtures = manifest.get("fixtures")
    require(isinstance(fixtures, list) and fixtures,
            "test-font manifest has no fixtures")
    identifiers = set()
    outputs = set()

    for fixture in fixtures:
        verify_fixture_entry(fixture)
        identifier = fixture["id"]
        output = fixture["output"]
        require(identifier not in identifiers,
                "duplicate fixture ID: " + identifier)
        require(output not in outputs,
                "duplicate fixture output: " + output)
        identifiers.add(identifier)
        outputs.add(output)

    return manifest


def verify_fixture_entry(fixture):
    require(isinstance(fixture, dict), "fixture entry must be an object")
    text_fields = (
        "id", "publisher", "project", "revision", "url", "output",
        "license", "licenseSource", "format")
    for field in text_fields:
        require(isinstance(fixture.get(field), str) and fixture[field],
                "fixture field is missing or invalid: " + field)

    identifier = fixture["id"]
    identifier_characters = string.ascii_lowercase + string.digits + "-"
    require(all(character in identifier_characters for character in identifier),
            "fixture ID must use lowercase ASCII letters, digits, and hyphens")
    output = fixture["output"]
    require(Path(output).name == output and output not in (".", ".."),
            "fixture output must be one filename: " + output)
    parsed_url = urllib.parse.urlparse(fixture["url"])
    require(parsed_url.scheme == "https" and parsed_url.netloc and
            parsed_url.username is None and parsed_url.password is None,
            "fixture URL must be credential-free HTTPS: " + identifier)
    verify_size(fixture.get("downloadSize"), "downloadSize", identifier)
    verify_size(fixture.get("size"), "size", identifier)
    require(is_sha256(fixture.get("downloadSha256")),
            "invalid downloadSha256: " + identifier)
    require(is_sha256(fixture.get("sha256")),
            "invalid sha256: " + identifier)
    verify_string_list(fixture.get("scripts"), "scripts", identifier)
    verify_string_list(fixture.get("features"), "features", identifier)

    archive_member = fixture.get("archiveMember")
    if archive_member is not None:
        require(isinstance(archive_member, str) and archive_member,
                "archiveMember must be a nonempty string: " + identifier)
        member_path = Path(archive_member)
        require(not member_path.is_absolute() and
                ".." not in member_path.parts,
                "archiveMember cannot escape its archive: " + identifier)


def verify_size(value, field, identifier):
    require(isinstance(value, int) and not isinstance(value, bool) and
            value > 0,
            "invalid " + field + ": " + identifier)


def verify_string_list(value, field, identifier):
    require(isinstance(value, list) and value,
            field + " must be a nonempty array: " + identifier)
    for item in value:
        require(isinstance(item, str) and item,
                field + " values must be nonempty strings: " + identifier)


def select_fixtures(fixtures, requested_ids):
    by_identifier = {fixture["id"]: fixture for fixture in fixtures}
    if not requested_ids:
        return fixtures

    selected = []
    seen = set()
    for identifier in requested_ids:
        require(identifier in by_identifier,
                "unknown fixture ID: " + identifier)
        require(identifier not in seen,
                "fixture ID was selected more than once: " + identifier)
        selected.append(by_identifier[identifier])
        seen.add(identifier)
    return selected


def list_fixtures(fixtures):
    for fixture in fixtures:
        archive = fixture.get("archiveMember", "direct")
        print("\t".join((
            fixture["id"],
            fixture["publisher"],
            fixture["output"],
            archive,
            fixture["sha256"])))


def verify_cache(cache, fixtures):
    failures = []
    for fixture in fixtures:
        path = cache / fixture["output"]
        try:
            verify_file(path, fixture["size"], fixture["sha256"])
            verify_font_signature(path.read_bytes()[:4], fixture["id"])
            print("verified " + fixture["id"] + " at " + str(path))
        except DownloadError as error:
            failures.append(str(error))

    if failures:
        fail("cache verification failed:\n" + "\n".join(failures))


def download_fixture(cache, fixture):
    output = cache / fixture["output"]
    if output.exists():
        try:
            verify_file(output, fixture["size"], fixture["sha256"])
            verify_font_signature(output.read_bytes()[:4], fixture["id"])
            print("cached " + fixture["id"] + " at " + str(output))
            return
        except DownloadError:
            pass

    temporary_download = create_temporary_path(cache, ".download-")
    temporary_output = create_temporary_path(cache, ".font-")
    try:
        fetch(fixture, temporary_download)
        archive_member = fixture.get("archiveMember")
        if archive_member is None:
            os.replace(temporary_download, temporary_output)
            temporary_download = None
        else:
            extract_member(
                temporary_download,
                archive_member,
                temporary_output,
                fixture["size"])

        verify_file(
            temporary_output,
            fixture["size"],
            fixture["sha256"])
        with temporary_output.open("rb") as stream:
            verify_font_signature(stream.read(4), fixture["id"])
        os.replace(temporary_output, output)
        temporary_output = None
        print("downloaded " + fixture["id"] + " to " + str(output))
    finally:
        remove_temporary(temporary_download)
        remove_temporary(temporary_output)


def create_temporary_path(directory, prefix):
    descriptor, name = tempfile.mkstemp(prefix=prefix, dir=directory)
    os.close(descriptor)
    return Path(name)


def fetch(fixture, destination):
    request = urllib.request.Request(
        fixture["url"],
        headers={"User-Agent": "ttf-test-font-fetcher/1"})
    digest = hashlib.sha256()
    count = 0

    try:
        with urllib.request.urlopen(request, timeout=60) as response:
            final_url = urllib.parse.urlparse(response.geturl())
            require(final_url.scheme == "https",
                    "download redirected away from HTTPS: " + fixture["id"])
            with destination.open("wb") as output:
                while True:
                    chunk = response.read(CHUNK_SIZE)
                    if not chunk:
                        break
                    count += len(chunk)
                    require(count <= fixture["downloadSize"],
                            "download exceeded pinned size: " + fixture["id"])
                    digest.update(chunk)
                    output.write(chunk)
    except (OSError, urllib.error.URLError) as error:
        raise DownloadError(
            "cannot download " + fixture["id"] + ": " + str(error))

    require(count == fixture["downloadSize"],
            "download size differs: " + fixture["id"])
    require(digest.hexdigest() == fixture["downloadSha256"],
            "download SHA-256 differs: " + fixture["id"])


def extract_member(archive_path, member_name, destination, expected_size):
    try:
        with zipfile.ZipFile(archive_path, "r") as archive:
            info = archive.getinfo(member_name)
            require(not info.is_dir(),
                    "archive member is a directory: " + member_name)
            require(info.file_size == expected_size,
                    "archive member size differs: " + member_name)
            count = 0
            with archive.open(info, "r") as source:
                with destination.open("wb") as output:
                    while True:
                        chunk = source.read(CHUNK_SIZE)
                        if not chunk:
                            break
                        count += len(chunk)
                        require(count <= expected_size,
                                "archive member exceeded pinned size: " +
                                member_name)
                        output.write(chunk)
            require(count == expected_size,
                    "archive member size differs: " + member_name)
    except (KeyError, OSError, zipfile.BadZipFile) as error:
        raise DownloadError(
            "cannot extract archive member " + member_name + ": " +
            str(error))


def verify_file(path, expected_size, expected_hash):
    try:
        stat = path.stat()
    except OSError as error:
        raise DownloadError(str(path) + ": " + str(error))
    if not path.is_file():
        raise DownloadError(str(path) + ": not a regular file")
    if stat.st_size != expected_size:
        raise DownloadError(str(path) + ": size differs")

    digest = hashlib.sha256()
    try:
        with path.open("rb") as stream:
            while True:
                chunk = stream.read(CHUNK_SIZE)
                if not chunk:
                    break
                digest.update(chunk)
    except OSError as error:
        raise DownloadError(str(path) + ": " + str(error))
    if digest.hexdigest() != expected_hash:
        raise DownloadError(str(path) + ": SHA-256 differs")


def verify_font_signature(signature, identifier):
    if signature not in FONT_SIGNATURES:
        raise DownloadError(identifier + ": unsupported font signature")


def remove_temporary(path):
    if path is None:
        return
    try:
        path.unlink(missing_ok=True)
    except OSError:
        pass


def is_sha256(value):
    return isinstance(value, str) and len(value) == 64 and all(
        character in "0123456789abcdef" for character in value)


def require(condition, message):
    if not condition:
        raise DownloadError(message)


def fail(message):
    print("error: " + message, file=sys.stderr)
    raise SystemExit(1)


class DownloadError(Exception):
    pass


if __name__ == "__main__":
    try:
        main()
    except DownloadError as error:
        fail(str(error))
