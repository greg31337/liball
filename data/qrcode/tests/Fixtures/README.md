# QR-family golden fixtures

This corpus is immutable test input for the encoder, decoder, matrix,
recovery, rendering, and image-pipeline tasks that follow T008. It is not
loaded by production code.

## Coverage

- Model 2 versions 1, 9, 10, 26, 27, and 40 cover both ends of every
  character-count version group. The vectors vary ECC level and mask and
  include numeric, alphanumeric, byte, Kanji/ECI, and structured-append data.
- Micro QR M1 through M4 each have finalized data bits, data/ECC codewords,
  and a matrix.
- Every one of the 32 standardized rMQR sizes has a vector and matrix, with
  M and H error correction represented.
- Legacy Model 1 has a CC0 encoded Version 2 reference whose format, mask,
  payload, codewords, and matrix are extracted independently. CC0 Version 1
  and Version 14 functional-layout references cover its boundary geometry.
- Damaged matrices cover Model 2, Micro QR, rMQR, and legacy Model 1. Every
  mutation records the exact flipped row/column coordinates.
- Headerless Gray8, RGB24, RGBA32, and BGRA32 raw rasters cover square,
  rectangular, rotated, inverted, quiet-zone, and padded-stride cases.

`Golden/vectors.json` is the main manifest. Matrices use one ASCII `0`/`1`
row per module row with no quiet zone. `damaged.json`,
`structured-append.json`, and `images.json` describe the specialized assets.
`Golden/SHA256SUMS` covers every fixture and retained source artifact.

## Provenance and regeneration

Model 2, Micro QR, rMQR, and structured-append outputs come from Zint commit
`baa91878f488f472fee78585184ffcee07c1dd51`, already pinned in
`specs/SOURCES.md`. Generation fixes versions, ECC levels, and masks rather
than accepting automatic size or mask choices. The generator records final
data bitstreams, padded data codewords, complete ECC/interleaved codewords,
and matrices from the same oracle invocation.

