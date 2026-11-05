import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys
import tempfile


FONT_SIGNATURES = (b"\x00\x01\x00\x00", b"OTTO", b"ttcf", b"wOFF", b"wOF2")
MAX_TOOL_OUTPUT = 1024 * 1024


class OracleError(Exception):
    pass


def fail(message):
    print("error: " + message, file=sys.stderr)
    raise SystemExit(1)


def require(condition, message):
    if not condition:
        raise OracleError(message)


def read_versions(path):
    try:
        manifest = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as error:
        raise OracleError("cannot read version manifest: " + str(error))
    require(manifest.get("formatVersion") == 1,
            "unsupported oracle version manifest")
    for field in ("harfbuzz", "fonttools", "freetypePy", "freetype"):
        require(isinstance(manifest.get(field), str) and manifest[field],
                "oracle version is missing: " + field)
    return manifest


def require_version(actual, expected, name, allow_mismatch):
    if not allow_mismatch:
        require(actual == expected,
                name + " version differs: expected " + expected +
                ", found " + actual)


def run_tool(command, timeout):
    environment = os.environ.copy()
    environment["LC_ALL"] = "C"
    environment["LANG"] = "C"
    try:
        process = subprocess.run(
            command,
            stdin=subprocess.DEVNULL,
            stdout=subprocess.PIPE,
            stderr=subprocess.PIPE,
            timeout=timeout,
            check=False,
            env=environment)
    except (OSError, subprocess.TimeoutExpired) as error:
        raise OracleError("cannot execute oracle: " + str(error))
    stdout = decode_output(process.stdout, "stdout")
    stderr = decode_output(process.stderr, "stderr")
    require(process.returncode == 0,
            "oracle exited with " + str(process.returncode) +
            (": " + stderr.strip() if stderr.strip() else ""))
    return stdout, stderr


def decode_output(data, name):
    require(len(data) <= MAX_TOOL_OUTPUT,
            "oracle " + name + " exceeded output limit")
    return data.decode("utf-8", errors="strict")


def verify_font(path):
    try:
        with path.open("rb") as stream:
            signature = stream.read(4)
        size = path.stat().st_size
    except OSError as error:
        raise OracleError("cannot read font: " + str(error))
    require(size > 0, "font is empty: " + str(path))
    require(signature in FONT_SIGNATURES,
            "unsupported font signature: " + str(path))
    return {
        "file": path.name,
        "size": size,
        "sha256": sha256_file(path)
    }


def sha256_file(path):
    digest = hashlib.sha256()
    try:
        with path.open("rb") as stream:
            while True:
                chunk = stream.read(64 * 1024)
                if not chunk:
                    break
                digest.update(chunk)
    except OSError as error:
        raise OracleError("cannot hash file: " + str(error))
    return digest.hexdigest()


def write_json(value, path):
    data = (json.dumps(
        value,
        ensure_ascii=True,
        indent=2,
        sort_keys=True) + "\n").encode("utf-8")
    if path is None:
        sys.stdout.buffer.write(data)
        return
    path = path.resolve()
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


def atomic_output_path(path):
    path = path.resolve()
    path.parent.mkdir(parents=True, exist_ok=True)
    descriptor, temporary_name = tempfile.mkstemp(
        prefix="." + path.name + ".",
        suffix=path.suffix,
        dir=path.parent)
    os.close(descriptor)
    return path, Path(temporary_name)


def replace_output(temporary, output):
    verify_font(temporary)
    os.replace(temporary, output)


def remove_temporary(path):
    try:
        path.unlink(missing_ok=True)
    except OSError:
        pass
