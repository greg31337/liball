# Universal AVIF library development plan

## Goal and completion definition

Build a self-contained, managed .NET 10 library that can inspect, validate, decode, render, create,
and rewrite AVIF files without native codecs or third-party production dependencies. “Universal” in
this plan means support for every normative AVIF 1.2 image form and every AV1 profile, bit depth,
chroma layout, and coding tool that can legally occur in AVIF, plus current HEIF/MIAF container
features needed by real files. It includes still images, collections, derived and auxiliary images,
progressive/layered images, grids, HDR gain maps, and timed image sequences.

Completion requires all of the following:

- Pure managed implementation under `src/`; only the .NET runtime and base class libraries may be
  referenced there.
- Conforming AV1 decoding for Main, High, and Professional profiles at 8, 10, and 12 bits, including
  monochrome, 4:2:0, 4:2:2, and 4:4:4 content.
- Conforming AV1 encoding for the same formats, with lossless and lossy still-image modes and timed
  sequence support. The encoder need not make every search decision available in the AV1 design
  space, but every bitstream it emits must be conforming and all advertised settings must work.
- AVIF item, collection, grid, layered, auxiliary, derived-image, gain-map, and sequence read/write
  support, with unknown non-essential data preserved when rewriting.
- Exact bounded parsing, deterministic diagnostics, configurable resource limits, and graceful
  rejection of malformed, unsupported-essential, cyclic, truncated, or hostile inputs.
- High-level conversion to common packed pixel formats and low-level access to decoded planar data,
  metadata, items, tracks, boxes, OBUs, layers, frames, and encoded payloads.
- Cross-platform behavior on every .NET 10 runtime architecture; optional runtime-intrinsic paths
  must always have managed scalar fallbacks.
- Conformance and differential tests against independent implementations. External tools and test
  packages are allowed only under `tests/`.

## Fixed implementation constraints

- Follow `syntax.md` exactly. Production API and implementation types are classes and enums; do not
  introduce structs, interfaces, records, properties, events, delegates, nullable value-type syntax,
  inheritance (except direct `Exception` types), generic declarations, `ref`/`out`/`in`, unsafe code,
  pointers, LINQ, iterator methods, local functions, pattern syntax, or `async`/`await`.
- Public state is exposed through documented fields and ordinary block-bodied methods. APIs use
  return objects instead of `out` parameters and synchronous state machines instead of async APIs.
- `Span<T>` and `ReadOnlySpan<T>` are preferred for bounded hot-path parsing, transforms, plane
  conversion, and caller-buffer APIs. Arrays and `Stream` provide ownership and I/O boundaries.
- Use checked arithmetic and explicit bounds checks before allocations and indexing. No parser may
  trust a box size, offset, item relationship, sample table, LEB128 value, image dimension, tile
  count, or decoded allocation size.
- Do not add dependencies to `src/avif.csproj`. Built-in APIs such as `Stream`, `ArrayPool<T>`,
  `System.Buffers.Binary`, `System.Numerics`, `System.Runtime.Intrinsics`, `Thread`, and `Parallel`
  are permitted where they fit the syntax policy.
- External NuGet packages, command-line codecs, and conformance corpora may be used by tests only.
- Do not create syntax-policy tests, source-scanning tests, performance tests, or benchmark projects.

## Research basis

The local source inventory and provenance are in `specs/README.md`. The feature set was cross-checked
against AVIF 1.2, the AV1 bitstream specification, AV1-ISOBMFF 1.3, current HEIF/MIAF records,
libavif 1.4-era APIs, and current libheif APIs.

Key conclusions from existing libraries:

- libavif establishes the focused baseline: all AV1 YUV formats and depths, alpha, ICC/Exif/XMP,
  CICP and CLLI, transforms, grids, progressive layers, animation timing, gain maps, sample-transform
  recipes, streaming/range I/O, strictness controls, resource limits, and opaque properties.
- libheif supplies important breadth often missed by AVIF-only wrappers: image collections,
  thumbnails and depth/auxiliary images, independent tile access, overlay/derived images, item and
  track relationships, sequence metadata, range-based streaming, and comprehensive security limits.
