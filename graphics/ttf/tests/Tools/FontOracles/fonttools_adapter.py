#!/usr/bin/env python3

import argparse
from pathlib import Path
import sys

from oracle_common import atomic_output_path
from oracle_common import fail
from oracle_common import OracleError
from oracle_common import read_versions
from oracle_common import remove_temporary
from oracle_common import replace_output
from oracle_common import require
from oracle_common import require_version
from oracle_common import verify_font
from oracle_common import write_json


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Stable fontTools parsing, instancing, and subset adapter.")
    parser.add_argument(
        "--versions",
        type=Path,
        default=script_directory / "versions.json")
    parser.add_argument("--allow-version-mismatch", action="store_true")
    subparsers = parser.add_subparsers(dest="operation", required=True)

    inspect = subparsers.add_parser("inspect")
    inspect.add_argument("font", type=Path)
    inspect.add_argument("--face-index", type=int, default=0)
    inspect.add_argument("--codepoints")
    inspect.add_argument("--report", type=Path)

    instance = subparsers.add_parser("instance")
    instance.add_argument("font", type=Path)
    instance.add_argument("--face-index", type=int, default=0)
    instance.add_argument("--variations", required=True)
    instance.add_argument("--output", type=Path, required=True)
    instance.add_argument("--no-optimize", action="store_true")
    instance.add_argument("--report", type=Path)

    subset = subparsers.add_parser("subset")
    subset.add_argument("font", type=Path)
    subset.add_argument("--face-index", type=int, default=0)
    selection = subset.add_mutually_exclusive_group(required=True)
    selection.add_argument("--text")
    selection.add_argument("--unicodes")
    selection.add_argument("--gids")
    subset.add_argument("--output", type=Path, required=True)
    subset.add_argument("--retain-gids", action="store_true")
    subset.add_argument("--no-hinting", action="store_true")
    subset.add_argument("--layout-features")
    subset.add_argument("--report", type=Path)
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    require(arguments.face_index >= 0, "--face-index cannot be negative")
    font_path = arguments.font.resolve()
    require(font_path.is_file(),
            "font does not exist: " + str(arguments.font))
    versions = read_versions(arguments.versions)
    fonttools, TTFont = import_fonttools()
    require_version(
        fonttools.__version__,
        versions["fonttools"],
        "fontTools",
        arguments.allow_version_mismatch)

    if arguments.operation == "inspect":
        font = open_font(TTFont, font_path, arguments.face_index)
        try:
            report = base_report(
                "fonttools-inspect",
                fonttools.__version__,
                font_path,
                font,
                arguments.codepoints)
        finally:
            font.close()
        write_json(report, arguments.report)
    elif arguments.operation == "instance":
        instance_font(fonttools, TTFont, font_path, arguments)
    else:
        subset_font(fonttools, TTFont, font_path, arguments)


def import_fonttools():
    try:
        import fontTools
        from fontTools.ttLib import TTFont
    except ImportError as error:
        raise OracleError(
            "fontTools is unavailable; install requirements.txt: " +
            str(error))
    return fontTools, TTFont


def open_font(TTFont, path, face_index):
    try:
        return TTFont(
            str(path),
            fontNumber=face_index,
            lazy=False,
            recalcTimestamp=False)
    except Exception as error:
        raise OracleError("fontTools cannot parse font: " + str(error))


def base_report(adapter, version, path, font, codepoint_text=None):
    glyph_order = font.getGlyphOrder()
    tables = sorted(str(tag) for tag in font.reader.keys())
    report = {
        "adapter": adapter,
        "version": version,
        "font": verify_font(path),
        "sfntVersion": sfnt_version(font.sfntVersion),
        "flavor": font.flavor or "sfnt",
        "tables": tables,
        "glyphCount": len(glyph_order),
        "glyphOrder": glyph_order,
        "unitsPerEm": font["head"].unitsPerEm if "head" in font else None,
        "axes": axes(font)
    }
    if codepoint_text:
        codepoints = parse_integer_list(codepoint_text, 0x10FFFF, 16)
        cmap = font.getBestCmap() or {}
        report["cmap"] = [{
            "codepoint": codepoint,
            "glyph": cmap.get(codepoint)
        } for codepoint in codepoints]
    return report


def sfnt_version(value):
    if isinstance(value, bytes):
        return value.hex().upper()
    if value == "\x00\x01\x00\x00":
        return "00010000"
    return str(value)


