#!/usr/bin/env python3
"""Generate independent encoder fixtures. Requires Python 3.9+, Pillow 11.3.0,
JDK 17+, CMake and a C compiler. Downloads pinned sources into --work-dir;
normal dotnet tests consume only the committed output and need none of these.
"""
import argparse
import base64
import ctypes
import gzip
import hashlib
import json
import subprocess
import sys
import tarfile
import urllib.request
from pathlib import Path

from generate_golden_fixtures import ZINT_COMMIT, ZINT_VERSION_PREFIX, parse_matrix_dump, write_checksum_manifest

PINS = {
    "ZXing": ("zxing/zxing", "ba9e91fb31206f726ceb7694fa562c94d30f9ce9", "zxing"),
    "Nayuki": ("nayuki/QR-Code-generator", "993ccbb6765327f5c49939ba77736098c04ab81d", "QR-Code-generator"),
    "Segno": ("heuer/segno", "b11dc2913c22b22b3bc0a6efaa989904c44977bf", "segno"),
    "libqrencode": ("fukuchi/libqrencode", "715e29fd4cd71b6e452ae0f4e36d917b43122ce8", "libqrencode"),
    "rmqrcode": ("OUDON/rmqrcode-python", "9dbbc2c6163fde897431a57cad9e475bf1e56e48", "rmqrcode-python"),
}
LEVELS = {"None": 0, "Low": 0, "Medium": 1, "Quartile": 2, "High": 3}
MODES = {"Numeric": 0, "Alphanumeric": 1, "Byte": 2, "Kanji": 3}


def run(command, **kwargs):
    return subprocess.check_output([str(value) for value in command], text=True, **kwargs)


def sources(root):
    result = {}
    for name, (repository, commit, directory) in PINS.items():
        destination = root / f"{directory}-{commit}"
        if not destination.exists():
            archive = root / f"{name}-{commit}.tar.gz"
            with urllib.request.urlopen(f"https://codeload.github.com/{repository}/tar.gz/{commit}") as response:
                archive.write_bytes(response.read())
            with tarfile.open(archive) as tar:
                # Extract regular files/directories below this root. ZXing's Android
                # resource aliases are symlinks and are not used by its Java core.
                for member in tar.getmembers():
                    target = (root / member.name).resolve()
                    if not target.is_relative_to(destination):
                        raise ValueError(f"Unexpected archive entry: {member.name}")
                tar.extractall(root, members=[m for m in tar.getmembers() if m.isfile() or m.isdir()])
        result[name] = destination
    return result


def configurations():
    from segno import consts
    for version in (1, 2, 6, 7, 9, 10, 26, 27, 32, 40):
        for level in ("Low", "Medium", "Quartile", "High"):
            for mode, text in (("Numeric", "01234567890123"), ("Alphanumeric", "HELLO 42"),
                               ("Byte", "abc\x00ÿ"), ("Kanji", "漢字")):
                if mode == "Byte":
                    capacity = consts.SYMBOL_CAPACITY[version][getattr(consts, "ERROR_LEVEL_" + level[0])]
                    count = (capacity - 4 - (8 if version < 10 else 16)) // 8
                    text = "".join(chr(i * 73 % 256) for i in range(count))
                yield case("Model2", version, level, (version + LEVELS[level] + MODES[mode]) % 8,
                           17 + 4 * version, 17 + 4 * version, mode, text)
    for version, levels in ((1, ("None",)), (2, ("Low", "Medium")),
                            (3, ("Low", "Medium")), (4, ("Low", "Medium", "Quartile"))):
        for level in levels:
            for mode, text in (("Numeric", "12345"), ("Alphanumeric", "A"), ("Byte", "a"), ("Kanji", "漢")):
                if version == 1 and mode != "Numeric" or version == 2 and mode in ("Byte", "Kanji"):
                    continue
                if version == 3:
                    # The pinned Segno pads M3 with zero bits instead of alternating
                    # pad codewords. Maximum payloads avoid that oracle-specific difference.
                    counts = (23, 14, 9, 6) if level == "Low" else (18, 11, 7, 4)
                    text = ("1", "A", "a", "漢")[MODES[mode]] * counts[MODES[mode]]
                for mask in range(4):
                    yield case("Micro", version, level, mask, 9 + 2 * version, 9 + 2 * version, mode, text)
    version = 0
    for height in (7, 9, 11, 13, 15, 17):
        for width in (27, 43, 59, 77, 99, 139):
            if width == 27 and height not in (11, 13):
                continue
            for level in ("Medium", "High"):
                for mode, text in (("Numeric", "12345"), ("Alphanumeric", "A"), ("Byte", "a"), ("Kanji", "漢")):
                    yield case("rMQR", version, level, 4, width, height, mode, text)
            version += 1