- Neither library is a suitable production dependency here because both delegate AV1 work to native
  codecs. This project therefore needs a native managed AV1 decoder and encoder, not a container-only
  wrapper.
- The latest standards landscape extends beyond older AVIF implementations: AVIF 1.2 defines `sato`
  sample transforms and `tmap`; ISO/IEC 23008-12 now includes tone-map derivation and the standardized
  low-overhead `mif3`/`mini` form. These are planned explicitly and capability-signaled.

## Architecture and delivery order

The implementation is split into layers with one-way dependencies:

1. Bounded byte/bit I/O, arithmetic, buffers, limits, and diagnostics.
2. ISO BMFF/HEIF parsing and writing, item graphs, properties, and track sample tables.
3. AV1 OBU syntax, semantic validation, entropy coding, decode pipeline, and frame storage.
4. Color conversion, metadata, image transforms, derived image rendering, and gain maps.
5. AV1 encoding, AVIF authoring, high-level reader/writer APIs, and rewrite operations.

The first usable milestone is a strict still-image decoder. Encoding follows only after decoded
pixels match independent decoders. Collections, animation, advanced derivations, and low-overhead
containers build on the same primitives rather than separate implementations.

## Complete feature inventory

### 1. Binary, I/O, and safety foundation

- Big-endian 8/16/24/32/64-bit integers; signed integers; fixed-point numbers; rationals; FourCCs;
  UUIDs; ISO epoch times; null-terminated and length-prefixed UTF-8 strings.
- MSB-first bit reading/writing, byte alignment, trailing-bit validation, UVLC/NS coding, signed
  coding, LEB128 with canonicality and overflow checks, AV1 symbol range decoding/encoding, and CDF
  update rules.
- Memory, seekable-stream, non-seekable-stream, incremental feed, range-oriented seekable input,
  caller-buffer output, and canonical buffered output paths.
- Array-pool-backed scratch memory, plane buffers with checked strides, bounded frame/reference
  pools, explicit copy/view operations, and predictable ownership.
- Limits for source bytes, box depth/count, item/property/reference counts, track/sample/frame counts,
  dimensions, pixels, tiles, metadata bytes, ICC/XMP/Exif sizes, recursion, decoded bytes, temporary
  bytes, and worker count.
- Structured diagnostics with severity, category, byte/bit offset, box path, item/track/frame/tile
  context, AV1 syntax context, standards reference, and stable error code.

### 2. ISO BMFF and HEIF container reading

- 32-bit, 64-bit, and to-end box sizes; `uuid`; FullBox version/flags; known container recursion;
  unknown-box skipping and optional raw preservation.
- File and compatibility brands: `avif`, `avis`, `avio`, `mif1`, `msf1`, `miaf`, `MA1B`, `MA1A`,
  `av01`, and recognized forward-compatible brands.
- Image item core: `ftyp`, `meta`, `hdlr`, `pitm`, `iloc` v0-v2, `iinf`, `infe` v2-v3, `iprp`,
  `ipco`, `ipma` v0-v1, `iref` v0-v1, `idat`, `mdat`, `dinf`, `dref`, `grpl`, `free`, and `skip`.
- Item construction methods, multi-extent payloads, zero-based and file/idat construction offsets,
  16/32-bit item IDs, shared extents, extent-index handling, location resolution, and overlap checks.
- Item types and references: `av01`, `grid`, `tmap`, `sato`, MIME/URI metadata, Exif, `auxl`, `thmb`,
  `dimg`, `prem`, `cdsc`, and unknown relationship preservation.
- Descriptive and transformative properties: `av1C`, `ispe`, `pixi` including extended syntax,
  `colr` (`nclx`, `rICC`, `prof`), `pasp`, `clap`, `irot`, `imir`, `auxC`, `a1op`, `lsel`, `a1lx`,
  `clli`, `mdcv`, `cclv`, `amve`, `reve`, `ndwt`, `cmin`, and `cmex`.
- Essential-property semantics, property ordering, duplicate/conflict rules, unknown essential
  rejection, unknown non-essential exposure, and opaque `uuid` properties.
- Primary, hidden, thumbnail, auxiliary alpha/depth, alternate, stereo, and arbitrary collection
  item discovery; entity groups including `altr` and `ster`.
