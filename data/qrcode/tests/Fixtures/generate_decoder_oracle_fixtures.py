#!/usr/bin/env python3
"""Record pinned ZXing, ZBar and quirc results without loading the C# library.
Requires Python 3.9+, JDK 17+, a C compiler, and iconv (macOS or Linux).
Sources/builds live under --work-dir; dotnet tests need only the saved corpus.
"""
import argparse
import base64
import gzip
import hashlib
import json
import subprocess
import sys
import tarfile
import urllib.request
from pathlib import Path

from generate_golden_fixtures import write_checksum_manifest

PINS = {
    "ZXing": ("zxing/zxing", "ba9e91fb31206f726ceb7694fa562c94d30f9ce9"),
    "ZBar": ("mchehab/zbar", "2ea2ca585b10a07e528af08ddf8890756e059b5b"),
    "quirc": ("dlbeer/quirc", "927d680904dc95fdff4cd9d022eb374b438ff8f2"),
    "Segno": ("heuer/segno", "b11dc2913c22b22b3bc0a6efaa989904c44977bf"),
}
HERE = Path(__file__).resolve().parent


def run(command, **kwargs):
    return subprocess.check_output([str(x) for x in command], text=True, **kwargs)


def source(root, name):
    repo, commit = PINS[name]
    destination = root / (repo.split("/")[1] + "-" + commit)
    if not destination.exists():
        archive = root / (name + ".tar.gz")
        if not archive.exists():
            archive.write_bytes(urllib.request.urlopen(f"https://codeload.github.com/{repo}/tar.gz/{commit}").read())
        with tarfile.open(archive) as tar:
            members = [m for m in tar.getmembers() if m.isfile() or m.isdir()]
            for member in members:
                if not (root / member.name).resolve().is_relative_to(destination):
                    raise ValueError(member.name)
            tar.extractall(root, members=members)
    return destination


def build(root, sources):
    classes = root / "classes"
    classes.mkdir(exist_ok=True)
    run(["javac", "-d", classes, *sorted((sources["ZXing"] / "core/src/main/java").rglob("*.java")),
         HERE / "oracles/ZxingDecoderOracle.java"])
    build_dir = root / "native-build"
    build_dir.mkdir(exist_ok=True)
    # Minimal QR-only image scanner configuration; no upstream source is patched.
    (build_dir / "config.h").write_text("""#define ENABLE_QRCODE 1
#define HAVE_INTTYPES_H 1
#define HAVE_STDINT_H 1
#define HAVE_UNISTD_H 1
#define HAVE_ERRNO_H 1
#define HAVE_SYS_TIME_H 1
#define HAVE_ICONV 1
#define ICONV_CONST
#define PACKAGE_VERSION "0.23.93"
#define PACKAGE_NAME "zbar"
#define ZBAR_VERSION_MAJOR 0
#define ZBAR_VERSION_MINOR 23
#define ZBAR_VERSION_PATCH 93
""")
    zbar = sources["ZBar"]
    files = "config error symbol image convert refcnt img_scanner scanner decoder misc video video/null decoder/qr_finder qrcode/qrdec qrcode/qrdectxt qrcode/rs qrcode/isaac qrcode/bch15_5 qrcode/binarize qrcode/util".split()
    # ZBar's movedelta macro expresses negative pointer offsets through uintptr_t.
    # Preserve wrapping arithmetic; otherwise Clang can optimize the scan into
    # an invalid pointer. This is a build option, not an oracle source patch.
    command = ["cc", "-O2", "-fno-strict-overflow", "-w", "-I" + str(build_dir), "-I" + str(zbar / "include"),
               "-I" + str(zbar / "zbar"), "-I" + str(sources["quirc"] / "lib"),
               HERE / "oracles/NativeDecoderOracles.c"]
    command += [zbar / "zbar" / (f + ".c") for f in files]
    command += sorted((sources["quirc"] / "lib").glob("*.c"))
    command += ["-lm"] + (["-liconv"] if sys.platform == "darwin" else [])
    command += ["-o", build_dir / "decoder-oracles"]
    run(command)
    return classes, build_dir / "decoder-oracles"


