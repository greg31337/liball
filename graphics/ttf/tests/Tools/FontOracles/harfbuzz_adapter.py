#!/usr/bin/env python3

import argparse
import json
from pathlib import Path
import shutil
import sys

from oracle_common import atomic_output_path
from oracle_common import fail
from oracle_common import OracleError
from oracle_common import read_versions
from oracle_common import remove_temporary
from oracle_common import replace_output
from oracle_common import require
from oracle_common import require_version
from oracle_common import run_tool
from oracle_common import verify_font
from oracle_common import write_json


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Stable JSON adapters for hb-shape and hb-subset.")
    parser.add_argument(
        "--versions",
        type=Path,
        default=script_directory / "versions.json")
    parser.add_argument("--hb-shape", default="hb-shape")
    parser.add_argument("--hb-subset", default="hb-subset")
    parser.add_argument("--timeout", type=int, default=30)
    parser.add_argument("--allow-version-mismatch", action="store_true")
    subparsers = parser.add_subparsers(dest="operation", required=True)

    shape = subparsers.add_parser("shape")
    shape.add_argument("font", type=Path)
    shape_input = shape.add_mutually_exclusive_group(required=True)
    shape_input.add_argument("--text")
    shape_input.add_argument("--unicodes")
    add_common_font_options(shape)
    shape.add_argument("--direction")
    shape.add_argument("--script")
    shape.add_argument("--language")
    shape.add_argument("--features")
    shape.add_argument("--cluster-level", type=int, default=0)
    shape.add_argument("--report", type=Path)

    subset = subparsers.add_parser("subset")
    subset.add_argument("font", type=Path)
    subset_input = subset.add_mutually_exclusive_group(required=True)
    subset_input.add_argument("--text")
    subset_input.add_argument("--unicodes")
    subset_input.add_argument("--gids")
    add_common_font_options(subset)
    subset.add_argument("--output", type=Path, required=True)
    subset.add_argument("--retain-gids", action="store_true")
    subset.add_argument("--no-hinting", action="store_true")
    subset.add_argument("--report", type=Path)
    return parser.parse_args()


def add_common_font_options(parser):
    parser.add_argument("--face-index", type=int, default=0)
    parser.add_argument("--variations")


def main():
    arguments = parse_arguments()
    require(arguments.timeout > 0, "--timeout must be positive")
    require(arguments.face_index >= 0, "--face-index cannot be negative")
    font = arguments.font.resolve()
    require(font.is_file(), "font does not exist: " + str(arguments.font))
    versions = read_versions(arguments.versions)
    if arguments.operation == "shape":
        executable = find_tool(arguments.hb_shape)
        version = tool_version(executable, arguments.timeout)
        require_version(
            version,
            versions["harfbuzz"],
            "HarfBuzz",
            arguments.allow_version_mismatch)
        shape_font(executable, version, font, arguments)
    else:
        executable = find_tool(arguments.hb_subset)
        version = tool_version(executable, arguments.timeout)
        require_version(
            version,
            versions["harfbuzz"],
            "HarfBuzz",
            arguments.allow_version_mismatch)
        subset_font(executable, version, font, arguments)


def find_tool(value):
    executable = shutil.which(value)
    require(executable is not None, "HarfBuzz executable was not found: " + value)
    return executable


def tool_version(executable, timeout):
    stdout, stderr = run_tool([executable, "--version"], timeout)
    first_line = (stdout or stderr).splitlines()[0]
    value = first_line.rsplit(" ", 1)[-1]
    require(value and value[0].isdigit(),
            "cannot parse HarfBuzz version: " + first_line)
    return value


def base_command(executable, font, arguments):
    command = [
        executable,
        str(font),
        "--face-index=" + str(arguments.face_index)
    ]
    if arguments.variations:
        command.append("--variations=" + arguments.variations)
    return command


def shape_font(executable, version, font, arguments):
    command = base_command(executable, font, arguments)
    command.extend((
        "--output-format=json",
        "--no-glyph-names",
        "--show-flags",
        "--cluster-level=" + str(arguments.cluster_level)))
    if arguments.text is not None:
        command.append("--text=" + arguments.text)
        source = {"text": arguments.text}
    else:
        command.append("--unicodes=" + arguments.unicodes)
        source = {"unicodes": arguments.unicodes}
    for name in ("direction", "script", "language", "features"):
        value = getattr(arguments, name)
        if value:
            command.append("--" + name + "=" + value)
    stdout, stderr = run_tool(command, arguments.timeout)
    try:
        glyphs = json.loads(stdout)
    except json.JSONDecodeError as error:
        raise OracleError("hb-shape returned invalid JSON: " + str(error))
    require(isinstance(glyphs, list), "hb-shape result must be an array")
    report = {
        "adapter": "harfbuzz-shape",
        "version": version,
        "font": verify_font(font),
        "faceIndex": arguments.face_index,
        "variations": arguments.variations or "",
        "source": source,
        "direction": arguments.direction or "auto",
        "script": arguments.script or "auto",
        "language": arguments.language or "default",
        "features": arguments.features or "",
        "clusterLevel": arguments.cluster_level,
        "glyphs": glyphs,
        "diagnostics": stderr.strip()
    }
    write_json(report, arguments.report)


def subset_font(executable, version, font, arguments):
    output, temporary = atomic_output_path(arguments.output)
    command = base_command(executable, font, arguments)
    command.append("--output-file=" + str(temporary))
    if arguments.text is not None:
        command.append("--text=" + arguments.text)
        selection = {"text": arguments.text}
    elif arguments.unicodes is not None:
        command.append("--unicodes=" + arguments.unicodes)
        selection = {"unicodes": arguments.unicodes}
    else:
        command.append("--gids=" + arguments.gids)
        selection = {"gids": arguments.gids}
    if arguments.retain_gids:
        command.append("--retain-gids")
    if arguments.no_hinting:
        command.append("--no-hinting")
    try:
        stdout, stderr = run_tool(command, arguments.timeout)
        replace_output(temporary, output)
    finally:
        remove_temporary(temporary)
    report = {
        "adapter": "harfbuzz-subset",
        "version": version,
        "sourceFont": verify_font(font),
        "outputFont": verify_font(output),
        "faceIndex": arguments.face_index,
        "variations": arguments.variations or "",
        "selection": selection,
        "retainGids": arguments.retain_gids,
        "noHinting": arguments.no_hinting,
        "diagnostics": (stdout + stderr).strip()
    }
    write_json(report, arguments.report)


if __name__ == "__main__":
    try:
        main()
    except OracleError as error:
        fail(str(error))