- Full item dependency graph construction, cycle detection, maximum-depth enforcement, deduplicated
  decode dependencies, and self-containment checks.
- `mif3` `MinimizedImageBox` expansion to its equivalent `meta`/`mdat` model, compact AV1 config,
  alpha/gain-map fields, and canonical low-overhead validation.

### 3. Timed AVIF and AV1-in-ISOBMFF reading

- Movie/track/media hierarchy: `moov`, `mvhd`, `trak`, `tkhd`, `edts`, `elst`, `mdia`, `mdhd`,
  `minf`, `vmhd`, `stbl`, `stsd`, `av01`, `av1C`, `stts`, `stsc`, `stsz`, `stco`, `co64`, `stss`,
  `sdtp`, `sbgp`, and `sgpd`.
- AV1 sample groups `av1f`, `av1m`, `av1s`, and `av1M`; keyframe, dependency, switch-frame,
  multi-frame, and metadata signaling.
- Sample-table expansion with overflow validation; sample offsets/sizes; decode and presentation
  timing; edit-list-based repetition; finite, infinite, and unspecified repeat counts.
- Image sequence selection versus primary item selection; multiple tracks; alternate groups;
  associated alpha/depth tracks; matched duration/timescale validation.
- Sequential decode, indexed frame decode from nearest random-access point, keyframe queries,
  frame timing queries, encoded extent queries, and partial/range input requests.
- AV1 codec configuration record parsing, config OBU validation, sequence-header reconciliation,
  codecs parameter generation/parsing, HDR box/metadata OBU consistency, and sample OBU normalization.
- Graceful identification of fragmented/protected media. Fragmented AV1/CMAF and Common Encryption
  are inspectable and preservable, but decrypting or authoring encrypted media is outside AVIF 1.2
  rendering scope and returns an explicit unsupported-protection result.

### 4. AV1 bitstream syntax and validation

- Low-overhead and length-delimited OBU streams; temporal delimiters; sequence headers; temporal
  units; frame headers; redundant headers; tile groups; frames; metadata; tile lists; padding;
  extension headers; spatial/temporal IDs; operating points; and layer filtering.
- Sequence profile, level, tier, decoder model, timing, frame-id, order-hint, color config, film-grain,
  still-picture, and reduced-still-picture syntax.
- All frame types and display modes: key, inter, intra-only, switch, show-existing, showable/nonshown,
  error-resilient, frame-size override, reference-frame refresh, and random access.
- Frame size, render size, super-resolution, tiles, quantization, quantization matrices, segmentation,
  delta-q, delta-loop-filter, loop filter, CDEF, loop restoration, TX mode, reference mode, skip mode,
  warped motion, global motion, and film grain parameters.
- Metadata OBUs for HDR CLL, HDR MDCV, scalability, ITU-T T.35/HDR10+, timecode, and safe opaque
  exposure of unknown metadata types.
- AVIF-specific semantic constraints: exactly one matching sequence header per image item, legal
  sync sample structure, no forbidden tile-list OBU in AV1 samples, property/OBU agreement, alpha
  requirements, profile/brand limits, and MIAF conformance.

### 5. AV1 decoding process

- Boolean/range symbol decoder, default CDF tables, adaptive CDF selection/update, literal reads,
  tile context initialization, and frame-end context updates.
- 64x64 and 128x128 superblocks; every legal square and rectangular partition/block size; tile
  rows/columns/groups; above/left contexts; palette contexts; coefficient contexts; and availability.
- Intra modes: DC, vertical, horizontal, all directional angles with angle deltas, smooth variants,
  Paeth, filter intra, chroma-from-luma, palette, and intra block copy.
- Inter modes: nearest/near/new/global motion vectors; single and compound references; compound
  average, distance-weighted, wedge, and difference-weighted masks; inter-intra; OBMC; warped motion;
  motion-field estimation; motion-vector projection/stack/search; and reference scaling.
- Coefficient token decoding, scan orders, EOB, signs, dequantization, lossless mode, quant matrices,
  inverse transforms of all AV1 sizes/types, transform splitting, and residual reconstruction.