The legacy encoded reference is the CC0 Wikimedia Commons file
[`QR Code Model 1 Example.svg`](https://commons.wikimedia.org/wiki/File:QR_Code_Model_1_Example.svg).
The Version 1 and Version 14 layouts are the corresponding CC0
[`Model 1 QR Version 1 Layout.svg`](https://commons.wikimedia.org/wiki/File:Model_1_QR_Version_1_Layout.svg)
and
[`Model 1 QR Version 14 Layout.svg`](https://commons.wikimedia.org/wiki/File:Model_1_QR_Version_14_Layout.svg).
Their exact downloaded bytes are retained under `Golden/sources` and pinned
by SHA-256. ImageMagick is used only to rasterize the encoded SVG on its exact
16-pixel module grid; the generator then independently decodes the Model 1
format word and traversal and requires the expected payload.

Build the pinned Zint CLI outside the repository, then run:

```sh
python3 tests/Fixtures/generate_golden_fixtures.py \
  --zint /path/to/pinned-zint-build/frontend/zint \
  --magick /path/to/magick
```

The generation script deletes and recreates only the three generated
directories under `tests/Fixtures/Golden`: `matrices`, `damaged`, and
`images`. It verifies the oracle version and all retained legacy source hashes
before producing output.

No test exists merely to assert that fixture files or project entries are
present. Functional tests should consume these files when their corresponding
`src/` implementations are added.

`Golden/matrix-combinations.json.gz` supplements the corpus with all 1,280
Model 2 version/ECC/mask combinations, all 32 Micro version/ECC/mask
combinations, and all 64 rMQR size/ECC combinations. Each vector records
the pinned Zint oracle's packed matrix, its SHA-256 over ASCII module rows
with a final LF, and full interleaved codewords for numeric payload `12345`.
Micro codewords are finalized independently in Python from Zint's emitted
data bits because its diagnostic output omits the final codewords.
The public matrix tests compare generated matrix hashes and independently
extract codewords from the oracle matrices. Regenerate this supplement with:

```sh
python3 tests/Fixtures/generate_matrix_hashes.py \
  --zint /path/to/pinned-zint-build/frontend/zint
```

## Cross-encoder comparisons (T137)

`Golden/cross-oracle-encoders.json.gz` records SHA-256 module-matrix hashes
from independent encoders. `QrEncoderOracleTests` encodes the same explicit
segments through `IQrEncoder` and compares against each oracle separately.
The fixture records source repositories and exact commits; generation never
loads the C# library. The normal test run needs no external programs or network.

- Model 2: ZXing, Nayuki, libqrencode, and Segno, with numeric, alphanumeric,
  byte, and Kanji segments; all ECC levels; masks spanning 0–7; versions
  1, 2, 6, 7, 9, 10, 26, 27, 32, and 40. Byte vectors fill the selected
  capacity and include binary values. Nayuki's low-level Kanji segment
  constructor receives bits converted from Python's Shift JIS mapping.
- Micro: Segno, libqrencode, and Zint for all supported version/ECC/mode
  combinations and all four masks.
- rMQR: Zint for all 32 sizes, both ECC levels, and all four payload modes;
  [rmqrcode-python](https://github.com/OUDON/rmqrcode-python/tree/9dbbc2c6163fde897431a57cad9e475bf1e56e48)
  is also run for every case, with the upstream limitations below.

The generator checks agreement before saving the fixture. Segno's pinned
`write_padding_bits` adds an extra zero byte when already aligned; its M3
padding also uses zero bits throughout. Model 2 byte and Micro M3 vectors
therefore use maximum payloads that avoid these padding differences. The
existing Zint corpus retains coverage of short, padded payloads. Every saved
comparison checks the complete matrix, and no oracle source is patched to
agree with the library under test.

The pinned rmqrcode-python `_make_final_codewords` stops interleaving when
the first short block is exhausted, losing the longer blocks' final data
words. For those configurations, the fixture's `known_disagreements` retains
the actual oracle hash and reason; it is not used as a correct encoder
expectation. Generation verifies this specific disagreement still occurs.
Its R13x27-M block table also assigns 14 data codewords despite declaring a
96-bit (12-codeword) capacity; those four mode cases are recorded the same
way. Zint supplies the complete-matrix expectation for these configurations.
R17x43-M also declares 60 instead of 61 total codewords and requests an
unavailable degree-21 ECC polynomial. Its four mode cases record the resulting
`KeyError` in `known_failures` and use Zint's full-matrix expectations.
All other rMQR cases must agree between both oracles.

Regeneration requires JDK 17+, CMake, a C compiler, and Python 3.9+ with
Pillow 11.3.0 (an import dependency of rmqrcode-python). The script downloads
the pinned sources and builds its test adapters in an external work directory:

```sh
python3 -m venv /tmp/qrcode-oracles-venv
/tmp/qrcode-oracles-venv/bin/pip install Pillow==11.3.0
/tmp/qrcode-oracles-venv/bin/python tests/Fixtures/generate_cross_oracle_fixtures.py \
  --work-dir /tmp/qrcode-oracles \
  --zint /path/to/pinned-zint-build/frontend/zint
```

The downloaded third-party sources and their licenses stay in that work
directory. The repository retains generated hashes and project-authored
adapters, without adding production dependencies.

## Cross-decoder comparisons (T145)

`Golden/cross-oracle-decoders.json.gz` records actual decoder output from
the ZXing, ZBar, and quirc revisions pinned in `specs/SOURCES.md`. The public
`QrDecoderOracleTests` decodes the saved matrices and compares payloads,
byte segments, and available version/ECC/mask metadata with those outputs.
Tests run offline without Java, native libraries, or an encoder dependency.

The 1,583 vectors comprise the complete Zint version/ECC/mask corpus, the
main golden vectors, and 160 Model 2 data-mode vectors. For the latter,
Segno regenerates each matrix and its hash must match the existing independent
cross-encoder fixture before any decoder runs. The generator never loads the
C# implementation. Numeric, alphanumeric, binary (including NUL), and Kanji
payloads cover all Model 2 versions and all ECC levels across the corpus.

- ZXing decodes the module grids with an explicit ISO-8859-1 fallback for
  bytes without ECI. Recorded text and byte-mode segments are checked separately.
- quirc decodes module grids and returns raw payload bytes, including Shift JIS
  bytes for Kanji, plus version, ECC, and mask metadata.
- ZBar scans four-pixel modules with a four-module quiet zone. Its binary
  option preserves bytes without ECI; explicit ECI can still produce UTF-8.
  Each saved result records whether it is compared with raw bytes or UTF-8 text.

The 1,450 Model 2 vectors have 1,450 matching ZXing results, 1,448 ZBar results,
and 1,447 quirc results. On the three individual structured-append parts,
quirc returns success with an empty payload; ZBar returns no symbol for two
parts. Their actual statuses and bytes are retained under `known_limitations`,
and all three parts still have ZXing and native payload expectations.
No incorrect oracle output becomes an expected payload. Micro (36), rMQR (96),
and legacy Model 1 (1) are retained as native conformance vectors because those
profiles are outside the scope of these pinned decoders.

Regenerate with Python 3.9+, JDK 17+, a C compiler, and iconv:

```sh
python3 tests/Fixtures/generate_decoder_oracle_fixtures.py \
  --work-dir /tmp/qrcode-decoder-oracles
```

The generator downloads pinned sources, builds the project-authored adapters,
and checks oracle agreement before writing the deterministic gzip fixture.
ZBar is built as a QR-only scanner with `-fno-strict-overflow`: its scanner
expresses negative pointer offsets through unsigned arithmetic, which otherwise
causes invalid-pointer optimization with Clang. No upstream source is patched.
Third-party source files and licenses remain in the external work directory.