def axes(font):
    if "fvar" not in font:
        return []
    return [{
        "tag": axis.axisTag,
        "minimum": axis.minValue,
        "default": axis.defaultValue,
        "maximum": axis.maxValue
    } for axis in font["fvar"].axes]


def instance_font(fonttools, TTFont, font_path, arguments):
    try:
        from fontTools.varLib.instancer import instantiateVariableFont
    except ImportError as error:
        raise OracleError("fontTools instancer is unavailable: " + str(error))
    locations = parse_variations(arguments.variations)
    font = open_font(TTFont, font_path, arguments.face_index)
    output, temporary = atomic_output_path(arguments.output)
    try:
        require("fvar" in font, "font has no fvar table")
        instantiateVariableFont(
            font,
            locations,
            inplace=True,
            optimize=not arguments.no_optimize)
        save_font(font, temporary)
        font.close()
        replace_output(temporary, output)
    finally:
        font.close()
        remove_temporary(temporary)
    output_font = open_font(TTFont, output, 0)
    try:
        report = base_report(
            "fonttools-instance",
            fonttools.__version__,
            output,
            output_font)
    finally:
        output_font.close()
    report["sourceFont"] = verify_font(font_path)
    report["location"] = locations
    report["optimized"] = not arguments.no_optimize
    write_json(report, arguments.report)


def subset_font(fonttools, TTFont, font_path, arguments):
    try:
        from fontTools import subset
    except ImportError as error:
        raise OracleError("fontTools subsetter is unavailable: " + str(error))
    options = subset.Options()
    options.retain_gids = arguments.retain_gids
    options.hinting = not arguments.no_hinting
    if arguments.layout_features:
        options.layout_features = split_values(arguments.layout_features)
    subsetter = subset.Subsetter(options=options)
    if arguments.text is not None:
        subsetter.populate(text=arguments.text)
        selection = {"text": arguments.text}
    elif arguments.unicodes is not None:
        unicodes = parse_integer_list(arguments.unicodes, 0x10FFFF, 16)
        subsetter.populate(unicodes=unicodes)
        selection = {"unicodes": unicodes}
    else:
        gids = parse_integer_list(arguments.gids, 0x7FFFFFFF, 10)
        subsetter.populate(gids=gids)
        selection = {"gids": gids}

    font = open_font(TTFont, font_path, arguments.face_index)
    output, temporary = atomic_output_path(arguments.output)
    try:
        subsetter.subset(font)
        save_font(font, temporary)
        font.close()
        replace_output(temporary, output)
    except Exception as error:
        raise OracleError("fontTools cannot subset font: " + str(error))
    finally:
        font.close()
        remove_temporary(temporary)
    output_font = open_font(TTFont, output, 0)
    try:
        report = base_report(
            "fonttools-subset",
            fonttools.__version__,
            output,
            output_font)
    finally:
        output_font.close()
    report["sourceFont"] = verify_font(font_path)
    report["selection"] = selection
    report["retainGids"] = arguments.retain_gids
    report["hinting"] = not arguments.no_hinting
    report["layoutFeatures"] = options.layout_features
    write_json(report, arguments.report)


def save_font(font, path):
    try:
        font.recalcTimestamp = False
        font.save(str(path), reorderTables=True)
    except Exception as error:
        raise OracleError("fontTools cannot write font: " + str(error))


def parse_variations(value):
    locations = {}
    for item in split_values(value):
        tag, separator, number = item.partition("=")
        require(separator == "=" and len(tag) == 4 and number,
                "variation must use TAG=VALUE: " + item)
        require(tag not in locations, "duplicate variation axis: " + tag)
        try:
            locations[tag] = float(number)
        except ValueError:
            raise OracleError("invalid variation value: " + item)
    require(locations, "at least one variation is required")
    return locations


def parse_integer_list(value, maximum, base):
    result = []
    seen = set()
    for item in split_values(value):
        try:
            number = int(item, base)
        except ValueError:
            raise OracleError("invalid hexadecimal integer: " + item)
        require(0 <= number <= maximum, "integer is out of range: " + item)
        require(number not in seen, "duplicate integer: " + item)
        result.append(number)
        seen.add(number)
    require(result, "integer list cannot be empty")
    return result


def split_values(value):
    return [item.strip() for item in value.split(",") if item.strip()]


if __name__ == "__main__":
    try:
        main()
    except OracleError as error:
        fail(str(error))