- Decode wrap-up in normative order: deblocking, CDEF, upscaling/super-resolution, Wiener/SGR loop
  restoration, motion-field/reference updates, output selection, and reference-frame management.
- Film-grain synthesis for 8/10/12-bit monochrome and color output with deterministic seeded output.
- All profiles and legal chroma sampling positions/ranges; odd dimensions; cropped/render dimensions;
  large images; multi-tile and multi-thread decode; spatial/temporal scalability; progressive layers;
  and optional AV1 large-scale tile decode outside AVIF samples.
- Incremental decode status, decoded-row/tile reporting, cancellation checkpoints, deterministic
  scalar fallback, and independent tile work scheduling within configured limits.

### 6. Pixels, color, HDR, and rendering

- Planar monochrome/YUV 4:2:0, 4:2:2, and 4:4:4 storage at 8/10/12/16 bits with explicit strides,
  ranges, chroma location, bit depth, ownership, and premultiplication state.
- Packed Gray/GrayAlpha, RGB/RGBA, BGR/BGRA, ARGB/ABGR, RGB565, 8-bit and 16-bit integer output,
  plus linear floating-point RGB output represented in caller-provided byte buffers.
- Correct full/limited-range normalization; H.273 color primaries, transfer characteristics, matrix
  coefficients, chroma positions; identity/RGB, BT.601/709/2020, YCgCo, ICtCp, PQ, and HLG paths.
- Configurable nearest, bilinear, and higher-quality separable chroma resampling; odd-size edge rules;
  RGB-to-YUV downsampling; bit-depth conversion; dithering; and alpha preservation.
- ICC v2/v4 profile validation and embedded-profile precedence; matrix/TRC and LUT transforms;
  PCS XYZ/Lab conversion; chromatic adaptation; rendering intents; CLUT interpolation; and safe
  preservation when a profile cannot be applied.
- HDR metadata: CLLI, MDCV, CCLV, ambient/mastering metadata, ITU-T T.35/HDR10+ raw payloads, and
  consistency checks between container properties and AV1 metadata OBUs.
- ISO 21496-1 gain map metadata, base/alternate headroom, per-channel gain/gamma/offset, base color
  flags, gain-map scaling, application to linear light, inverse creation, and SDR/HDR selection.
- `tmap` reconstruction, base/gain-map alternative grouping, metadata-version handling, and fallback
  to the base image when tone-map derivation is unsupported by a caller-selected policy.
- Transform application in normative order: clean aperture, rotation, mirror, then rendering;
  pixel-aspect reporting; Exif orientation reconciliation; and optional untransformed output.
- Alpha decode and association, premultiplied relationships, resize to master dimensions when legal,
  composition/unpremultiplication, depth and arbitrary auxiliary-plane exposure.
- Grid assembly with border cropping and independently addressable cells; tile-region decode; partial
  render; derived item caching; `sato` postfix evaluation with saturating 8/16/32/64-bit arithmetic;
  bit-depth extension recipes; alternate and stereo selection.

### 7. Metadata and preservation

- Exact Exif payload/TIFF-header offset validation, endian-aware IFD traversal, orientation lookup and
  update, safe unknown-tag preservation, size/cycle/depth limits, and raw payload access.
- XMP UTF-8/XML packet validation using built-in .NET XML APIs, packet preservation, standard field
  access, Exif/XMP reconciliation helpers, and rejection of unsafe external entity resolution.
- ICC raw profile access plus the color-transform behavior above.
- MIME and URI metadata items, content-description relationships, multiple metadata blocks per image,
  sequence metadata samples, unknown metadata OBUs, unknown item types, unknown boxes/properties, and
  lossless pass-through during rewrite when their referenced byte ranges remain valid.
- Container dump/model APIs for brands, boxes, items, extents, properties, references, groups, tracks,
  samples, OBUs, and validation findings without decoding pixels.

### 8. AV1 encoding

- Managed AV1 bit writer, symbol/range encoder, inverse CDF update, OBU construction, sequence/frame
  headers, metadata OBUs, tile groups, and AVIF-compliant low-overhead sample output.