def case(family, version, level, mask, width, height, mode, text):
    return dict(id=f"{family}-{version}-{level}-{mask}-{mode}", family=family, version=version,
                level=level, mask=mask, width=width, height=height, mode=mode, text=text, hashes={})


def digest(rows):
    return hashlib.sha256(("\n".join("".join(str(int(cell)) for cell in row) for row in rows) + "\n").encode("ascii")).hexdigest()


class QRcode(ctypes.Structure):
    _fields_ = [("version", ctypes.c_int), ("width", ctypes.c_int), ("data", ctypes.POINTER(ctypes.c_ubyte))]


def load_lib(root, source):
    build = root / "lib-build"
    run(["cmake", "-S", source, "-B", build, "-DCMAKE_POLICY_VERSION_MINIMUM=3.5",
         "-DWITH_TOOLS=OFF", "-DWITH_TESTS=ON", "-DWITHOUT_PNG=ON", "-DBUILD_SHARED_LIBS=ON"])
    run(["cmake", "--build", build, "--target", "qrencode", "-j", "4"])
    library = next(iter(sorted(build.glob("libqrencode*.dylib"))), None)
    if library is None:
        library = next(iter(sorted(build.glob("libqrencode.so*"))))
    lib = ctypes.CDLL(str(library))
    for name in ("QRinput_new2", "QRinput_newMQR"):
        method = getattr(lib, name)
        method.argtypes, method.restype = [ctypes.c_int, ctypes.c_int], ctypes.c_void_p
    lib.QRinput_append.argtypes = [ctypes.c_void_p, ctypes.c_int, ctypes.c_int, ctypes.c_char_p]
    lib.QRinput_append.restype = ctypes.c_int
    lib.QRinput_free.argtypes = [ctypes.c_void_p]
    lib.QRinput_free.restype = None
    for name in ("QRcode_encodeMask", "QRcode_encodeMaskMQR"):
        method = getattr(lib, name)
        method.argtypes, method.restype = [ctypes.c_void_p, ctypes.c_int], ctypes.POINTER(QRcode)
    lib.QRcode_free.argtypes = [ctypes.POINTER(QRcode)]
    lib.QRcode_free.restype = None
    return lib


def lib_hash(lib, c):
    micro = c["family"] == "Micro"
    input_data = (lib.QRinput_newMQR if micro else lib.QRinput_new2)(c["version"], LEVELS[c["level"]])
    if not input_data:
        raise ValueError(f"libqrencode rejected {c['id']}")
    output = None
    try:
        data = c["text"].encode("shift_jis" if c["mode"] == "Kanji" else "iso-8859-1")
        if lib.QRinput_append(input_data, MODES[c["mode"]], len(data), data) != 0:
            raise ValueError("libqrencode rejected segment")
        output = (lib.QRcode_encodeMaskMQR if micro else lib.QRcode_encodeMask)(input_data, c["mask"])
        if not output or output.contents.version != c["version"] or output.contents.width != c["width"]:
            raise ValueError("libqrencode changed requested dimensions")
        return digest([[output.contents.data[y * c["width"] + x] & 1 for x in range(c["width"])] for y in range(c["height"])])
    finally:
        if output:
            lib.QRcode_free(output)
        lib.QRinput_free(input_data)


