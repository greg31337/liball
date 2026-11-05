# Independent font oracle adapters

These opt-in adapters expose stable JSON around the independent tools used by
differential tests:

- HarfBuzz 14.3.0 for shaping and subsetting;
- fontTools 4.60.2 for parsing, static instancing, and subsetting;
- FreeType 2.13.2 through freetype-py 2.5.1 for unscaled outlines and metrics.

The versions match the existing captured differential oracles and are pinned in
`versions.json`. A mismatch is an error unless the caller deliberately passes
`--allow-version-mismatch` while investigating a newer oracle. The adapters,
Python dependencies, native libraries, and command-line programs are not
referenced by `src/`, `ttf.slnx`, or normal test setup.

## Environment

Create an isolated test-only Python environment and install the exact managed
bindings:

```sh
python3 -m venv /tmp/ttf-font-oracles
/tmp/ttf-font-oracles/bin/pip install \
  -r tests/Tools/FontOracles/requirements.txt
```

Install the pinned native HarfBuzz and FreeType releases separately. The
adapters never install packages, discover system fonts, download inputs, or
write anywhere except an explicitly supplied output/report path.

## HarfBuzz

Shape text into numeric glyph IDs, clusters, offsets, advances, and flags:

```sh
python3 tests/Tools/FontOracles/harfbuzz_adapter.py shape \
  /path/to/font.ttf \
  --text=office \
  --direction=ltr \
  --script=Latn \
  --language=en \
  --features=liga,kern \
  --report=/tmp/harfbuzz-shape.json
```

Produce and hash an independently generated subset:

```sh
python3 tests/Tools/FontOracles/harfbuzz_adapter.py subset \
  /path/to/font.ttf \
  --unicodes=41,42,20AC \
  --output=/tmp/harfbuzz-subset.ttf \
  --report=/tmp/harfbuzz-subset.json
```

## fontTools

Parse a font and query selected hexadecimal codepoints:

```sh
/tmp/ttf-font-oracles/bin/python \
  tests/Tools/FontOracles/fonttools_adapter.py inspect \
  /path/to/font.ttf \
  --codepoints=41,20AC
```

Create a static instance or subset:

```sh
/tmp/ttf-font-oracles/bin/python \
  tests/Tools/FontOracles/fonttools_adapter.py instance \
  /path/to/variable.ttf \
  --variations=wght=700 \
  --output=/tmp/fonttools-instance.ttf

/tmp/ttf-font-oracles/bin/python \
  tests/Tools/FontOracles/fonttools_adapter.py subset \
  /path/to/font.ttf \
  --unicodes=41,42,20AC \
  --output=/tmp/fonttools-subset.ttf
```

Unicode lists are hexadecimal; fontTools glyph-ID lists are decimal.

## FreeType

Capture one glyph's unscaled points, contour ends, tags, control box, face
metrics, and horizontal/vertical glyph metrics:

```sh
/tmp/ttf-font-oracles/bin/python \
  tests/Tools/FontOracles/freetype_adapter.py \
  /path/to/font.ttf \
  --codepoint=41 \
  --report=/tmp/freetype-glyph.json
```

Every report includes the input font size and SHA-256. Output fonts are written
atomically, checked for a supported font signature, and reported with their
size and SHA-256 so a differential test can pin the exact oracle input/output.
