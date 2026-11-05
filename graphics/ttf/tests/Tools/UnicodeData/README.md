# Unicode data generator

This test-side tool pins the Unicode 17.0.0 inputs used to build the runtime
`UnicodeData.bin`. It uses only the Python standard library and never runs from
the production build.

The manifest records the archived UCD URL, retrieval date, Unicode license,
archive checksum, and a size and SHA-256 checksum for every runtime or
conformance member consumed by the project. Runtime inputs contribute to the
binary fingerprint; conformance inputs are verified but are not embedded.

The conformance fixtures read their vectors directly from the verified archive:

- `BidiTest.txt` and `BidiCharacterTest.txt` cover the bidirectional algorithm;
- `auxiliary/GraphemeBreakTest.txt` covers extended grapheme clusters;
- `auxiliary/LineBreakTest.txt` covers default line-break opportunities; and
- `NormalizationTest.txt` covers all explicit normalization vectors plus the
  required identity behavior for other assigned scalars, in bounded
  parameterized batches.

Word- and sentence-break files are not imported because the production library
does not implement those algorithms. No conformance text file is copied into C#
source or extracted into the repository.

Generate an artifact from the repository root:

```sh
python3 tests/Tools/UnicodeData/generate-unicode-data.py \
  --output /tmp/UnicodeData.bin \
  --print-fingerprint
```

Verify an existing artifact without rewriting it:

```sh
python3 tests/Tools/UnicodeData/generate-unicode-data.py \
  --output src/Unicode/UnicodeData.bin \
  --check
```

## Upgrading the Unicode version

A Unicode upgrade is an explicit compatibility change; do not replace only the
binary artifact or point the manifest at an unverified live download.

1. Choose the Unicode release and archive its UCD ZIP as
   `specs/unicode/UCD_<version>.zip`. Record its source URL, retrieval date,
   license, byte size, and SHA-256 before changing generated output.
2. Update `inputs.json`: set `unicodeVersion`, the archive file/URL/checksum,
   and every consumed member's size and SHA-256. Keep runtime and conformance
   roles explicit. Add or remove members only when the implementation or an
   official conformance suite requires that change.
3. Update the generator's default archive path and every versioned conformance
   archive name. Use `rg 'UCD_[0-9]+\\.[0-9]+\\.[0-9]+'` to find pins; do not
   leave tests reading a different release from the runtime artifact.
4. Generate a candidate and inspect the reported runtime-input fingerprint:

   ```sh
   python3 tests/Tools/UnicodeData/generate-unicode-data.py \
     --archive specs/unicode/UCD_<version>.zip \
     --output /tmp/UnicodeData.bin \
     --print-fingerprint
   ```

5. After reviewing the candidate, generate the committed artifact at
   `src/Unicode/UnicodeData.bin`, rerun the same command with `--check`, and
   confirm that a second generation is byte-identical. The production project
   embeds this reviewed file; it must not invoke the generator.
6. Update `specs/README.md` and `specs/SHA256SUMS`, then run the complete test
   suite. Review official bidi, grapheme, normalization, and other conformance
   count changes rather than merely accepting new counts. Add focused shaping
   regressions for any property change that affects glyphs, clusters, advances,
   offsets, or unsafe-break behavior.
7. Record intentional behavior changes in the shaping compatibility section of
   the root README and update the library release notes. Keep the previous pin
   until the artifact, manifest, archived inputs, documentation, and tests are
   ready to land together.

The generator compiles the runtime property inputs into tagged enumerated-range,
mapping, and sequence sections. Additional experimental payloads can be added
with repeatable `--section TAG=PATH` arguments. Tags are four uppercase ASCII
letters or digits. Sections are sorted by tag, offsets and lengths are unsigned
32-bit big-endian values, and payloads are padded to four-byte boundaries. The
48-byte header contains:

- the eight-byte `TTFUCD\0\0` magic;
- the unsigned 16-bit format version;
- the three-byte Unicode version and one reserved byte;
- the SHA-256 fingerprint of the verified runtime inputs; and
- the unsigned 16-bit section count.

Each 12-byte directory entry contains a four-byte tag, offset, and length. No
timestamp, machine path, ZIP metadata, or locale-sensitive value enters the
output. Enumerated sections contain a value-name pool and ordered non-default
ranges; mapping sections contain ordered code-point mappings and optional bracket
types; sequence sections contain ordered keys and a shared scalar pool.