def zxing_hashes(root, source, cases):
    classes = root / "java-classes"
    classes.mkdir(exist_ok=True)
    adapter = Path(__file__).parent / "oracles" / "ZxingMatrixOracle.java"
    run(["javac", "-d", classes, *sorted((source / "core/src/main/java").rglob("*.java")), adapter])
    lines = []
    for c in cases:
        if c["family"] == "Model2":
            lines.append("\t".join((c["id"], str(c["version"]), c["level"][0], str(c["mask"]), c["mode"],
                                    base64.b64encode(c["text"].encode("utf-8")).decode("ascii"))))
    return dict(line.split("\t") for line in run(["java", "-cp", classes, "ZxingMatrixOracle"], input="\n".join(lines) + "\n").splitlines())


def generate(root, zint):
    root.mkdir(parents=True, exist_ok=True)
    oracle_sources = sources(root)
    sys.path[:0] = [str(oracle_sources["Segno"]), str(oracle_sources["Nayuki"] / "python"), str(oracle_sources["rmqrcode"] / "src")]
    import segno.encoder
    from qrcodegen import QrCode, QrSegment
    import rmqrcode
    from rmqrcode.format.rmqr_versions import rMQRVersions
    if not run([zint, "--version"]).strip().startswith(ZINT_VERSION_PREFIX):
        raise ValueError("Expected pinned Zint version")
    lib = load_lib(root, oracle_sources["libqrencode"])
    cases = list(configurations())
    zx = zxing_hashes(root, oracle_sources["ZXing"], cases)
    for c in cases:
        if c["family"] != "rMQR":
            micro = c["family"] == "Micro"
            qr = segno.encoder.encode(c["text"], version=f"M{c['version']}" if micro else c["version"],
                error=None if c["level"] == "None" else c["level"][0], mode=c["mode"].lower(),
                mask=c["mask"], micro=micro, boost_error=False,
                encoding="shift_jis" if c["mode"] == "Kanji" else "iso-8859-1")
            c["hashes"]["Segno"] = digest(qr.matrix)
            c["hashes"]["libqrencode"] = lib_hash(lib, c)
            if not micro:
                c["hashes"]["ZXing"] = zx[c["id"]]
                if c["mode"] == "Kanji":
                    # Nayuki exposes the Kanji segment constructor but no text transcoder.
                    data = c["text"].encode("shift_jis")
                    bits = []
                    for i in range(0, len(data), 2):
                        code = data[i] * 256 + data[i + 1]
                        adjusted = code - (0x8140 if code < 0xA000 else 0xC140)
                        value = (adjusted >> 8) * 0xC0 + (adjusted & 255)
                        bits.extend((value >> bit) & 1 for bit in range(12, -1, -1))
                    segment = QrSegment(QrSegment.Mode.KANJI, len(c["text"]), bits)
                else:
                    segment = QrSegment.make_numeric(c["text"]) if c["mode"] == "Numeric" else QrSegment.make_alphanumeric(c["text"]) \
                        if c["mode"] == "Alphanumeric" else QrSegment.make_bytes(c["text"].encode("iso-8859-1"))
                ecc = (QrCode.Ecc.LOW, QrCode.Ecc.MEDIUM, QrCode.Ecc.QUARTILE, QrCode.Ecc.HIGH)[LEVELS[c["level"]]]
                qr = QrCode.encode_segments([segment], ecc, c["version"], c["version"], c["mask"], False)
                c["hashes"]["Nayuki"] = digest([[qr.get_module(x, y) for x in range(qr.get_size())] for y in range(qr.get_size())])
        else:
            qr = rmqrcode.rMQR(f"R{c['height']}x{c['width']}", getattr(rmqrcode.ErrorCorrectionLevel, c["level"][0]))
            qr.add_segment(c["text"], getattr(rmqrcode.encoder, c["mode"] + "Encoder"))
            try:
                qr.make()
            except KeyError as error:
                if qr.version_name() != "R17x43" or c["level"] != "Medium" or error.args != (21,):
                    raise
                c["known_failures"] = {"rmqrcode": "R17x43-M table declares 60 rather than 61 total codewords, requesting missing ECC polynomial 21."}
            else:
                reference_hash = digest(qr.to_list(with_quiet_zone=False))
                definition = rMQRVersions[qr.version_name()]
                error_level = getattr(rmqrcode.ErrorCorrectionLevel, c["level"][0])
                blocks = definition["blocks"][error_level]
                disagreement_reason = None
                if len({block["k"] for block in blocks}) > 1:
                    # Upstream _make_final_codewords breaks at the first short block,
                    # omitting the longer blocks' final data words. Record its output,
                    # but do not make a known corrupt matrix a production expectation.
                    disagreement_reason = "Unequal data block lengths: upstream interleaver breaks instead of continuing past a short block."
                elif sum(block["num"] * block["k"] * 8 for block in blocks) != definition["number_of_data_bits"][error_level]:
                    disagreement_reason = "R13x27-M block table allocates 14 data codewords despite declaring a 96-bit (12-codeword) data capacity."
                if disagreement_reason:
                    c["known_disagreements"] = {"rmqrcode": {"matrix_sha256": reference_hash, "reason": disagreement_reason}}
                else:
                    c["hashes"]["rmqrcode"] = reference_hash
        # Zint independently chooses the most compact mode for these single-mode payloads.
        # Model 2 byte payloads include Latin-1 and NUL; use the other four explicit-mode oracles there.
        if c["family"] != "Model2":
            data_file, matrix_file = root / "input.txt", root / "matrix.txt"
            data_file.write_bytes(c["text"].encode("utf-8"))
            command = [zint, "--barcode=microqr" if c["family"] == "Micro" else "--barcode=rmqr",
                       f"--vers={c['version'] if c['family'] == 'Micro' else c['version'] + 1}",
                       f"--secure={LEVELS[c['level']] + 1}", f"--input={data_file}", "--filetype=TXT", f"--output={matrix_file}"]
            if c["family"] == "Micro":
                command.append(f"--mask={c['mask']}")
            run(command)
            zint_rows = parse_matrix_dump(matrix_file, c["width"], c["height"])
            c["hashes"]["Zint"] = digest(zint_rows)
        if len(set(c["hashes"].values())) != 1:
            raise ValueError(f"Oracle disagreement: {c['id']}: {c['hashes']}")
        for oracle, disagreement in c.get("known_disagreements", {}).items():
            if disagreement["matrix_sha256"] == c["hashes"]["Zint"]:
                raise ValueError(f"Known {oracle} disagreement no longer reproduces: {c['id']}")
    document = {"oracles": {name: {"repository": repo, "commit": commit} for name, (repo, commit, _) in PINS.items()},
                "hash_encoding": "ASCII 0/1 rows, LF after every row, no quiet zone", "vectors": cases}
    document["oracles"]["Zint"] = {"repository": "zint/zint", "commit": ZINT_COMMIT}
    output = Path(__file__).parent / "Golden" / "cross-oracle-encoders.json.gz"
    output.write_bytes(gzip.compress(json.dumps(document, separators=(",", ":"), ensure_ascii=True).encode("ascii"), mtime=0))
    write_checksum_manifest()
    print(f"Wrote {len(cases)} cross-oracle vectors to {output}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work-dir", type=Path, required=True)
    parser.add_argument("--zint", type=Path, required=True)
    args = parser.parse_args()
    generate(args.work_dir.resolve(), args.zint.resolve())
