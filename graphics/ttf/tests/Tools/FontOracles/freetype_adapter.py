#!/usr/bin/env python3

import argparse
import importlib.metadata
from pathlib import Path

from oracle_common import fail
from oracle_common import OracleError
from oracle_common import read_versions
from oracle_common import require
from oracle_common import require_version
from oracle_common import verify_font
from oracle_common import write_json


def parse_arguments():
    script_directory = Path(__file__).resolve().parent
    parser = argparse.ArgumentParser(
        description="Stable FreeType unscaled outline and metric adapter.")
    parser.add_argument(
        "--versions",
        type=Path,
        default=script_directory / "versions.json")
    parser.add_argument("--allow-version-mismatch", action="store_true")
    parser.add_argument("font", type=Path)
    parser.add_argument("--face-index", type=int, default=0)
    selection = parser.add_mutually_exclusive_group(required=True)
    selection.add_argument("--glyph-id", type=int)
    selection.add_argument("--codepoint")
    parser.add_argument("--report", type=Path)
    return parser.parse_args()


def main():
    arguments = parse_arguments()
    require(arguments.face_index >= 0, "--face-index cannot be negative")
    font_path = arguments.font.resolve()
    require(font_path.is_file(),
            "font does not exist: " + str(arguments.font))
    versions = read_versions(arguments.versions)
    freetype = import_freetype()
    binding_version = importlib.metadata.version("freetype-py")
    library_version = ".".join(str(value) for value in freetype.version())
    require_version(
        binding_version,
        versions["freetypePy"],
        "freetype-py",
        arguments.allow_version_mismatch)
    require_version(
        library_version,
        versions["freetype"],
        "FreeType",
        arguments.allow_version_mismatch)

    try:
        face = freetype.Face(str(font_path), index=arguments.face_index)
    except Exception as error:
        raise OracleError("FreeType cannot open font: " + str(error))
    if arguments.glyph_id is not None:
        glyph_id = arguments.glyph_id
        source = {"glyphId": glyph_id}
    else:
        codepoint = parse_codepoint(arguments.codepoint)
        glyph_id = face.get_char_index(codepoint)
        source = {"codepoint": codepoint, "glyphId": glyph_id}
    require(0 <= glyph_id < face.num_glyphs,
            "glyph ID is outside the face")

    flags = freetype.FT_LOAD_NO_SCALE | \
        freetype.FT_LOAD_NO_HINTING | \
        freetype.FT_LOAD_NO_BITMAP
    try:
        face.load_glyph(glyph_id, flags)
    except Exception as error:
        raise OracleError("FreeType cannot load glyph: " + str(error))
    slot = face.glyph
    metrics = slot.metrics
    outline = slot.outline
    box = outline.get_bbox()
    report = {
        "adapter": "freetype-outline-metrics",
        "freetypeVersion": library_version,
        "bindingVersion": binding_version,
        "font": verify_font(font_path),
        "faceIndex": arguments.face_index,
        "source": source,
        "faceMetrics": {
            "unitsPerEm": face.units_per_EM,
            "ascender": face.ascender,
            "descender": face.descender,
            "height": face.height,
            "maximumAdvanceWidth": face.max_advance_width,
            "maximumAdvanceHeight": face.max_advance_height,
            "glyphCount": face.num_glyphs
        },
        "glyphMetrics": {
            "width": metrics.width,
            "height": metrics.height,
            "horizontalBearingX": metrics.horiBearingX,
            "horizontalBearingY": metrics.horiBearingY,
            "horizontalAdvance": metrics.horiAdvance,
            "verticalBearingX": metrics.vertBearingX,
            "verticalBearingY": metrics.vertBearingY,
            "verticalAdvance": metrics.vertAdvance,
            "advanceX": slot.advance.x,
            "advanceY": slot.advance.y,
            "linearHorizontalAdvance": slot.linearHoriAdvance,
            "linearVerticalAdvance": slot.linearVertAdvance
        },
        "outline": {
            "pointCount": len(outline.points),
            "contourCount": len(outline.contours),
            "points": [[point[0], point[1]] for point in outline.points],
            "tags": [int(tag) for tag in outline.tags],
            "contourEnds": [int(value) for value in outline.contours],
            "controlBox": {
                "xMin": box.xMin,
                "yMin": box.yMin,
                "xMax": box.xMax,
                "yMax": box.yMax
            }
        }
    }
    write_json(report, arguments.report)


def import_freetype():
    try:
        import freetype
    except ImportError as error:
        raise OracleError(
            "freetype-py is unavailable; install requirements.txt: " +
            str(error))
    return freetype


def parse_codepoint(value):
    try:
        codepoint = int(value, 16)
    except ValueError:
        raise OracleError("codepoint must be hexadecimal: " + value)
    require(0 <= codepoint <= 0x10FFFF and not 0xD800 <= codepoint <= 0xDFFF,
            "codepoint is not a Unicode scalar")
    return codepoint


if __name__ == "__main__":
    try:
        main()
    except OracleError as error:
        fail(str(error))
