# Corpus manifest

Six fixtures over 20 MB are intentionally omitted from this checkout and ignored
by Git. See [download and regeneration instructions](../REFERENCE-DOWNLOADS.md#large-raw-corpus-fixtures)
before running the affected corpus tests. Their original hashes and provenance
remain in the manifest; the size exclusion overrides their historical
`repository` availability labels.

`manifest.json` is the authoritative inventory of external, generated, and
private files used as conformance evidence. Its format is versioned by
`schemaVersion` and defined by `manifest.schema.json`. Paths are relative to
this directory and must never contain `..` components.

The manifest records three different kinds of identity:

- `hashes.fileSha256` identifies the exact container bytes;
- `hashes.decodedSamplesSha256` identifies the canonical decoded sample byte
  stream selected by the corresponding test; and
- `hashes.renderSha256` identifies a fully specified golden render.

Derived hashes remain `null` until their byte layout, render options, and
production path are fixed by corpus tooling. A materialized repository sample
must have a path, byte length, and file hash. A remote sample must have a
source URI and file hash before it can be downloaded. IDs and paths must be
unique, lowercase SHA-256 values are mandatory, and a file is never accepted
when its length or hash differs from the manifest.

For the current integer corpus, `decodedSamplesSha256` is SHA-256 over
`RawSensorImage.IntegerSamples` in row-major, pixel-interleaved order, encoding
every normalized sample as an unsigned little-endian 16-bit value. The width,
height, and channel count are supplied by the selected read profile. This
definition makes the golden digest independent of host byte order.

For repository samples with a recorded `renderSha256`, the digest is SHA-256
over `RawRgbImage.ByteSamples` from this fixed recipe: bilinear demosaic, no
white-balance adjustment, RawCamera color space, linear transfer, gamut
clipping, 8-bit output, orientation applied, and a 64-by-64 output. CFA inputs
use half-size reconstruction before resizing; non-CFA inputs do not. The
standalone generator applies this recipe after decoding every materialized
sample, so an allocation-limit failure or any render change prevents a new
digest from being published.

## Availability and rights

`availability` describes where bytes can be obtained, independently of the
redistribution decision:

| Value | Meaning |
|---|---|
| `repository` | The file is checked in at `path`. |
| `remote` | The file may be fetched from the recorded URI after verifying its hash. |
| `private` | A locally held, hash-pinned file exists but must not be committed. |
| `private-placeholder` | Required coverage is known, but no private file or hash is registered yet. |
| `missing` | Required coverage has no presently identified source. |

`rights.redistribution` is a separate legal status. Only `allowed` files with
documented evidence may be committed or published. `restricted`, `prohibited`,
and `unknown` files stay under `corpus/private/`, which is ignored by Git.
Hashes and non-identifying labels may be committed as private corpus evidence;
owner names, serial numbers, GPS data, and other identifying provenance must
not be added to the public manifest.

Private placeholders deliberately use a null path, size, and file hash. To
activate one locally, record the exact hash and length, change availability to
`private`, and use a `private/...` path. This must not change its redistribution
status unless written permission has also been recorded.

## Labels, profiles, and expectations

Every entry carries explicit format, make, model, camera ID, firmware, variant,
compression, and bit-depth labels. Unknown labels are `null`; they are never
guessed. `profileIds` links the evidence to exact read or write profiles.

Each compatibility level has one expectation:

- `required`: the corpus suite must demonstrate the capability;
- `unsupported`: the suite must demonstrate a stable unsupported result;
- `not-applicable`: the operation does not apply to this sample; or
- `not-evaluated`: no compatibility claim may be made yet.

Moving a capability from `not-evaluated` to `required` requires corresponding
corpus evidence. `vendorConfirmed` remains `not-evaluated` or `unsupported`
until a named application and exact version successfully exercise the sample;
an unavailable application does not establish evidence.

## Updating the corpus

1. Add or update the manifest entry before using a sample in a test.
2. Verify the source rights and record the evidence separately from its
   availability.
3. Compute SHA-256 over the unmodified bytes and record the exact byte length.
4. Store redistributable files under a repository path and private files only
   under `private/`.
5. Validate `manifest.json` against `manifest.schema.json`, verify unique IDs
   and paths, then run the corpus tests affected by the entry.

T113 established this contract and its initial evidence inventory. T114
materialized the redistributable samples and filled decode-golden hashes. T116
recorded canonical render hashes and published the tested application matrix
in `compatibility.md`; `coverage.md` documents the remaining gaps.

## Reproducing generated writer samples

The exact-profile writer outputs are produced with the public API by the
standalone `generator/` project. It is corpus tooling, not production code or
a test project. The generator requires the acquired CC0 EOS 6D Mark II RAW
sample because it reuses that file's smallest JPEG preview.

```sh
dotnet run --project corpus/generator/raw.CorpusGenerator.csproj --configuration Release
```

The command writes all three files under `generated/`, enables structural
post-write validation and stored-pixel identity, then prints container,
decoded-sample, and canonical-render hashes for every repository artifact.
[The coverage matrix](coverage.md) records what each artifact proves and what
evidence is still missing.