def packed(rows):
    bits = [int(cell) for row in rows for cell in row]
    data = bytearray((len(bits) + 7) // 8)
    for i, bit in enumerate(bits):
        data[i // 8] |= bit << (7 - i % 8)
    return base64.b64encode(data).decode("ascii")


def vectors(segno_source):
    sys.path.insert(0, str(segno_source))
    import segno.encoder
    combinations = json.loads(gzip.decompress((HERE / "Golden/matrix-combinations.json.gz").read_bytes()))
    result = []
    for entry in combinations["vectors"]:
        c = {k: entry[k] for k in ("id", "family", "version", "level", "mask", "width", "height", "matrix_bits")}
        c.update(text="12345", raw_hex=b"12345".hex(), origin="matrix-combinations.json.gz")
        result.append(c)
    for entry in json.loads((HERE / "Golden/vectors.json").read_text())["vectors"]:
        rows = (HERE / "Golden" / entry["matrix_file"]).read_text().splitlines()
        text = entry["payload_utf8"]
        c = dict(id=entry["id"], family=entry["profile"], version=entry["version"] if isinstance(entry["version"], int)
                 else (entry["width"] - 9) // 2 if entry["profile"] == "Micro" else 0,
                 level={"L": "Low", "M": "Medium", "Q": "Quartile", "H": "High", "None": "None"}[entry["error_correction"]],
                 mask=entry["mask"], width=entry["width"], height=entry["height"], matrix_bits=packed(rows),
                 text=text, raw_hex=text.encode("shift_jis" if entry["eci"] == 20 else "utf-8").hex(), origin="vectors.json")
        if c["family"] == "Micro" and c["version"] == 1:
            # Zint labels the requested security setting L; M1 has detection-only
            # parity and the native API represents its ECC level as None.
            c["level"] = "None"
        if entry["structured_append"]:
            c["structured_append"] = entry["structured_append"]
        result.append(c)
    encoders = json.loads(gzip.decompress((HERE / "Golden/cross-oracle-encoders.json.gz").read_bytes()))
    for entry in encoders["vectors"]:
        if entry["family"] != "Model2":
            continue
        encoding = "shift_jis" if entry["mode"] == "Kanji" else "iso-8859-1"
        qr = segno.encoder.encode(entry["text"], version=entry["version"], error=entry["level"][0],
                                 mode=entry["mode"].lower(), mask=entry["mask"], micro=False, boost_error=False, encoding=encoding)
        rows = ["".join(str(cell) for cell in row) for row in qr.matrix]
        digest = hashlib.sha256(("\n".join(rows) + "\n").encode("ascii")).hexdigest()
        if digest != entry["hashes"]["Segno"]:
            raise ValueError("Encoder fixture mismatch: " + entry["id"])
        c = {k: entry[k] for k in ("family", "version", "level", "mask", "width", "height", "text", "mode")}
        c.update(id="modes-" + entry["id"], matrix_bits=packed(rows), raw_hex=entry["text"].encode(encoding).hex(),
                 origin="cross-oracle-encoders.json.gz (matrix regenerated and hash verified)")
        result.append(c)
    return result


def generate(root):
    root.mkdir(parents=True, exist_ok=True)
    sources = {name: source(root, name) for name in PINS}
    classes, native = build(root, sources)
    cases = vectors(sources["Segno"])
    modern = [c for c in cases if c["family"] == "Model2"]
    java_input = "".join(f"{c['id']}\t{c['width']}\t{c['matrix_bits']}\n" for c in modern)
    zx = {line.split("\t")[0]: line.split("\t")[1:] for line in
          run(["java", "-cp", classes, "ZxingDecoderOracle"], input=java_input).splitlines()}
    native_input = "".join(f"{c['id']} {c['width']} {base64.b64decode(c['matrix_bits']).hex()}\n" for c in modern)
    outputs = {line.split("\t")[0]: line.split("\t")[1:] for line in run([native], input=native_input).splitlines()}
    for c in cases:
        c["oracles"] = {}
        if c["family"] != "Model2":
            c["native_reason"] = "Profile outside the Model 2 scope of these pinned decoders."
            continue
        fields = zx[c["id"]]
        if fields[0] != "OK":
            raise ValueError((c["id"], "ZXing", fields))
        text = base64.b64decode(fields[1]).decode("utf-8")
        if text != c["text"]:
            raise ValueError((c["id"], "ZXing text", repr(text)))
        c["oracles"]["ZXing"] = dict(text=text, byte_segments=fields[2], level=fields[3])
        if int(fields[4]) >= 0:
            info = dict(index=int(fields[4]) >> 4, count=(int(fields[4]) & 15) + 1, parity=int(fields[5]))
            original = c["structured_append"]
            if info != dict(index=original["index"] - 1, count=original["count"], parity=original["parity"]):
                raise ValueError((c["id"], "ZXing structured append", info))
            c["oracles"]["ZXing"]["structured_append"] = info
        fields = outputs[c["id"]]
        for oracle, success, actual in (("quirc", fields[0] == "0", fields[1]), ("ZBar", fields[5] == "1", fields[6])):
            expected = c["raw_hex"]
            comparison = "raw"
            if oracle == "ZBar" and actual != expected and actual == c["text"].encode("utf-8").hex():
                comparison = "utf8"
                expected = actual
            if success and actual == expected:
                c["oracles"][oracle] = dict(comparison=comparison, hex=actual)
                if oracle == "quirc":
                    c["oracles"][oracle].update(version=int(fields[2]), level=["Medium", "Low", "High", "Quartile"][int(fields[3])], mask=int(fields[4]))
            elif "structured_append" in c and actual == "" and (
                    oracle == "quirc" and fields[0] == "0" or
                    oracle == "ZBar" and fields[5] == "0" and c["structured_append"]["index"] > 1):
                c.setdefault("known_limitations", {})[oracle] = dict(status=fields[0] if oracle == "quirc" else fields[5],
                    actual_hex=actual, reason="Individual structured-append part is not returned as the complete part payload by this decoder.")
            else:
                raise ValueError((c["id"], oracle, fields, expected))
    document = dict(schema_version=1, oracles={n: dict(repository=r, commit=p) for n, (r, p) in PINS.items()},
                    matrix_packing="row-major MSB-first, final byte zero-padded", vectors=cases)
    output = HERE / "Golden/cross-oracle-decoders.json.gz"
    output.write_bytes(gzip.compress(json.dumps(document, separators=(",", ":"), ensure_ascii=True).encode("ascii"), mtime=0))
    write_checksum_manifest()
    print(f"Recorded {len(cases)} vectors; " + ", ".join(f"{name}: {sum(name in c['oracles'] for c in cases)}" for name in ("ZXing", "ZBar", "quirc")))


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--work-dir", required=True, type=Path)
    generate(parser.parse_args().work_dir.resolve())
