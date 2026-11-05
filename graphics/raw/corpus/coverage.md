# Corpus coverage after T116

This matrix distinguishes real-camera evidence from exact-profile synthetic
writer output. A generated file can prove deterministic writer structure and
pixel identity, but it cannot replace a camera-produced file or establish
vendor-application compatibility.

## Read profiles

| Read profile | Real camera sample | File hash | Decode golden | Render golden | Remaining gap |
|---|---|---|---|---|---|
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.read` | CC0 file in `files/` | Verified | Recorded | Recorded | Independent applications are unavailable on the T116 host. |
| `canon.eos-6d-mark-ii.cr2.mraw-3x2.firmware-1.0.2.read` | CC0 file in `files/` | Verified | Recorded | Recorded | Independent applications are unavailable on the T116 host. |
| `canon.eos-6d-mark-ii.cr2.sraw-3x2.firmware-1.0.2.read` | CC0 file in `files/` | Verified | Recorded | Recorded | Independent applications are unavailable on the T116 host. |
| `canon.eos-20d.cr2.raw-3x2.firmware-2.0.3.read` | CC0 file in `files/` | Verified | Recorded | Recorded | Independent applications are unavailable on the T116 host. |
| `canon.eos-1d.tiff.raw-3x2.firmware-1.4.0.read` | CC0 file in `files/` | Verified | Recorded | Recorded | Independent applications are unavailable on the T116 host. |
| `nikon.z-7.nef.uncompressed-14bit.fx.read.experimental` | Missing; private placeholder only | Not available | Exact-profile generated output recorded | Generated-output render recorded | A lawfully held, hash-pinned real-camera file and independent applications are required. |
| `nikon.z-8.nef.lossless-14bit.fx.read.experimental` | Missing; private placeholder only | Not available | Exact-profile generated output recorded | Generated-output render recorded | A lawfully held, hash-pinned real-camera file and independent applications are required. |

Every built-in camera read profile remains experimental and requires explicit
opt-in in v1.0.0 because no named independent decode application was available
for the release run. The two Nikon profiles have the additional gap that no
hash-pinned camera-produced sample is available. Their generated files confirm
that the project can reopen its own exact-profile output; they do not
independently confirm all maker-note values or camera encoder behavior.

## Write profiles

| Write profile | Exact-profile output | Structural reopen | Stored-pixel identity | Render golden | Remaining gap |
|---|---|---|---|---|---|
| `canon.eos-6d-mark-ii.cr2.raw-3x2.firmware-1.0.2.write.experimental` | `generated/canon-eos-6d-mark-ii-raw-3x2-write-golden.cr2` | Passed | Passed | Recorded | Named independent/vendor applications are unavailable on the T116 host. |
| `nikon.z-7.nef.uncompressed-14bit.fx.write.experimental` | `generated/nikon-z-7-uncompressed-14bit-fx-write-golden.nef` | Passed | Passed | Recorded | Real-camera input and named independent/vendor applications are required. |
| `nikon.z-8.nef.lossless-14bit.fx.write.experimental` | `generated/nikon-z-8-lossless-14bit-fx-write-golden.nef` | Passed | Passed | Recorded | Real-camera input and named independent/vendor applications are required. |

All writer outputs use exact built-in geometry and codecs, a deterministic
synthetic 14-bit pattern, fixed metadata, and the smallest JPEG preview from
the acquired CC0 EOS 6D Mark II file. The generator enables post-write
validation and lossless stored-pixel identity. All write profiles remain
experimental until a future run supplies independent application evidence.

Descriptor-driven generic RAW support has no built-in camera/write profile and
is therefore outside this profile inventory; its format permutations remain
covered by the synthetic codec tests under `tests/`.
