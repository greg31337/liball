#!/usr/bin/env python3
"""Generate exhaustive matrix behavior vectors with the repository's pinned Zint oracle.

Usage: python3 tests/Fixtures/generate_matrix_hashes.py --zint /path/to/zint
The output is deterministic gzip JSON, independent of the C# implementation.
"""

import argparse
import base64
import gzip
import hashlib
import json
import re
import subprocess
import tempfile
from pathlib import Path

from generate_golden_fixtures import (
    ZINT_COMMIT, ZINT_VERSION_PREFIX, parse_hex_section, parse_matrix_dump,
    reed_solomon_encode, write_checksum_manifest,
)


MICRO = [(1, "None", 1, 20, 2), (2, "Low", 1, 40, 5),
         (2, "Medium", 2, 32, 6), (3, "Low", 1, 84, 6),
         (3, "Medium", 2, 68, 8), (4, "Low", 1, 128, 8),
         (4, "Medium", 2, 112, 10), (4, "Quartile", 3, 80, 14)]


def micro_codewords(output, version, capacity, ecc):
    bits = re.search(r"Binary \([0-9]+\): ([01]+)", output).group(1)
    bits += "0" * min(2 * version + 1, capacity - len(bits))
    bits += "0" * min((-len(bits)) % 8, capacity - len(bits))
    pad = 0
    while capacity - len(bits) >= 8:
        bits += f"{0xEC if pad % 2 == 0 else 0x11:08b}"
        pad += 1
    bits += "0" * (capacity - len(bits))
    data = [int(bits[i:i + 8].ljust(8, "0"), 2) for i in range(0, capacity, 8)]
    return data + reed_solomon_encode(data, ecc)


def configurations():
    for version in range(1, 41):
        for secure, level in enumerate(("Low", "Medium", "Quartile", "High"), 1):
            for mask in range(8):
                yield "Model2", version, level, secure, mask, 0, 0
    for version, level, secure, capacity, ecc in MICRO:
        for mask in range(4):
            yield "Micro", version, level, secure, mask, capacity, ecc
    for size in range(32):
        for level, secure in (("Medium", 2), ("High", 4)):
            yield "rMQR", size, level, secure, 4, 0, 0


def generate(zint):
    version_text = subprocess.check_output([str(zint), "--version"], text=True).strip()
    if not version_text.startswith(ZINT_VERSION_PREFIX):
        raise ValueError(f"Expected {ZINT_VERSION_PREFIX}, got {version_text}")
    vectors = []
    with tempfile.TemporaryDirectory(prefix="qrcode-matrix-hashes-") as temporary:
        output_path = Path(temporary) / "matrix.txt"
        for family, version, level, secure, mask, capacity, ecc in configurations():
            barcode = {"Model2": "qrcode", "Micro": "microqr", "rMQR": "rmqr"}[family]
            command = [str(zint), f"--barcode={barcode}", f"--vers={version + 1 if family == 'rMQR' else version}",
                       f"--secure={secure}", "--data=12345", "--verbose", "--filetype=TXT", f"--output={output_path}"]
            if family != "rMQR":
                command.append(f"--mask={mask}")
            output = subprocess.check_output(command, text=True)
            if family == "Model2":
                match = re.search(r"Selected version: (\d+)-([LMQH]) \((\d+)x(\d+)\)", output)
                if int(match[1]) != version or match[2] != level[0]:
                    raise ValueError("Oracle changed the requested Model 2 configuration")
                height, width = int(match[3]), int(match[4])
            elif family == "Micro":
                match = re.search(r"Version: M(\d+)-([LMQH]), Size: (\d+)x(\d+)", output)
                if int(match[1]) != version or (version != 1 and match[2] != level[0]):
                    raise ValueError("Oracle changed the requested Micro configuration")
                height, width = int(match[3]), int(match[4])
            else:
                match = re.search(r"Selected version: (\d+) = R(\d+)x(\d+)-([MH])", output)
                if int(match[1]) != version + 1 or match[4] != level[0]:
                    raise ValueError("Oracle changed the requested rMQR configuration")
                height, width = int(match[2]), int(match[3])
            if family != "rMQR" and int(re.search(r"Mask: (\d+)", output)[1]) != mask:
                raise ValueError("Oracle changed the requested mask")
            rows = parse_matrix_dump(output_path, width, height)
            matrix_bytes = ("\n".join(rows) + "\n").encode("ascii")
            bits = "".join(rows)
            bits += "0" * ((-len(bits)) % 8)
            packed = bytes(int(bits[i:i + 8], 2) for i in range(0, len(bits), 8))
            codewords = micro_codewords(output, version, capacity, ecc) if family == "Micro" else parse_hex_section(output, "Data Stream")
            vectors.append({"id": f"{family}-{version}-{level}-{mask}", "family": family,
                            "version": version, "level": level, "mask": mask, "width": width, "height": height,
                            "codewords": base64.b64encode(bytes(codewords)).decode("ascii"),
                            "matrix_bits": base64.b64encode(packed).decode("ascii"),
                            "matrix_sha256": hashlib.sha256(matrix_bytes).hexdigest()})
    document = {"zint_commit": ZINT_COMMIT, "oracle": version_text, "payload": "12345",
                "matrix_packing": "row-major MSB-first, final byte padded with zero bits",
                "hash_encoding": "ASCII 0/1 rows, LF after every row, no quiet zone", "vectors": vectors}
    destination = Path(__file__).parent / "Golden" / "matrix-combinations.json.gz"
    destination.write_bytes(gzip.compress(json.dumps(document, separators=(",", ":")).encode("ascii"), mtime=0))
    write_checksum_manifest()
    print(f"Wrote {len(vectors)} vectors ({destination.stat().st_size} bytes) to {destination}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--zint", type=Path, required=True)
    generate(parser.parse_args().zint)