- Still-picture and reduced-still-picture modes; Main/High/Professional profiles; 8/10/12-bit,
  monochrome and all legal chroma layouts; full/limited range; lossless and lossy operation.
- Transform/quantization pipeline, forward transforms, coefficient scans/tokenization, quantization
  matrices, segmentation, delta-q/delta-lf, skip decisions, and exact reconstruction buffer.
- Intra analysis for directional, smooth, Paeth, filter-intra, CfL, palette, and intraBC candidates;
  partition search across legal block shapes; configurable search effort and deterministic tie breaks.
- Inter analysis for sequences: reference selection, integer/subpixel motion estimation, compound and
  inter-intra choices, global/warped motion, keyframe placement, reference refresh, show-existing,
  and lag/reorder policy.
- In-loop filtering decisions for deblock, CDEF, super-resolution, and restoration, with reconstructed
  references matching decoder output.
- Constant-quality, explicit quantizer, mathematically lossless, target-size still, and target-bitrate
  sequence modes; separate alpha/gain-map quality; keyframe interval; speed/effort presets; tile
  selection; worker limit; and deterministic mode.
- Film-grain parameter carriage, HDR metadata OBUs, spatial layers/progressive images, operating
  points, and metadata/property consistency.
- Caller-supplied planar input and high-level packed RGB input with color conversion; independent
  color, alpha, auxiliary, gain-map, tile-cell, and layer encoding pipelines.

### 9. AVIF writing and rewriting

- Canonical `ftyp`/`meta`/`mdat` still file, `idat` option for small metadata/derived payloads, correct
  item IDs/extents/property associations/references, 32/64-bit offsets, and seekable/non-seekable
  output strategies.
- Correct brand selection for `avif`, `avis`, `avio`, `mif1`, `msf1`, `miaf`, `MA1B`, and `MA1A`;
  explicit general-profile fallback; self-validation before returning bytes.
- Single image, multi-image collection, primary/hidden items, thumbnails, alpha/depth/arbitrary
  auxiliary images, premultiplication, alternatives, stereo groups, metadata items, and opaque
  non-essential properties.
- AV1 coded items, grids, layered/progressive images with `a1op`/`lsel`/`a1lx`, tone-map/gain-map
  derived images, and arbitrary/recipe-based `sato` sample transforms.
- Timed `avis` tracks with frame durations, timescale, alpha/auxiliary tracks, keyframe/sample groups,
  finite/infinite repetition, sample tables, still primary fallback item, and inter/intra-only modes.
- Standardized `mif3`/`mini` authoring when all constraints are representable, with deterministic
  fallback to the regular `meta` form when they are not.
- Metadata-preserving rewrite: change primary item, remove/add metadata, normalize brands, update
  transforms, remux encoded AV1 without generation loss, reorder payloads, and preserve unknown safe
  non-essential structures.

### 10. Public usability and diagnostics

- One-call probe, decode, encode, and transcode entry points for byte arrays, spans, files, and streams.
- Stateful reader for inspect-then-decode, frame/item/tile random access, progressive/incremental feed,
  low-level encoded payload extraction, and explicit disposal through `Close()`.
- Stateful writer for incremental construction of items, grids, layers, metadata, and sequences.
- Strict, compatible, and permissive validation modes; separate unsupported-feature versus corrupt
  data results; warning collection; feature/capability report; no silent color/metadata loss.
- XML documentation for every public class, enum, enum member, field, constructor, and method.

## Public API completion tasks

