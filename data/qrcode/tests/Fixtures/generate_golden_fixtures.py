#!/usr/bin/env python3
"""Regenerate the checked-in QR-family golden fixture corpus."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import shutil
import subprocess
import tempfile
from pathlib import Path


ROOT = Path(__file__).resolve().parents[2]
FIXTURE_ROOT = ROOT / "tests" / "Fixtures" / "Golden"
MATRIX_DIRECTORY = FIXTURE_ROOT / "matrices"
DAMAGED_DIRECTORY = FIXTURE_ROOT / "damaged"
IMAGE_DIRECTORY = FIXTURE_ROOT / "images"
SOURCE_DIRECTORY = FIXTURE_ROOT / "sources"

ZINT_COMMIT = "baa91878f488f472fee78585184ffcee07c1dd51"
ZINT_VERSION_PREFIX = "Zint version 2.16.0.9"

LEGACY_SOURCE_HASHES = {
    "legacy-model1-v01-layout.svg": "3462ae838cffab56f655ec9ef9895afbf508cb6b26e6ff7a5164602536279a2b",
    "legacy-model1-v02-qr-code-model-1.svg": "db7412524b0b1af7f983c8756798120f48fcd99bbf3d1949f244087abdbc7713",
    "legacy-model1-v14-layout.svg": "f54747a5361a8b27263fca29d10301108e2efee0d9d6d22d2f28829005da163a",
}

MODEL1_DATA_CODEWORDS = {
    "L": [19, 36, 57, 80, 108, 136, 170, 208, 246, 290, 336, 384, 432, 489],
    "M": [16, 30, 44, 60, 82, 106, 132, 160, 186, 222, 256, 292, 332, 368],
    "Q": [13, 24, 36, 50, 68, 86, 108, 128, 156, 183, 208, 244, 276, 310],
    "H": [9, 16, 24, 34, 46, 58, 72, 87, 102, 124, 145, 165, 192, 210],
}

MICRO_DATA_BITS = {1: 20, 2: 40, 3: 84, 4: 128}
MICRO_ECC_CODEWORDS = {1: 2, 2: 5, 3: 6, 4: 8}
MICRO_TERMINATOR_BITS = {1: 3, 2: 5, 3: 7, 4: 9}


def sha256_bytes(data: bytes) -> str:
    return hashlib.sha256(data).hexdigest()


def prepare_output_directories() -> None:
    for directory in (MATRIX_DIRECTORY, DAMAGED_DIRECTORY, IMAGE_DIRECTORY):
        if directory.exists():
            shutil.rmtree(directory)
        directory.mkdir(parents=True)


def verify_tools(zint: Path, magick: Path) -> None:
    zint_version = subprocess.run(
        [str(zint), "--version"], check=True, stdout=subprocess.PIPE, text=True
    ).stdout.strip()
    if not zint_version.startswith(ZINT_VERSION_PREFIX):
        raise ValueError(f"unexpected Zint version: {zint_version}")
    subprocess.run([str(magick), "--version"], check=True, stdout=subprocess.PIPE)

    for name, expected_hash in LEGACY_SOURCE_HASHES.items():
        actual_hash = sha256_bytes((SOURCE_DIRECTORY / name).read_bytes())
        if actual_hash != expected_hash:
            raise ValueError(f"{name} has SHA-256 {actual_hash}; expected {expected_hash}")


def parse_matrix_dump(path: Path, width: int, height: int) -> list[str]:
    rows = []
    for line in path.read_text(encoding="ascii").splitlines():
        tokens = line.split()
        bits = "".join(f"{int(token, 16):0{len(token) * 4}b}" for token in tokens)
        rows.append(bits[:width])
    if len(rows) != height or any(len(row) != width for row in rows):
        raise ValueError(f"invalid matrix dimensions in {path}")
    return rows


def parse_hex_section(output: str, heading: str) -> list[int]:
    match = re.search(heading + r" \(([0-9]+)\):\s*", output)
    if match is None:
        raise ValueError(f"missing verbose section {heading}")
    count = int(match.group(1))
    tokens = re.findall(r"(?:0x)?([0-9A-Fa-f]{1,2})(?:\s|$)", output[match.end():])
    values = [int(token, 16) for token in tokens[:count]]
    if len(values) != count:
        raise ValueError(f"incomplete verbose section {heading}")
    return values


def run_zint_case(zint: Path, case: dict[str, object]) -> tuple[dict[str, object], list[str]]:
    with tempfile.TemporaryDirectory(prefix="qrcode-golden-") as temporary:
        matrix_path = Path(temporary) / "matrix.txt"
        command = [
            str(zint),
            "--barcode=" + str(case["barcode"]),
            "--vers=" + str(case["version_option"]),
            "--secure=" + str(case["secure"]),
            "--data=" + str(case["payload"]),
            "--verbose",
            "--filetype=TXT",
            "--output=" + str(matrix_path),
        ]
        if "mask_option" in case:
            command.append("--mask=" + str(case["mask_option"]))
        if "eci" in case:
            command.append("--eci=" + str(case["eci"]))
        if "structured_append" in case:
            structured_append = case["structured_append"]
            command.append(
                "--structapp="
                + str(structured_append["index"])
                + ","
                + str(structured_append["count"])
                + ","
                + str(structured_append["parity"])
            )

        completed = subprocess.run(command, check=True, stdout=subprocess.PIPE, text=True)
        output = completed.stdout
        selected = re.search(r"Selected version: .*?([0-9]+)x([0-9]+)", output)
        if selected is None:
            selected = re.search(r"Version: .*?Size: ([0-9]+)x([0-9]+)", output)
        if selected is None:
            raise ValueError("Zint did not report a selected size")
        height = int(selected.group(1))
        width = int(selected.group(2))
        matrix = parse_matrix_dump(matrix_path, width, height)

    mask_match = re.search(r"Mask: ([0-9]+)", output)
    if mask_match is None and case["barcode"] != "rmqr":
        raise ValueError("Zint did not report a mask")
    mask = 4 if case["barcode"] == "rmqr" else int(mask_match.group(1))

    if case["barcode"] == "microqr":
        binary_match = re.search(r"Binary \([0-9]+\): ([01]+)", output)
        if binary_match is None:
            raise ValueError("Zint did not report the Micro QR binary stream")
        final_bits, data_codewords, full_codewords = finish_micro_stream(
            int(case["version_option"]), binary_match.group(1)
        )
    else:
        data_codewords = parse_hex_section(output, r"Resulting codewords")
        full_codewords = parse_hex_section(output, r"Data Stream")
        final_bits = "".join(f"{value:08b}" for value in data_codewords)

    matrix_file = "matrices/" + str(case["id"]) + ".txt"
    matrix_bytes = matrix_to_bytes(matrix)
    (FIXTURE_ROOT / matrix_file).write_bytes(matrix_bytes)
    vector = {
        "id": case["id"],
        "profile": case["profile"],
        "version": case["version"],
        "width": width,
        "height": height,
        "error_correction": case["error_correction"],
        "mask": mask,
        "payload_utf8": case["payload"],
        "eci": case.get("eci", 0),
        "structured_append": case.get("structured_append"),
        "final_data_bitstream": final_bits,
        "data_codewords": data_codewords,
        "full_codewords": full_codewords,
        "matrix_file": matrix_file,
        "matrix_sha256": sha256_bytes(matrix_bytes),
        "oracle": "Zint " + ZINT_VERSION_PREFIX.removeprefix("Zint version "),
    }
    return vector, matrix


def finish_micro_stream(version: int, bits: str) -> tuple[str, list[int], list[int]]:
    capacity = MICRO_DATA_BITS[version]
    bits_left = capacity - len(bits)
    terminator = min(bits_left, MICRO_TERMINATOR_BITS[version])
    bits += "0" * terminator
    bits_left -= terminator
    final_nibble = version in (1, 3)
    if final_nibble and bits_left and bits_left <= 4:
        bits += "0" * bits_left
        bits_left = 0
    if bits_left:
        remainder = 8 - len(bits) % 8
        if remainder != 8:
            bits += "0" * remainder
            bits_left -= remainder
        if final_nibble and bits_left > 4:
            bits_left -= 4
        for index in range(bits_left // 8):
            bits += f"{0x11 if index % 2 else 0xEC:08b}"
        if final_nibble:
            bits += "0000"
    if len(bits) != capacity:
        raise ValueError("Micro QR padding did not reach its data capacity")

    data_codewords = []
    for offset in range(0, len(bits), 8):
        chunk = bits[offset:offset + 8]
        data_codewords.append(int(chunk.ljust(8, "0"), 2))
    ecc = reed_solomon_encode(data_codewords, MICRO_ECC_CODEWORDS[version])
    return bits, data_codewords, data_codewords + ecc


def reed_solomon_encode(data: list[int], degree: int) -> list[int]:
    exponent = [0] * 512
    logarithm = [0] * 256
    value = 1
    for index in range(255):
        exponent[index] = value
        logarithm[value] = index
        value <<= 1
        if value & 0x100:
            value ^= 0x11D
    for index in range(255, 512):
        exponent[index] = exponent[index - 255]

    def multiply(left: int, right: int) -> int:
        if left == 0 or right == 0:
            return 0
        return exponent[logarithm[left] + logarithm[right]]

    generator = [1]
    for root in range(degree):
        result = [0] * (len(generator) + 1)
        for index, coefficient in enumerate(generator):
            result[index] ^= coefficient
            result[index + 1] ^= multiply(coefficient, exponent[root])
        generator = result

    remainder = data + [0] * degree
    for index in range(len(data)):
        coefficient = remainder[index]
        if coefficient:
            for generator_index, generator_value in enumerate(generator):
                remainder[index + generator_index] ^= multiply(generator_value, coefficient)
    return remainder[-degree:]


def rasterize_legacy_model1(magick: Path) -> tuple[dict[str, object], list[str]]:
    source_name = "legacy-model1-v02-qr-code-model-1.svg"
    source = SOURCE_DIRECTORY / source_name
    completed = subprocess.run(
        [
            str(magick), str(source), "-background", "white", "-alpha", "remove",
            "-colorspace", "Gray", "-depth", "8", "gray:-",
        ],
        check=True,
        stdout=subprocess.PIPE,
    )
    pixels = completed.stdout
    image_size = 528
    scale = 16
    outer_modules = 33
    if len(pixels) != image_size * image_size:
        raise ValueError("unexpected raster size for the legacy Model 1 SVG")
    outer = []
    for row in range(outer_modules):
        modules = []
        for column in range(outer_modules):
            offset = (row * scale + scale // 2) * image_size + column * scale + scale // 2
            modules.append("1" if pixels[offset] < 128 else "0")
        outer.append("".join(modules))
    if any("1" in row for row in outer[:4] + outer[-4:]):
        raise ValueError("legacy top or bottom quiet zone is not clear")
    matrix = [row[4:-4] for row in outer[4:-4]]
    version = (len(matrix) - 17) // 4
    error_correction, mask = read_model1_format(matrix)
    full_codewords = read_model1_codewords(matrix, mask)
    data_count = MODEL1_DATA_CODEWORDS[error_correction][version - 1]
    data_codewords = full_codewords[:data_count]
    final_bits = "".join(f"{value:08b}" for value in data_codewords)[4:]
    if decode_model1_payload(final_bits, version) != "QR Code Model 1 ":
        raise ValueError("legacy Model 1 payload did not decode as expected")

    matrix_file = "matrices/legacy-model1-v02-m.txt"
    matrix_bytes = matrix_to_bytes(matrix)
    (FIXTURE_ROOT / matrix_file).write_bytes(matrix_bytes)
    vector = {
        "id": "legacy-model1-v02-m",
        "profile": "LegacyModel1",
        "version": 2,
        "width": 25,
        "height": 25,
        "error_correction": error_correction,
        "mask": mask,
        "payload_utf8": "QR Code Model 1 ",
        "eci": 0,
        "structured_append": None,
        "final_data_bitstream": final_bits,
        "data_codewords": data_codewords,
        "full_codewords": full_codewords,
        "matrix_file": matrix_file,
        "matrix_sha256": sha256_bytes(matrix_bytes),
        "oracle": "CC0 Wikimedia encoded reference; codewords extracted with the ISO Model 1 traversal",
        "source_file": "sources/" + source_name,
        "source_sha256": LEGACY_SOURCE_HASHES[source_name],
    }
    return vector, matrix


def read_model1_format(matrix: list[str]) -> tuple[str, int]:
    dimension = len(matrix)
    first_bits = [matrix[8][column] for column in range(6)]
    first_bits += [matrix[8][7], matrix[8][8], matrix[7][8]]
    first_bits += [matrix[row][8] for row in range(5, -1, -1)]
    second_bits = [matrix[row][8] for row in range(dimension - 1, dimension - 9, -1)]
    second_bits += [matrix[8][column] for column in range(dimension - 8, dimension)]
    observed = [int("".join(first_bits), 2), int("".join(second_bits), 2)]
    level_bits = {"L": 1, "M": 0, "Q": 3, "H": 2}
    best = None
    for level, bits in level_bits.items():
        for mask in range(8):
            code = create_format_code((bits << 3) | mask) ^ 0x2825
            distance = min(bin(value ^ code).count("1") for value in observed)
            candidate = (distance, level, mask)
            if best is None or candidate < best:
                best = candidate
    if best is None or best[0] != 0:
        raise ValueError("legacy Model 1 format information is not exact")
    return best[1], best[2]


def create_format_code(data: int) -> int:
    remainder = data << 10
    for bit in range(14, 9, -1):
        if remainder & (1 << bit):
            remainder ^= 0x537 << (bit - 10)
    return data << 10 | remainder


def mask_bit(mask: int, column: int, row: int) -> bool:
    expressions = (
        (row + column) % 2 == 0,
        row % 2 == 0,
        column % 3 == 0,
        (row + column) % 3 == 0,
        (row // 2 + column // 3) % 2 == 0,
        (row * column) % 6 == 0,
        (row * column) % 6 < 3,
        (row + column + (row * column) % 3) % 2 == 0,
    )
    return expressions[mask]


def read_model1_codewords(matrix: list[str], mask: int) -> list[int]:
    dimension = len(matrix)
    columns = dimension // 4 + 3
    codewords = []

    def read_symbol(column: int, row: int, vertical: bool) -> int:
        value = 0
        for bit in range(8):
            x = column - bit % (2 if vertical else 4)
            y = row - bit // (2 if vertical else 4)
            value = (value << 1) | int(mask_bit(mask, x, y) != (matrix[y][x] == "1"))
        return value

    for column_group in range(columns):
        if column_group <= 1:
            rows = (dimension - 8) // 4
            for row_group in range(rows):
                if column_group == 0 and row_group % 2 == 0 and 0 < row_group < rows - 1:
                    continue
                x = dimension - 1 - column_group * 2
                y = dimension - 1 - row_group * 4
                codewords.append(read_symbol(x, y, True))
        elif columns - column_group <= 4:
            rows = (dimension - 16) // 4
            for row_group in range(rows):
                x = (columns - column_group - 1) * 2 + 1
                if columns - column_group == 4:
                    x += 1
                y = dimension - 9 - row_group * 4
                codewords.append(read_symbol(x, y, True))
        else:
            rows = dimension // 2
            for row_group in range(rows):
                if column_group == 2 and row_group >= rows - 4:
                    continue
                if row_group == 0 and column_group % 2 == 1 and column_group + 1 != columns - 4:
                    continue
                x = dimension - 5 - (column_group - 2) * 4
                y = dimension - 1 - row_group * 2 - int(row_group >= rows - 3)
                codewords.append(read_symbol(x, y, False))
    codewords[0] &= 0x0F
    return codewords


def decode_model1_payload(bits: str, version: int) -> str:
    position = 0
    output = ""
    alphanumeric = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ $%*+-./:"
    widths = {1: 10 if version < 10 else 12, 2: 9 if version < 10 else 11, 4: 8 if version < 10 else 16}
    while position + 4 <= len(bits):
        mode = int(bits[position:position + 4], 2)
        position += 4
        if mode == 0:
            return output
        if mode not in widths:
            raise ValueError("unsupported mode in legacy reference")
        width = widths[mode]
        count = int(bits[position:position + width], 2)
        position += width
        if mode == 2:
            while count >= 2:
                value = int(bits[position:position + 11], 2)
                position += 11
                output += alphanumeric[value // 45] + alphanumeric[value % 45]
                count -= 2
            if count:
                output += alphanumeric[int(bits[position:position + 6], 2)]
                position += 6
        elif mode == 4:
            data = bytearray()
            for _ in range(count):
                data.append(int(bits[position:position + 8], 2))
                position += 8
            output += data.decode("iso-8859-1")
        else:
            raise ValueError("unexpected numeric mode in legacy reference")
    raise ValueError("legacy payload has no terminator")


def matrix_to_bytes(matrix: list[str]) -> bytes:
    return ("\n".join(matrix) + "\n").encode("ascii")


def create_damaged_fixtures(matrices: dict[str, list[str]]) -> list[dict[str, object]]:
    definitions = [
        ("model2-v10-q-format-3", "model2-v10-q", "format", [(8, 0), (8, 1), (8, 2)]),
        ("model2-v40-h-burst-12", "model2-v40-h", "module-burst", [(90, 90 + index) for index in range(12)]),
        ("micro-m4-l-format-2", "micro-m4-l", "format", [(8, 1), (8, 2)]),
        ("rmqr-r17x139-h-format-3", "rmqr-r17x139-h", "format", [(1, 11), (2, 11), (3, 11)]),
        ("legacy-model1-v02-m-format-3", "legacy-model1-v02-m", "format", [(8, 0), (8, 1), (8, 2)]),
    ]
    fixtures = []
    for fixture_id, source_id, damage_kind, coordinates in definitions:
        source = matrices[source_id]
        mutable = [list(row) for row in source]
        for row, column in coordinates:
            mutable[row][column] = "0" if mutable[row][column] == "1" else "1"
        damaged = ["".join(row) for row in mutable]
        filename = "damaged/" + fixture_id + ".txt"
        data = matrix_to_bytes(damaged)
        (FIXTURE_ROOT / filename).write_bytes(data)
        fixtures.append({
            "id": fixture_id,
            "source_vector": source_id,
            "damage_kind": damage_kind,
            "flipped_modules_row_column": coordinates,
            "matrix_file": filename,
            "matrix_sha256": sha256_bytes(data),
        })
    return fixtures


def create_raw_images(matrices: dict[str, list[str]]) -> list[dict[str, object]]:
    definitions = [
        ("model2-v01-gray8", "model2-v01-l", "Gray8", 4, 4, 0, False, 0),
        ("model2-v10-rgb24-rot90", "model2-v10-q", "Rgb24", 2, 4, 90, False, 0),
        ("micro-m1-gray8", "micro-m1-l", "Gray8", 8, 2, 0, False, 0),
        ("micro-m4-rgba32-inverted", "micro-m4-l", "Rgba32", 6, 2, 0, True, 0),
        ("rmqr-r7x43-bgra32-padded", "rmqr-r7x43-m", "Bgra32", 4, 2, 0, False, 8),
        ("legacy-model1-v02-rgb24", "legacy-model1-v02-m", "Rgb24", 4, 4, 0, False, 0),
    ]
    fixtures = []
    for fixture_id, source_id, pixel_format, scale, quiet, rotation, inverted, padding in definitions:
        matrix = rotate_matrix(matrices[source_id], rotation)
        module_width = len(matrix[0]) + quiet * 2
        module_height = len(matrix) + quiet * 2
        width = module_width * scale
        height = module_height * scale
        bytes_per_pixel = {"Gray8": 1, "Rgb24": 3, "Rgba32": 4, "Bgra32": 4}[pixel_format]
        stride = width * bytes_per_pixel + padding
        data = bytearray()
        for pixel_row in range(height):
            module_row = pixel_row // scale - quiet
            for pixel_column in range(width):
                module_column = pixel_column // scale - quiet
                dark = (
                    0 <= module_row < len(matrix)
                    and 0 <= module_column < len(matrix[0])
                    and matrix[module_row][module_column] == "1"
                )
                if inverted:
                    dark = not dark
                value = 0 if dark else 255
                if pixel_format == "Gray8":
                    data.append(value)
                elif pixel_format == "Rgb24":
                    data.extend((value, value, value))
                elif pixel_format == "Rgba32":
                    data.extend((value, value, value, 255))
                else:
                    data.extend((value, value, value, 255))
            data.extend([0xCD] * padding)
        filename = "images/" + fixture_id + ".raw"
        (FIXTURE_ROOT / filename).write_bytes(data)
        fixtures.append({
            "id": fixture_id,
            "source_vector": source_id,
            "pixel_format": pixel_format,
            "width": width,
            "height": height,
            "stride": stride,
            "scale": scale,
            "quiet_zone_modules": quiet,
            "rotation_degrees_clockwise": rotation,
            "inverted": inverted,
            "padding_byte": 0xCD if padding else None,
            "file": filename,
            "sha256": sha256_bytes(data),
        })
    return fixtures


def rotate_matrix(matrix: list[str], degrees: int) -> list[str]:
    if degrees == 0:
        return matrix
    if degrees == 90:
        return ["".join(matrix[len(matrix) - 1 - row][column] for row in range(len(matrix))) for column in range(len(matrix[0]))]
    raise ValueError("unsupported fixture rotation")


def create_cases() -> tuple[list[dict[str, object]], dict[str, object]]:
    cases = [
        {"id": "model2-v01-l", "barcode": "qrcode", "profile": "Model2", "version": 1, "version_option": 1, "secure": 1, "error_correction": "L", "mask_option": 0, "payload": "01234567"},
        {"id": "model2-v09-m", "barcode": "qrcode", "profile": "Model2", "version": 9, "version_option": 9, "secure": 2, "error_correction": "M", "mask_option": 1, "payload": "VERSION 9 BOUNDARY"},
        {"id": "model2-v10-q", "barcode": "qrcode", "profile": "Model2", "version": 10, "version_option": 10, "secure": 3, "error_correction": "Q", "mask_option": 2, "payload": "version-10-boundary"},
        {"id": "model2-v26-h", "barcode": "qrcode", "profile": "Model2", "version": 26, "version_option": 26, "secure": 4, "error_correction": "H", "mask_option": 3, "payload": "VERSION 26"},
        {"id": "model2-v27-l", "barcode": "qrcode", "profile": "Model2", "version": 27, "version_option": 27, "secure": 1, "error_correction": "L", "mask_option": 4, "payload": "VERSION 27"},
        {"id": "model2-v40-h", "barcode": "qrcode", "profile": "Model2", "version": 40, "version_option": 40, "secure": 4, "error_correction": "H", "mask_option": 7, "payload": "VERSION 40"},
        {"id": "model2-v01-l-kanji", "barcode": "qrcode", "profile": "Model2", "version": 1, "version_option": 1, "secure": 1, "error_correction": "L", "mask_option": 5, "payload": "漢字", "eci": 20},
        {"id": "micro-m1-l", "barcode": "microqr", "profile": "Micro", "version": "M1", "version_option": 1, "secure": 1, "error_correction": "L", "mask_option": 0, "payload": "12345"},
        {"id": "micro-m2-l", "barcode": "microqr", "profile": "Micro", "version": "M2", "version_option": 2, "secure": 1, "error_correction": "L", "mask_option": 1, "payload": "HELLO"},
        {"id": "micro-m3-l", "barcode": "microqr", "profile": "Micro", "version": "M3", "version_option": 3, "secure": 1, "error_correction": "L", "mask_option": 2, "payload": "micro3"},
        {"id": "micro-m4-l", "barcode": "microqr", "profile": "Micro", "version": "M4", "version_option": 4, "secure": 1, "error_correction": "L", "mask_option": 3, "payload": "MICRO FOUR"},
    ]

    rmqr_sizes = [
        (7, 43), (7, 59), (7, 77), (7, 99), (7, 139),
        (9, 43), (9, 59), (9, 77), (9, 99), (9, 139),
        (11, 27), (11, 43), (11, 59), (11, 77), (11, 99), (11, 139),
        (13, 27), (13, 43), (13, 59), (13, 77), (13, 99), (13, 139),
        (15, 43), (15, 59), (15, 77), (15, 99), (15, 139),
        (17, 43), (17, 59), (17, 77), (17, 99), (17, 139),
    ]
    for index, (height, width) in enumerate(rmqr_sizes, 1):
        level = "M" if index % 2 else "H"
        cases.append({
            "id": f"rmqr-r{height}x{width}-{level.lower()}",
            "barcode": "rmqr",
            "profile": "rMQR",
            "version": f"R{height}x{width}",
            "version_option": index,
            "secure": 2 if level == "M" else 4,
            "error_correction": level,
            "payload": "12345",
        })

    parts = ["STRUCTURED-", "APPEND-", "VECTOR"]
    parity = 0
    for byte in "".join(parts).encode("utf-8"):
        parity ^= byte
    for index, payload in enumerate(parts, 1):
        cases.append({
            "id": f"structured-append-3-part-{index}",
            "barcode": "qrcode",
            "profile": "Model2",
            "version": 2,
            "version_option": 2,
            "secure": 1,
            "error_correction": "L",
            "mask_option": index - 1,
            "payload": payload,
            "structured_append": {"index": index, "count": 3, "parity": parity},
        })
    structured_append = {
        "id": "three-part-ascii",
        "part_vector_ids": [f"structured-append-3-part-{index}" for index in range(1, 4)],
        "part_payloads_utf8": parts,
        "reassembled_payload_utf8": "".join(parts),
        "parity": parity,
    }
    return cases, structured_append


def write_checksum_manifest() -> None:
    entries = []
    for path in sorted(FIXTURE_ROOT.rglob("*")):
        if path.is_file() and path.name != "SHA256SUMS":
            entries.append(f"{sha256_bytes(path.read_bytes())}  {path.relative_to(ROOT)}")
    (FIXTURE_ROOT / "SHA256SUMS").write_text("\n".join(entries) + "\n", encoding="ascii")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--zint", required=True, type=Path)
    parser.add_argument("--magick", default=Path("magick"), type=Path)
    arguments = parser.parse_args()
    verify_tools(arguments.zint, arguments.magick)
    prepare_output_directories()

    cases, structured_append = create_cases()
    vectors = []
    matrices = {}
    for case in cases:
        vector, matrix = run_zint_case(arguments.zint, case)
        vectors.append(vector)
        matrices[str(vector["id"])] = matrix

    legacy_vector, legacy_matrix = rasterize_legacy_model1(arguments.magick)
    vectors.append(legacy_vector)
    matrices[str(legacy_vector["id"])] = legacy_matrix

    damaged = create_damaged_fixtures(matrices)
    images = create_raw_images(matrices)
    layout_references = [
        {"profile": "LegacyModel1", "version": 1, "file": "sources/legacy-model1-v01-layout.svg", "sha256": LEGACY_SOURCE_HASHES["legacy-model1-v01-layout.svg"]},
        {"profile": "LegacyModel1", "version": 14, "file": "sources/legacy-model1-v14-layout.svg", "sha256": LEGACY_SOURCE_HASHES["legacy-model1-v14-layout.svg"]},
    ]
    manifest = {
        "schema_version": 1,
        "zint_commit": ZINT_COMMIT,
        "vectors": vectors,
        "legacy_boundary_layout_references": layout_references,
    }
    (FIXTURE_ROOT / "vectors.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False) + "\n", encoding="utf-8"
    )
    (FIXTURE_ROOT / "damaged.json").write_text(json.dumps(damaged, indent=2) + "\n", encoding="utf-8")
    (FIXTURE_ROOT / "structured-append.json").write_text(
        json.dumps(structured_append, indent=2) + "\n", encoding="utf-8"
    )
    (FIXTURE_ROOT / "images.json").write_text(json.dumps(images, indent=2) + "\n", encoding="utf-8")
    write_checksum_manifest()


if __name__ == "__main__":
    main()