- [DONE] T001: Extend the public one-call codec contract and implementation with uniquely named byte/span, stream, and file operations for probe, inspect, validate, planar decode, encode, write, and transcode while preserving caller ownership of streams and files.
- [DONE] T002: Add high-level packed-pixel decode and encode operations that accept caller-selected packed formats, integer or linear floating-point depth, color information, alpha mode, rendering options, encoder options, limits, and cancellation without requiring callers to assemble intermediate planar conversions.
- [DONE] T003: Add public seekable-stream, non-seekable-stream, and range-oriented reader entry points plus an interface-based random-access source that reports and fulfills exact missing byte ranges without exposing internal input types.
- [DONE] T004: Extend incremental reading to accept bounded out-of-order ranges as well as contiguous appends and to report independently useful parse, item, frame, tile, layer, and decoded-row availability.
- [DONE] T005: Add caller-buffer and destination-stream output operations for encoded AVIF data, encoded AV1 data, box data, item extents, samples, metadata, and rewrite results so callers can avoid mandatory whole-result byte-array copies.
- [DONE] T006: Add public AV1 codec-configuration and RFC 6381 codec-parameter models and operations that parse and generate profile, level, tier, bit depth, monochrome, chroma subsampling, chroma sample position, and color-range signaling and reconcile it with `av1C` and sequence headers.
- [DONE] T007: Expand capability and inspection results to report all detected item, collection, grid, layered/progressive, auxiliary, gain-map, sample-transform, sequence, low-overhead, fragmented, and protected-media features together with whether each feature is decodable, renderable, writable, rewritable, or only preservable.
- [DONE] T008: Make structured diagnostics uniformly accessible from probe, read, decode, encode, low-level AV1, writer, rewriter, incremental, color, metadata, and rendering results, including all available byte/bit, box, item, track, frame, tile, AV1 syntax, and standards-reference context.
- [DONE] T009: Expand decode results and stateful reader operations to return selected alpha, depth, arbitrary auxiliary images, gain maps, tone-map alternatives, progressive layers, metadata, and sequence-associated outputs without losing the parsed document or forcing callers to rediscover relationships.
- [DONE] T010: Add bounded extraction operations for complete and segmented item payloads, track samples, sample dependency ranges, configuration OBUs, metadata OBUs, layer payloads, and unknown metadata while retaining exact source offsets and independent ownership.
- [DONE] T011: Complete metadata authoring and rewriting APIs for MIME content encoding, URI metadata items, multiple metadata associations, content-description relationships, sequence metadata samples, unknown metadata OBUs, and safe opaque metadata preservation.
- [DONE] T012: Complete HDR and viewing-environment authoring APIs for CLLI, MDCV, CCLV, AMVE, REVE, nominal diffuse white, camera properties, ITU-T T.35/HDR10+ payloads, and container/AV1 metadata consistency.
- [DONE] T013: Expose the remaining AV1 and AVIF authoring controls for film-grain carriage, metadata OBUs, still versus inter-frame sequence policy, frame lag/reordering, show-existing frames, reference refresh, global/warped motion, operating points, spatial/temporal layers, progressive output, and independent color, alpha, auxiliary, gain-map, grid-cell, and layer quality.
- [DONE] T014: Add low-level authoring and remux APIs for caller-supplied conforming AV1 payloads, arbitrary collections and item relationships, entity groups, opaque non-essential properties, and safe unknown top-level boxes without requiring decode and re-encode.
- [DONE] T015: Add functional tests for every public facade, reader, incremental/range source, result, option, limit, cancellation, lifecycle, stream/file, and caller-buffer operation, covering success plus invalid argument, invalid state, closed, truncated, unsupported, limit-exceeded, cancelled, and I/O outcomes.
- [DONE] T016: Add functional tests for every public document, box, item, extent, property, reference, group, movie, track, sample, frame, layer, auxiliary, capability, diagnostic, codec-parameter, and payload-extraction operation using still, collection, low-overhead, progressive, timed, fragmented, protected, and malformed inputs.
- [DONE] T017: Add functional tests for every public planar and packed image, plane, pixel-buffer, color conversion, ICC, Exif, XMP, HDR, transform, grid, sample-transform, gain-map, tone-map, alpha, depth, and arbitrary auxiliary operation across all supported formats, depths, ranges, ownership modes, and boundary conditions.
- [DONE] T018: Add functional tests for every public AV1 OBU reader, AV1 decoder, AV1 encoder, box reader, box writer, AVIF writer, and rewriter operation, including all advertised coding controls, authoring forms, normalization choices, preservation modes, round trips, and failure states.
- [DONE] T019: Add end-to-end public-API coverage tests for one-call and stateful workflows over stills, collections, grids, layers, gain maps, sample transforms, sequences, metadata, packed pixels, transcoding, range input, and lossless remuxing, with independent implementations used from tests where needed to verify externally observable results.
