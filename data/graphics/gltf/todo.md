# Universal Khronos glTF 2.0 toolkit development plan

## Objective

Build a dependency-free production C# toolkit for reading, representing, resolving, validating, inspecting, editing, transforming, converting, and deterministically writing Khronos glTF 2.x assets. The toolkit must cover JSON `.gltf` and binary `.glb` containers, the complete glTF 2.0.1 core model, lossless unknown extension/extras preservation, typed support for registered extensions, and replaceable hooks for features whose codecs cannot be implemented with built-in .NET alone.

The design is layered from byte-level container and accessor handling to a field-based document model and then to high-level validation, scene evaluation, transformation, optimization, and conversion services. `src/` will use only built-in .NET APIs. Tests may use external packages and Khronos conformance assets.

## Governing constraints

- Follow `syntax.md` even where existing code or ordinary modern C# practice differs.
- Put every public type under `src/Api`, in exactly the category directory required by `syntax.md`.
- Use public data fields rather than properties. Every data class has at least one public instance field, has no public methods, and does not implement an interface.
- Represent optional scalar values with explicit `Has...` boolean fields, or a documented sentinel only where `-1` is unambiguous. Do not use nullable value types.
- Use nullable reference types where absence must be represented. Do not use the null-forgiving operator.
- Declare no generic types or generic methods. Built-in generic collection types such as `List<T>` and `Dictionary<TKey,TValue>` remain appropriate.
- Give every method on an interface, and every combined interface set on an implementation class, a unique name. Do not use method overloading.
- Keep the API synchronous because `await` is outside the allowed syntax. Provide file, `Stream`, and byte-array entry points with distinct names.
- Use `ReadOnlySpan<byte>` and `Span<byte>` inside byte parsers, writers, accessor codecs, Base64 logic, and image-header inspection where their lifetime restrictions are suitable. Store owned bytes as `byte[]` or streams, never spans.
- Use `System.Text.Json`/`System.Text.Json.Nodes`, `System.Buffers.Binary`, `System.Numerics`, `System.IO`, `System.Uri`, and other built-in .NET facilities only.
- Do not embed Draco, Meshopt, Basis Universal/KTX2, WebP/AVIF, MikkTSpace, or mesh simplifier libraries in `src/`. Expose interfaces through which callers can provide such codecs or algorithms.
- Treat untrusted input as a primary use case: bounded JSON depth, byte lengths, collection counts, decoded resource sizes, URI schemes, and recursion; overflow-safe range checks; no implicit network access; deterministic errors.
- Public implementation classes expose one constructor and exactly the instance members declared by their interface set. Public static methods are never exposed.

## Research basis and resulting design choices

The downloaded inputs are inventoried in [`specs/SOURCES.md`](specs/SOURCES.md). The normative baseline is glTF 2.0.1 plus the current glTF 2.0 extension registry retrieved on 2026-08-27.

| Reference | Relevant feature surface | Design consequence |
| --- | --- | --- |
| Khronos glTF 2.0.1 specification and schemas | Core object model, GLB layout, binary alignment, coordinate system, animation, PBR, extension rules | Implement the complete core directly and validate both schema shape and cross-object/binary semantics. |
| cgltf | Compact core loader/writer, accessor unpacking, extras, many extensions; extra resources are caller-managed | Keep low-level reading small, but include a first-class resource resolver/writer so buffers and images are not an afterthought. |
| TinyGLTF | `.gltf`/`.glb`, file/memory input, data URIs, sparse accessors, callback-based URI/image I/O, hardened v3 parsing | Offer file/stream/bytes APIs, strict limits, resolver and codec interfaces, and sparse overlay support. |
| SharpGLTF | Separate low-level model, runtime evaluation, and creation/manipulation toolkit layers | Separate data, I/O, evaluation, validation, editing, and conversion contracts instead of one monolithic API. |
| glTF-Transform | Graph-managed indices/offsets, read-edit-write workflow, composable transforms, inspect/merge/partition/dedup/prune and codec integrations | Keep document references index-safe, provide clone/merge/compact operations and ordered transform plans, and place external algorithms behind hooks. |
| three.js GLTFLoader | Broad ratified extension consumption and pluggable Draco/KTX2/Meshopt decoding | Make extension discovery explicit and allow codecs to be registered without coupling the core model to them. |
| Khronos glTF Validator | JSON/GLB/schema/reference/binary/image/extension checks, structured issue codes and asset statistics | Return stable severity/code/JSON-pointer issues, collect multiple failures, and separate structural, semantic, resource, and extension passes. |

## Support levels

1. **Lossless syntax support:** parse, own, edit, clone, and re-emit every glTF core object, `extras`, registered or unknown `extensions`, unknown JSON object members, and unknown GLB chunks according to configurable preservation policy.
2. **Core semantic support:** fully understand, validate, inspect, evaluate, convert, and write every glTF 2.0.1 core feature.
3. **Registered extension support:** provide typed models, serialization, semantic validation, dependency declarations, and fallback behavior for ratified extensions; preserve and schema-validate release-candidate, multi-vendor, vendor, archived, and future extensions even when no typed behavior exists.
4. **Codec/algorithm support:** expose stable public hooks for compressed geometry, compressed images, image transcoding, tangent/UV generation, simplification, and custom transforms. Built-in lossless operations work without hooks; operations requiring a missing hook fail with a precise unsupported-feature error without damaging the document.

## Complete feature inventory

### 1. Containers, JSON, and streams

- Detect JSON glTF versus GLB by content, with an explicit format override and extension-independent behavior.
- Read from a path, `Stream`, or owned byte array; write to a path, `Stream`, or owned byte array.
- Parse UTF-8 JSON according to RFC 8259, reject malformed encodings and non-finite JSON numbers, and apply configurable depth/count/byte limits.
- Read GLB v2 little-endian headers, declared total length, ordered chunks, mandatory first JSON chunk, optional BIN chunk, 4-byte padding, JSON space padding, BIN zero padding, truncation, trailing data, duplicate chunks, and unknown chunk preservation/reporting.
- Preserve container provenance and original unknown chunks when requested.
- Serialize compact or indented JSON with invariant numeric formatting and valid escaping.
- Produce deterministic JSON: fixed schema-property order, stable dictionary/extension/attribute ordering, shortest round-trippable finite numbers, normalized negative zero policy, no timestamps, stable whitespace, and stable resource names.
- Optionally canonicalize free-form JSON values using RFC 8785-compatible ordering and number/string rules where compatible with glTF requirements.
- Write GLB with correct unsigned magic/chunk identifiers, checked 32-bit file/chunk sizes, exact padding, and a single embedded buffer.
- Support non-seekable input/output streams through bounded buffering; leave caller-owned streams open according to options.

### 2. Document and graph model

- Asset metadata: required version, optional minimum version, generator, copyright, extensions, extras, and unknown members.
- Root collections: accessors, animations, buffers, bufferViews, cameras, images, materials, meshes, nodes, samplers, scenes, skins, textures, default scene, extensions used/required, root extensions/extras, and unknown members.
- Stable integer indices matching glTF JSON, explicit absent-index representation, and utilities to remap all references after additions, removals, merge, prune, or reorder.
- Multiple scenes, shared root nodes, default scene selection, empty assets, names, extras, and extension data on every permitted object.
- Deep clone with independently owned JSON/binary data and merge with collision-free reference and resource remapping.
- Dependency graph enumeration, reverse references, orphan detection, and document statistics.

### 3. Scenes, nodes, coordinates, and transforms

- Scene root-node lists and acyclic node hierarchies with single-parent-per-scene validation where required by semantics.
- Nodes referencing children, mesh, camera, skin, morph weights, name, extensions, and extras.
- Mutually exclusive 4x4 column-major matrix versus translation/quaternion rotation/scale (TRS), with specification defaults.
- glTF right-handed coordinates, metres, radians, +Y up, +Z forward, -Z camera/light viewing direction, +X right, counter-clockwise front faces, and column-vector transform convention.
- Compose local matrix as `T * R * S`, calculate world transforms, transform points/vectors/normals, and detect invalid or non-decomposable matrices.
- Quaternion normalization and shortest-path spherical interpolation; determinant/winding implications of negative scale.
- Axis/unit conversion plans that transform nodes, geometry, normals/tangents, inverse bind matrices, animation channels, cameras, and winding consistently.
- Scene/world bounds including indexed primitives, sparse accessors, morph bounds policy, and instanced geometry hooks.

### 4. Meshes, primitives, attributes, indices, and morph targets

- Mesh primitive modes: points, lines, line loop, line strip, triangles, triangle strip, and triangle fan.
- Indexed and non-indexed primitives with unsigned byte/short/int index accessors and restart-value restrictions.
- Standard attributes: `POSITION`, `NORMAL`, `TANGENT`, `TEXCOORD_n`, `COLOR_n`, `JOINTS_n`, and `WEIGHTS_n`.
- Application attributes beginning with `_`, arbitrary set numbers, semantic parsing, duplicate detection, and extension-added semantics.
- Attribute component/type/normalization combinations, equal vertex counts, finite values, position min/max, unit normals, tangent handedness, normalized colors and weights, joint bounds, and primitive topology count rules.
- Morph targets for position, normal, and tangent deltas; target count/shape consistency; mesh and node weights; animation of weights; sparse target data.
- Primitive material references and default-material fallback.
- Raw, decoded, normalized, interleaved, and tightly packed vertex/index access.
- Lossless weld/unweld, primitive merge/split hooks, vertex reorder hooks, normal generation, tangent generation hook, UV generation hook, and simplification hook.

### 5. Accessors, buffers, and bufferViews

- Component types: signed byte, unsigned byte, signed short, unsigned short, unsigned int, and float.
- Accessor shapes: scalar, vec2, vec3, vec4, mat2, mat3, and mat4.
- Count, byte offset, normalized flag, optional bufferView, min/max arrays, name, extensions, extras, and unknown members.
- Element/component byte-size calculation, checked ranges, byte stride, interleaving, 4-byte vertex alignment, matrix column padding rules for 8/16-bit matrices, and little-endian conversion.
- Decode normalized signed and unsigned integers with exact glTF clamping rules; encode with defined rounding and overflow behavior.
- Typed reads for float components, signed integer components, unsigned integer components, indices, matrices, and raw element bytes; bounded copies to caller buffers internally using spans.
- Buffer declarations, external/data URI/GLB BIN storage, declared-versus-actual length, ownership, lazy/eager resolution, cache identity, and duplicate resource reuse.
- BufferView offsets, lengths, optional stride and target, overlap analysis, padding, and image/compression use restrictions.
- Sparse accessors with optional zero-initialized base, indices component types, strictly increasing unique indices, bounds, values layout, overlay decoding, dense materialization, and sparse generation from zero-heavy data.
- Accessor creation and buffer packing with alignment, interleaving policy, buffer partitioning, and automatic min/max calculation.

### 6. Materials and physically based rendering

- Metallic-roughness PBR model with base-color factor/texture, metallic and roughness factors, combined metallic-roughness texture channels, and specification defaults.
- Emissive factor/texture, normal texture scale, occlusion texture strength, alpha modes opaque/mask/blend, alpha cutoff, and double-sided state.
- Correct linear versus sRGB texture slot classification and channel use: base color/emissive sRGB; metallic, roughness, normal, occlusion, and extension data linear.
- Texture coordinate set selection and texture transform extension interaction.
- Material fallback behavior when unsupported extensions are optional, plus required-extension failure behavior.
- Typed ratified material extensions: anisotropy, clearcoat, dispersion, emissive strength, IOR, iridescence, sheen, specular, transmission, unlit, variants, and volume.
- Typed release-candidate diffuse transmission support and lossless archived specular-glossiness ingestion with conversion hook to metallic-roughness.
- Material inspection, equality-by-value for deduplication without `IEquatable`, channel usage, texture-slot enumeration, and safe material merging/palette hooks.

### 7. Textures, samplers, images, and image references

- Texture source and sampler references, default sampler behavior, names, extensions, extras, and unknown members.
- Minification/magnification filters, mipmap variants, wrap S/T modes, legal numeric constants, and completeness checks.
- Images from external URI, data URI, or bufferView plus MIME type; enforce URI/bufferView exclusivity and required MIME rules.
- Core PNG and JPEG references; MIME and magic-byte detection; built-in header inspection for dimensions, bit depth/components where practical, animation/color-profile warnings, truncation, and format mismatch.
- Extension image formats: KTX2/BasisU, WebP, and preserved AVIF/ASTC/DDS/vendor formats with typed source selection and fallback validation.
- RFC 3986 relative-reference resolution, percent encoding/decoding without double decoding, path/query/fragment preservation, and base-URI handling.
- RFC 2397 data URLs with media type parameters, percent-encoded payloads, RFC 4648 Base64, whitespace/padding/alphabet validation, size limits, and deterministic encoding.
- File URI and relative-file resolution with traversal/root/network policy; no network access unless a caller-provided resolver explicitly supports it.
- Resolver requests containing base URI, source URI, resource kind, declared MIME/length, and limits; responses containing owned bytes, effective URI, MIME, and cache key.
- Resource embedding, extraction, copying, renaming, collision handling, content hashing, deduplication, and stable relative URI generation.
- Image decode/transcode/resize/compress hooks; the core never assumes `System.Drawing` or a native codec.

### 8. Cameras

- Perspective cameras with vertical field of view, aspect ratio, near plane, optional finite far plane, and infinite perspective projection.
- Orthographic cameras with x/y magnitudes and near/far planes.
- Projection matrix calculation, finite/range checks, camera-node association, and glTF -Z viewing convention.

### 9. Skins

- Joint lists, optional skeleton root, optional inverse-bind-matrix accessor, names, extensions, extras, and unknown members.
- Default identity inverse bind matrices, matrix count/format validation, common-root and same-scene checks, cycle checks, and joint-index validation.
- Skinning palette calculation from joint world transforms and inverse bind matrices.
- `JOINTS_n`/`WEIGHTS_n` validation including multiple sets, non-negative normalized weights, duplicate effective influences, unused zero-weight joints, and joint bounds.
- Coordinate conversion and node-transform baking that correctly updates inverse bind matrices.

### 10. Animations

- Animations containing samplers and channels, with names, extensions, extras, and unknown members.
- Sampler input time accessors and output value accessors; STEP, LINEAR, and CUBICSPLINE interpolation.
- Channel targets for node translation, rotation, scale, and weights; pointer-based targets through `KHR_animation_pointer`.
- Strictly increasing finite non-negative key times, output shape/count checks, cubic in/value/out triplets, normalized rotation values, and morph-weight packing.
- Sampling at exact/between/outside key ranges with explicit clamp/loop policy; linear vectors/scalars, shortest-path quaternion slerp, and cubic Hermite evaluation with time scaling.
- Produce independent animation poses, apply poses to cloned documents, calculate clip duration, and resample/remove redundant keys with tolerances.
- Preserve shared samplers and validate animatable pointer targets and extension-defined interpolation/value constraints.

### 11. Extensions and extras

- Preserve free-form `extras` and extension JSON at every glTF property location without losing JSON number/string/object/array/null types.
- Preserve unknown object properties separately from extensions/extras so forward-compatible glTF 2.x content round-trips.
- Enforce extension naming, uniqueness, `extensionsRequired` subset of `extensionsUsed`, declaration of encountered extensions, and required unsupported-feature handling.
- Registry metadata includes name, status, object locations, dependencies/conflicts, fallback rules, JSON schemas, and codec requirements.
- Extension handlers can parse typed data, write typed data, validate JSON/semantics/resources, enumerate references, and participate in index remapping.
- Unknown extensions remain lossless; transforms either preserve them safely, invoke a handler to remap them, or refuse a potentially destructive operation.

Typed ratified support target:

- `KHR_animation_pointer`, `KHR_draco_mesh_compression`, `KHR_lights_punctual`.
- `KHR_materials_anisotropy`, `KHR_materials_clearcoat`, `KHR_materials_dispersion`, `KHR_materials_emissive_strength`, `KHR_materials_ior`, `KHR_materials_iridescence`, `KHR_materials_sheen`, `KHR_materials_specular`, `KHR_materials_transmission`, `KHR_materials_unlit`, `KHR_materials_variants`, `KHR_materials_volume`.
- `KHR_mesh_quantization`, `KHR_node_visibility`, `KHR_texture_basisu`, `KHR_texture_transform`, `KHR_xmp_json_ld`.
- `EXT_mesh_gpu_instancing`, `EXT_meshopt_compression`, `EXT_texture_webp`.

Typed release-candidate support target, versioned separately from ratified metadata:

- `KHR_gaussian_splatting`, `KHR_interactivity`, `KHR_materials_diffuse_transmission`, `KHR_meshopt_compression`, `KHR_node_hoverability`, and `KHR_node_selectability`.

Generic preservation plus available-schema validation target:

- Multi-vendor: `EXT_lights_ies`, `EXT_lights_image_based`, `EXT_mesh_manifold`.
- Vendor: `ADOBE_materials_clearcoat_specular`, `ADOBE_materials_clearcoat_tint`, `ADOBE_materials_thin_transparency`, `AGI_articulations`, `AGI_stk_metadata`, `CESIUM_primitive_outline`, `FB_geometry_metadata`, `GODOT_single_root`, `GRIFFEL_bim_data`, `MPEG_accessor_timed`, `MPEG_animation_timing`, `MPEG_audio_spatial`, `MPEG_buffer_circular`, `MPEG_media`, `MPEG_mesh_linking`, `MPEG_scene_dynamic`, `MPEG_texture_video`, `MPEG_viewport_recommended`, `MSFT_lod`, `MSFT_packing_normalRoughnessMetallic`, `MSFT_packing_occlusionRoughnessMetallic`, `MSFT_texture_dds`, and `NV_materials_mdl`.
- Additional registry descriptions present in the downloaded snapshot but not promoted in its registry table: `EXT_mesh_primitive_edge_visibility`, `EXT_mesh_primitive_restart`, `EXT_meshopt_compression` legacy location, and `EXT_texture_astc`.
- Archived read compatibility: `KHR_materials_pbrSpecularGlossiness`, `KHR_techniques_webgl`, and `KHR_xmp`; never emit these by default.
- Drafts referenced only by open proposals/review pull requests (`KHR_accessor_float64`, `KHR_audio_graph`, `KHR_collision_shapes`, `KHR_materials_subsurface`, `KHR_physics_rigid_bodies`, `KHR_texture_procedurals`, `EXT_texture_procedurals_mx_1_39`, `KHR_texture_video`) are preserved generically until a versioned specification and schema are added to the local registry.
- External-reference project/glXF: preserve external-reference declarations and provide resolver hooks; defer typed semantics until the proposal reaches a stable published schema.

### 12. Validation

- Validate JSON syntax and GLB structure before constructing unsafe ranges.
- Implement the glTF draft-04 JSON Schemas with required fields, types, array lengths, numeric/string limits, enums, dependencies, mutual exclusivity, and additional-property behavior.
- Validate all indices, references, graph cycles, extension declarations, default scene, object uniqueness, and cross-object compatibility.
- Validate buffers, bufferViews, accessors, sparse overlays, alignment, strides, matrix padding, declared lengths, finite values, min/max, and semantic-specific data.
- Validate mesh topology, attribute sets, morph targets, materials/texture slot color spaces, sampler values, images and MIME types, cameras, skins, and animations.
- Validate resolved resources optionally, distinguishing unavailable external resources from malformed resources.
- Run registered extension schemas and semantic handlers; report unsupported required extensions as errors and unknown optional extensions as information/warnings by policy.
- Return all issues with stable code, severity, human message, RFC 6901 JSON pointer, optional byte/resource location, and related object indices.
- Provide strict/fail-fast and collecting modes, severity thresholds, suppressed issue codes, maximum issue count, and deterministic issue ordering.
- Produce asset statistics: object counts, primitive/vertex/triangle counts, animation duration/keyframes, bounds, buffer/image byte use, draw-call estimates, extension use, sparse/interleaved data, and unresolved resources.
- Cross-map planned issue codes against the downloaded Khronos Validator issue catalog so that no validator category is silently omitted.

### 13. Deterministic writing and conversion

- Validate-before-write policy, or allow explicit best-effort writing with returned diagnostics.
- Rebuild all indices and binary offsets deterministically; never depend on dictionary hash order or machine culture.
- Preserve versus normalize modes for unknown JSON, omitted defaults, original numeric forms, unknown GLB chunks, data URIs, and resource placement.
- `.gltf` to `.glb`: resolve resources, consolidate/align buffers, embed the BIN chunk and selected images, update bufferViews/MIME types, and preserve valid external resources by option.
- `.glb` to `.gltf`: extract BIN/image resources or create data URIs, assign stable collision-free filenames, partition buffers, and rewrite references.
- `.gltf` to `.gltf` and `.glb` to `.glb`: repackage, normalize, validate, and deterministically rewrite.
- Resource modes: preserve, external, data URI, bufferView-embedded image, and GLB BIN; define incompatible combinations explicitly.
- Atomic path output using temporary sibling files and final replacement where supported; never leave a successful-looking partial target.
- Conversion result includes written primary bytes/path, resource manifest, hashes, diagnostics, and applied extension/codec information.

### 14. Editing, transformation, and optimization

- Ordered transform plan with validate-before, validate-after, clone-versus-in-place, unknown-extension safety, and rollback-on-failure semantics.
- Public custom transform hook receiving a context with document, resource set, diagnostics, index remapper, and registered codecs.
- Built-in lossless transforms: clone, merge, compact/reindex, prune unused objects, deduplicate equivalent objects/resources/accessors, consolidate/partition buffers, embed/extract resources, dense/sparse conversion, weld/unweld exact vertices, remove redundant animation keys, and canonicalize ordering.
- Built-in geometry/scene transforms: apply matrix, bake node transforms, flatten selected hierarchies, center/ground scene, unit/axis conversion, flip winding/tangent handedness, generate bounds/min/max, generate normals, join compatible primitives, and instance repeated meshes where safe.
- Hook-driven transforms: Draco/Meshopt encode/decode, KTX2/Basis/WebP/AVIF/image transcoding, tangent and UV generation, vertex-cache/fetch reorder, mesh simplification/LOD, quantization strategy, palette textures, and custom material workflow conversion.
- Quantize/dequantize core accessor data and manage `KHR_mesh_quantization`; preserve tolerances and validate resulting ranges.
- Transform reports list changed objects/resources, size deltas, warnings, skipped unsafe operations, extension changes, and deterministic hashes.

### 15. High-level workflows

- Create an empty standards-compliant document and add scenes, nodes, meshes, accessors, materials, images, skins, and animations through a factory/editor without manual index bookkeeping.
- Read-resolve-validate in one call while retaining separate low-level calls for applications needing control.
- Inspect without decoding image pixels or mutating the document.
- Evaluate a scene graph and animation pose without requiring a rendering engine.
- Convert containers and resource layouts in memory or on disk.
- Run reproducible transform pipelines and validate the result.
- Register resolvers, extension handlers, compression codecs, image codecs, and transform hooks through interface instances rather than delegates.

## Proposed public API and files

All names are provisional, but directory/category placement is mandatory. Each listed file contains only the stated category of public types and full permitted XML documentation.

### `src/Api/Data`

| Planned file | Public data classes and purpose |
| --- | --- |
| `GltfDocument.cs` | `GltfDocument`: root collections, default scene presence, root extensions/extras/unknown members, owned resource set, and container metadata. |
| `GltfAsset.cs` | `GltfAsset`: version/minVersion/generator/copyright and property JSON. |
| `GltfScene.cs` | `GltfScene`: root node indices and property JSON. |
| `GltfNode.cs` | `GltfNode`: child/reference indices, matrix/TRS arrays, weights, and property JSON. |
| `GltfMesh.cs` | `GltfMesh`, `GltfMeshPrimitive`, `GltfMorphTarget`, `GltfAttribute`: primitives, semantics, indices, material, mode, and targets. |
| `GltfAccessor.cs` | `GltfAccessor`, `GltfAccessorSparse`, `GltfAccessorSparseIndices`, `GltfAccessorSparseValues`: dense and sparse binary views. |
| `GltfBuffer.cs` | `GltfBuffer`, `GltfBufferView`: declarations, URI/offset/length/stride/target and property JSON. |
| `GltfMaterial.cs` | `GltfMaterial`, `GltfPbrMetallicRoughness`, `GltfTextureInfo`, `GltfNormalTextureInfo`, `GltfOcclusionTextureInfo`: core PBR data. |
| `GltfTexture.cs` | `GltfTexture`, `GltfSampler`, `GltfImage`: texture graph and image references. |
| `GltfCamera.cs` | `GltfCamera`, `GltfPerspectiveCamera`, `GltfOrthographicCamera`: camera choice and parameters. |
| `GltfSkin.cs` | `GltfSkin`: joints, skeleton, and inverse bind matrices. |
| `GltfAnimation.cs` | `GltfAnimation`, `GltfAnimationSampler`, `GltfAnimationChannel`, `GltfAnimationTarget`: clips and targets. |
| `GltfPropertyJson.cs` | `GltfPropertyJson`: reusable owned dictionaries for extensions, extras, and unknown members, embedded as a field rather than inherited. |
| `GltfContainerData.cs` | `GltfContainerData`, `GltfGlbChunk`: detected format, JSON bytes, BIN bytes, and preserved unknown chunks. |
| `GltfResource.cs` | `GltfResource`, `GltfResourceRequest`, `GltfResourceSet`, `GltfResourceManifestEntry`: resolved bytes and provenance. |
| `GltfReadOptions.cs` | `GltfReadOptions`, `GltfReadLimits`, `GltfReadResult`: format, preservation, resolver/handler/codec lists, limits, document, and diagnostics. |
| `GltfWriteOptions.cs` | `GltfWriteOptions`, `GltfWriteResult`: format/layout/determinism/validation/preservation/resource options and outputs. |
| `GltfValidation.cs` | `GltfValidationOptions`, `GltfValidationIssue`, `GltfValidationReport`, `GltfDocumentStatistics`: validation configuration and results. |
| `GltfAccessorData.cs` | `GltfAccessorReadRequest`, `GltfAccessorData`, `GltfAccessorWriteRequest`: typed component arrays, layout metadata, and creation input. |
| `GltfSceneEvaluation.cs` | `GltfSceneEvaluationOptions`, `GltfScenePose`, `GltfNodePose`, `GltfBounds`: matrices, evaluated transforms, and bounds. |
| `GltfAnimationEvaluation.cs` | `GltfAnimationEvaluationOptions`, `GltfAnimationPose`: time/wrap policy and sampled node/property values. |
| `GltfExtension.cs` | `GltfExtensionDescriptor`, `GltfExtensionContext`, `GltfExtensionResult`: registry status, locations, dependencies, JSON, and handler diagnostics. |
| `GltfMaterialExtensions.cs` | Typed field models for all targeted ratified/release-candidate material extensions and archived specular-glossiness reads. |
| `GltfSceneExtensions.cs` | Typed field models for lights, visibility/hover/select, GPU instancing, animation pointer, variants, Gaussian splatting, and interactivity entry points. |
| `GltfCompressionExtensions.cs` | Typed metadata models for Draco, Meshopt, mesh quantization, BasisU/KTX2, and WebP source selection. |
| `GltfMetadataExtensions.cs` | Typed XMP JSON-LD packet/assignment data and preserved registry extension payload metadata. |
| `GltfCodecData.cs` | `GltfCompressionRequest`, `GltfCompressionResult`, `GltfImageRequest`, `GltfImageInfo`, `GltfImageResult`: codec-neutral owned data. |
| `GltfEditData.cs` | `GltfEditOptions`, `GltfIndexRemap`, `GltfMergeResult`: graph-edit choices and mappings. |
| `GltfTransformData.cs` | `GltfTransformPlan`, `GltfTransformStep`, `GltfTransformContext`, `GltfTransformOptions`, `GltfTransformReport`: ordered transforms and results. |
| `GltfConversionData.cs` | `GltfConversionOptions`, `GltfConversionResult`: container/resource conversion inputs and manifest. |

### `src/Api/Contracts`

| Planned file | Interface and uniquely named operations |
| --- | --- |
| `IGltfReader.cs` | `IGltfReader`: `ReadFile`, `ReadStream`, `ReadContainerStream`, `ReadBytes`, `ReadContainerBytes`, and `ReadContainer`. |
| `IGltfWriter.cs` | `IGltfWriter`: `WriteFile`, `WriteStream`, `WriteContainerStream`, `WriteBytes`, `WriteContainerBytes`, and `WriteJsonText`. |
| `IGltfResourceResolver.cs` | `IGltfResourceResolver`: `ResolveResource`; frequent miss behavior returns a result status rather than requiring exceptions. |
| `IGltfValidator.cs` | `IGltfValidator`: `ValidateDocument`, `ValidateFile`, `ValidateBytes`, and `ValidateContainer`. |
| `IGltfAccessorCodec.cs` | `IGltfAccessorCodec`: distinct methods for float, signed, unsigned, index, matrix, and raw reads plus dense/sparse accessor creation. |
| `IGltfSceneEvaluator.cs` | `IGltfSceneEvaluator`: `EvaluateScene`, `CalculateBounds`, and `CalculateProjection`. |
| `IGltfAnimationEvaluator.cs` | `IGltfAnimationEvaluator`: `SampleAnimation`, `ApplyAnimationPose`, and `CalculateAnimationDuration`. |
| `IGltfExtensionHandler.cs` | `IGltfExtensionHandler`: `DescribeExtension`, `ReadExtension`, `WriteExtension`, `ValidateExtension`, and `RemapExtensionReferences`. |
| `IGltfCompressionCodec.cs` | `IGltfCompressionCodec`: `DecodeGeometry`, `EncodeGeometry`, `DecodeMeshoptData`, and `EncodeMeshoptData`. |
| `IGltfImageCodec.cs` | `IGltfImageCodec`: `InspectImage`, `DecodeImage`, `EncodeImage`, and `TranscodeImage`. |
| `IGltfDocumentEditor.cs` | `IGltfDocumentEditor`: `CreateDocument`, `CloneDocument`, `MergeDocument`, `CompactDocument`, and `RebuildReferences`. |
| `IGltfTransformer.cs` | `IGltfTransformer`: `TransformDocument`. |
| `IGltfTransformHook.cs` | `IGltfTransformHook`: `ApplyTransformHook`. |
| `IGltfInspector.cs` | `IGltfInspector`: `InspectDocument` and `EnumerateDependencies`. |
| `IGltfConverter.cs` | `IGltfConverter`: `ConvertFile`, `ConvertStream`, `ConvertBytes`, and `ConvertDocument`. |

### `src/Api/Enums`

| Planned file | Public enum |
| --- | --- |
| `GltfContainerFormat.cs` | `GltfContainerFormat`: auto, JSON glTF, GLB. |
| `GltfDuplicateMemberPolicy.cs` | `GltfDuplicateMemberPolicy`: reject duplicates, keep first, or keep last. |
| `GltfComponentType.cs` | `GltfComponentType`: the six core numeric constants. |
| `GltfAccessorType.cs` | `GltfAccessorType`: scalar/vector/matrix shapes. |
| `GltfPrimitiveMode.cs` | `GltfPrimitiveMode`: all seven primitive modes. |
| `GltfBufferViewTarget.cs` | `GltfBufferViewTarget`: absent/array/element-array semantics. |
| `GltfAlphaMode.cs` | `GltfAlphaMode`: opaque, mask, blend. |
| `GltfSamplerFilter.cs` | `GltfSamplerFilter`: legal mag/min filters including mip variants. |
| `GltfSamplerWrap.cs` | `GltfSamplerWrap`: clamp, mirrored repeat, repeat. |
| `GltfCameraType.cs` | `GltfCameraType`: perspective, orthographic. |
| `GltfAnimationInterpolation.cs` | `GltfAnimationInterpolation`: step, linear, cubic spline. |
| `GltfAnimationPath.cs` | `GltfAnimationPath`: translation, rotation, scale, weights, pointer. |
| `GltfValidationSeverity.cs` | `GltfValidationSeverity`: information, warning, error. |
| `GltfUriKind.cs` | `GltfUriKind`: absent, relative, absolute, file, data, network, custom. |
| `GltfResourceKind.cs` | `GltfResourceKind`: buffer, image, schema, extension, other. |
| `GltfImageFormat.cs` | `GltfImageFormat`: unknown, PNG, JPEG, KTX2, WebP, AVIF, DDS, ASTC, custom. |
| `GltfResourceMode.cs` | `GltfResourceMode`: preserve, external, data URI, bufferView, GLB BIN. |
| `GltfExtensionStatus.cs` | `GltfExtensionStatus`: unknown, vendor, multi-vendor, draft, release candidate, ratified, archived. |
| `GltfTransformKind.cs` | `GltfTransformKind`: all built-in and hook-driven transform plan operations. |
| `GltfAnimationWrapMode.cs` | `GltfAnimationWrapMode`: clamp, loop. |
| `GltfOperationStatus.cs` | `GltfOperationStatus`: success, skipped, unavailable, invalid, failed. |

### `src/Api/Static`

| Planned file | Static data-only class |
| --- | --- |
| `GltfConstants.cs` | `GltfConstants`: versions, GLB magic/chunk IDs, alignments, limits/default sentinel values. |
| `GltfSemantics.cs` | `GltfSemantics`: standard attribute and animation-path strings. |
| `GltfMimeTypes.cs` | `GltfMimeTypes`: container, buffer, and known image media types. |
| `GltfExtensionNames.cs` | `GltfExtensionNames`: constants for every extension named in the downloaded registry snapshot. |
| `GltfIssueCodes.cs` | `GltfIssueCodes`: stable public validation and operation issue-code strings. |

### `src/Api/Exceptions`

| Planned file | Direct `System.Exception` subclass |
| --- | --- |
| `GltfParseException.cs` | Malformed JSON/GLB or unsafe declared ranges that prevent model construction. |
| `GltfResourceException.cs` | Required buffer/image resolution or data-URI failures. |
| `GltfValidationException.cs` | Strict-mode validation failure carrying a concise message; collecting validation uses reports. |
| `GltfWriteException.cs` | Invalid or unrepresentable output/container/resource layout. |
| `GltfUnsupportedFeatureException.cs` | Missing required extension, codec, or transform hook. |

### `src/Api/Classes`

| Planned file | Public implementation class and interface set |
| --- | --- |
| `GltfReader.cs` | `GltfReader : IGltfReader`. |
| `GltfWriter.cs` | `GltfWriter : IGltfWriter`. |
| `FileGltfResourceResolver.cs` | `FileGltfResourceResolver : IGltfResourceResolver`; relative/file/data support only, policy-bound. |
| `GltfValidator.cs` | `GltfValidator : IGltfValidator`. |
| `GltfAccessorCodec.cs` | `GltfAccessorCodec : IGltfAccessorCodec`. |
| `GltfSceneEvaluator.cs` | `GltfSceneEvaluator : IGltfSceneEvaluator`. |
| `GltfAnimationEvaluator.cs` | `GltfAnimationEvaluator : IGltfAnimationEvaluator`. |
| `GltfExtensionHandler.cs` | `GltfExtensionHandler : IGltfExtensionHandler`; built-in registry dispatcher. |
| `GltfImageCodec.cs` | `GltfImageCodec : IGltfImageCodec`; header inspection and core passthrough only. |
| `GltfDocumentEditor.cs` | `GltfDocumentEditor : IGltfDocumentEditor`. |
| `GltfTransformer.cs` | `GltfTransformer : IGltfTransformer`. |
| `GltfInspector.cs` | `GltfInspector : IGltfInspector`. |
| `GltfConverter.cs` | `GltfConverter : IGltfConverter`. |

### Internal implementation files outside `src/Api`

Internal types remain non-public and are organized by mechanism:

- `src/Internal/Containers/`: `GlbReader.cs`, `GlbWriter.cs`, `ContainerDetector.cs`, `ChunkLayout.cs`.
- `src/Internal/Json/`: `GltfJsonReader.cs`, `GltfJsonWriter.cs`, `DeterministicJsonWriter.cs`, `JsonValueCloner.cs`, `JsonPointerBuilder.cs`, `Draft4SchemaValidator.cs`.
- `src/Internal/Binary/`: `AccessorLayout.cs`, `AccessorDecoder.cs`, `AccessorEncoder.cs`, `SparseAccessorOverlay.cs`, `BufferPacker.cs`, `IndexRemapper.cs`.
- `src/Internal/Resources/`: `UriReference.cs`, `DataUriCodec.cs`, `ResourceCache.cs`, `ResourceNameAllocator.cs`, `ImageHeaderInspector.cs`.
- `src/Internal/Validation/`: category rule files for document, references, resources, buffers/accessors, mesh, material/texture/image, scene/camera, skin, animation, and extensions.
- `src/Internal/Evaluation/`: `TransformMath.cs`, `SceneGraphEvaluator.cs`, `AnimationSampler.cs`, `BoundsCalculator.cs`, `SkinEvaluator.cs`.
- `src/Internal/Extensions/`: registry, core typed serializers/validators, schema loader, unknown-extension preservation, and codec adapters.
- `src/Internal/Editing/`: clone, merge, reference traversal, compaction, dependency graph, and transactional working-copy logic.
- `src/Internal/Transforms/`: one internal class per built-in transform plus hook dispatch and report aggregation.
- `src/Internal/Conversion/`: resource layout planner, glTF/GLB conversion pipeline, atomic path writer, and manifest builder.

## Implementation tasks

### Foundation and public model

- [DONE] T001: Establish the internal namespace/directory skeleton and shared checked arithmetic, finite-number, 4-byte alignment, collection-limit, and owned-byte helpers using only built-in .NET APIs.
- [DONE] T002: Implement all planned public enums with glTF numeric constants where specified, and ensure each enum is consumed by a public data class or interface.
- [DONE] T003: Implement `GltfConstants`, `GltfSemantics`, `GltfMimeTypes`, `GltfExtensionNames`, and `GltfIssueCodes` as static data-only classes with documented const/readonly fields and no methods.
- [DONE] T004: Implement the complete core field-based document model from asset/root through scenes, nodes, meshes, accessors, buffers, materials, textures, cameras, skins, and animations, including explicit scalar-presence fields, schema defaults, property JSON, and unknown members.
- [DONE] T005: Implement resource, container, read/write, validation, accessor, evaluation, extension, codec, edit, transform, inspection, and conversion request/result data classes with owned mutable data and unambiguous defaults.
- [DONE] T006: Implement typed data classes for all targeted ratified and release-candidate extensions, keeping common glTF property state by containment rather than forbidden inheritance.
- [DONE] T007: Implement all public operation interfaces with unique non-overloaded synchronous method names and parameter/result data classes suitable for file, stream, and byte-array workflows.
- [DONE] T008: Implement each public exception as a direct `System.Exception` subclass with one documented constructor and stable failure-message conventions.
- [DONE] T009: Add the public implementation-class shells, each with one constructor and no public instance member beyond its declared interface methods, wired to internal collaborators without public static factories.

### Container and JSON layer

- [DONE] T010: Implement content-based `.gltf`/GLB detection from a byte prefix and explicit format overrides, including empty, whitespace-only, BOM, malformed magic, and extension-mismatch cases.
- [DONE] T011: Implement the bounded GLB v2 header reader with little-endian magic/version/length parsing, overflow checks, truncation checks, and precise byte-offset errors.
- [DONE] T012: Implement GLB chunk iteration and validation for JSON-first ordering, JSON/BIN uniqueness, 4-byte lengths, padding bytes, total-length agreement, trailing bytes, and configurable unknown-chunk preservation.
- [DONE] T013: Implement the GLB writer with checked unsigned identifiers and lengths, UTF-8 JSON/BIN padding, empty-BIN behavior, preserved unknown chunks, and output to seekable or non-seekable streams.
- [DONE] T014: Implement `GltfContainerData` reading and writing independent of document parsing so callers can inspect/repackage raw container content.
- [DONE] T015: Implement bounded buffering for non-seekable streams and explicit caller-stream ownership behavior for every read/write stream path.
- [DONE] T016: Implement UTF-8 JSON token reading with RFC 8259 conformance, duplicate-member policy, depth/token/string/number limits, finite-number checks, and byte/JSON-pointer source locations.
- [DONE] T017: Implement schema-property readers for every glTF 2.0.1 object, including required members, exact scalar presence, defaults, enums, arrays, dictionaries, and unknown-member capture.
- [DONE] T018: Implement deep ownership/cloning of `JsonNode`/JSON values so parsed extension, extras, and unknown JSON survives source document disposal and independent document cloning.
- [DONE] T019: Implement core object JSON writing with fixed schema-member order, optional-default omission policy, extension/extras/unknown-member preservation, invariant strings/numbers, and no dictionary-order dependence.
- [DONE] T020: Implement deterministic free-form JSON writing with ordinal object-key sorting, stable array order, RFC 8785-compatible escaping/number treatment where applicable, and a documented glTF negative-zero policy.
- [DONE] T021: Implement compact and indented JSON modes without altering semantic ordering, and return owned UTF-8 bytes plus optional text without encoding round-trip differences.
- [DONE] T022: Implement the `IGltfReader` file, stream, byte-array, and raw-container methods by composing detection, container parsing, JSON parsing, model construction, optional resolution, and optional validation.
- [DONE] T023: Implement the `IGltfWriter` file, stream, byte-array, and JSON-text methods by composing validation, resource planning, deterministic JSON, and container output.
- [DONE] T024: Add writer overflow/finiteness/reference preflight so invalid output fails before modifying a target stream or path.

### URI, resource, buffer, and image-reference layer

- [DONE] T025: Implement RFC 3986 URI-reference classification and base resolution, preserving query/fragment components and preventing double percent-decoding.
- [DONE] T026: Implement strict percent encode/decode routines for glTF URI text, UTF-8 path segments, reserved characters, malformed escapes, and deterministic relative URI output.
- [DONE] T027: Implement RFC 2397 data-URI parsing for omitted/default media types, parameters, percent payloads, Base64 marker handling, and declared versus detected MIME information.
- [DONE] T028: Implement RFC 4648 Base64 decode/encode using spans or built-in primitives with alphabet, padding, whitespace, overflow, decoded-size, and canonical-output checks.
- [DONE] T029: Implement file/relative/data resource resolution with configured roots, traversal and absolute-path policy, no implicit network access, maximum resource size, and deterministic cache keys.
- [DONE] T030: Implement resolver dispatch and resource caching keyed by effective URI/content identity while keeping request context, source URI, declared length/MIME, and resource kind.
- [DONE] T031: Implement eager and lazy buffer resolution, GLB BIN binding, external/data buffer loading, declared-length enforcement, extra GLB padding handling, and independently owned bytes.
- [DONE] T032: Implement image resolution from URI, data URI, or bufferView, enforce source exclusivity/MIME rules, and retain original reference plus effective resource provenance.
- [DONE] T033: Implement built-in PNG and JPEG header inspection with signature, dimensions, supported bit/component metadata, truncation and MIME mismatch detection without decoding pixels.
- [DONE] T034: Implement header identification for KTX2, WebP, AVIF, DDS, and ASTC sufficiently to report MIME/format/dimensions when available and route full work to an image codec hook.
- [DONE] T035: Implement stable resource-name allocation, extension selection, collision suffixing, relative-path normalization, and optional content-hash naming.
- [DONE] T036: Implement resource copying, embedding, extraction, hashing, byte deduplication, and manifest generation without mutating source resources until a plan succeeds.
- [DONE] T037: Implement bufferView image extraction and packing with correct alignment, byte lengths, MIME updates, and safe coexistence with accessor data.
- [DONE] T038: Implement `GltfImageCodec` header inspection and passthrough operations, plus clear unavailable results for pixel decode/encode/transcode operations requiring a registered external codec.
- [DONE] T039: Apply read limits consistently to primary container bytes, JSON, Base64 expansion, resource count/bytes, buffers, images, and cache totals.

### Binary accessors and buffer packing

- [DONE] T040: Implement component-size/count and accessor element-layout calculation for all scalar/vector/matrix types with checked multiplication/addition.
- [DONE] T041: Implement matrix column padding for MAT2/MAT3 8-bit and 16-bit storage and validate element alignment independently from logical component count.
- [DONE] T042: Implement bufferView/accessor byte-range calculation for tight and interleaved data, byte offsets, byte stride, final element extent, targets, and zero-count edge cases.
- [DONE] T043: Implement span-based little-endian decode for all six component types into float, signed, unsigned, index, matrix, and raw result forms.
- [DONE] T044: Implement exact glTF normalized integer decoding and encoding, including signed minimum clamping, deterministic rounding, and range errors.
- [DONE] T045: Implement sparse accessor validation and overlay reads with zero-initialized bases, unsigned sparse indices, strict monotonicity, bounds, and non-interleaved sparse payload rules.
- [DONE] T046: Implement sparse-to-dense materialization and dense-to-sparse generation with configurable zero tolerance, correct new bufferViews, and deterministic index ordering.
- [DONE] T047: Implement accessor min/max calculation and verification for scalar/vector position data while handling normalized values, sparse overlays, empty/invalid values, and finite-number requirements.
- [DONE] T048: Implement typed accessor creation from float, signed, unsigned, index, matrix, or raw inputs, selecting legal component/type combinations and creating associated views/buffers.
- [DONE] T049: Implement a deterministic buffer packer for alignment, tight/interleaved layout, target grouping, padding, buffer size limits, and optional partitioning.
- [DONE] T050: Implement safe buffer consolidation and repartitioning with complete accessor, sparse, image, extension, and bufferView remapping.
- [DONE] T051: Implement every `IGltfAccessorCodec` method and return precise status/diagnostics for unresolved data, invalid layout, incompatible requested form, and missing compression codec.

### Core semantics and model services

- [DONE] T052: Implement attribute semantic parsing and formatting for core semantics, numbered sets, underscore-prefixed custom attributes, and extension-added semantics.
- [DONE] T053: Implement primitive topology count/index checks, index bounds/restart restrictions, equal attribute counts, material references, and morph-target shape/count checks.
- [DONE] T054: Implement PBR metallic-roughness defaults, texture slot/channel/color-space classification, alpha behavior, normal/occlusion parameters, and default material evaluation.
- [DONE] T055: Implement sampler default/effective values and legal filter/wrap validation without introducing renderer-specific behavior.
- [DONE] T056: Implement perspective/orthographic camera parameter handling and projection matrix calculation including infinite far perspective.
- [DONE] T057: Implement scene hierarchy traversal, parent/reverse-reference maps, cycle detection, multi-scene roots, shared roots, and deterministic traversal order without LINQ.
- [DONE] T058: Implement local/world TRS and matrix composition using `System.Numerics` where suitable, quaternion normalization, normal matrices, and non-decomposable/negative determinant reporting.
- [DONE] T059: Implement right-handed axis/unit conversion primitives including geometry, normals/tangents, node transforms, cameras, winding, inverse bind matrices, and animation value conversion.
- [DONE] T060: Implement skin joint/common-root/same-scene analysis, inverse bind defaults/reads, skin palette calculation, and joint/weight influence validation helpers.
- [DONE] T061: Implement animation input/output layout analysis, strictly increasing key times, target shape/count rules, cubic-spline triplets, morph weight packing, and quaternion validity checks.
- [DONE] T062: Implement `GltfAnimationEvaluator` STEP/LINEAR/CUBICSPLINE sampling, quaternion shortest-path interpolation, clamp/loop time mapping, pose creation, pose application to a clone, and clip duration.
- [DONE] T063: Implement `GltfSceneEvaluator` local/world pose evaluation, camera projection, skin matrices, and scene/mesh/primitive bounds with indexed and sparse position data.
- [DONE] T064: Implement `GltfInspector` counts, dependency enumeration, reverse references, bounds, primitive/vertex/triangle estimates, draw-call estimates, animation statistics, resource bytes, and extension use without mutating the document.

### Schema and semantic validation

- [DONE] T065: Copy the normative glTF 2.0 draft-04 schemas into build inputs and implement internal schema loading without runtime network access.
- [DONE] T066: Implement the required JSON Schema draft-04 subset used by glTF and registered extension schemas: references, composition, types, required, dependencies, enums/constants, numeric/string/array/object constraints, and additional properties.
- [DONE] T067: Implement structural validation over preserved JSON/model presence so omitted versus explicit-default values and type/range failures produce exact RFC 6901 pointers.
- [DONE] T068: Implement root/object/reference validation for asset versions, default scene, list/index bounds, names where constrained, extension declarations, unknown required extensions, and graph cycles.
- [DONE] T069: Implement buffer/bufferView/accessor validation for resolved/unresolved resources, targets, alignment, ranges, strides, overlaps, matrix padding, sparse layout, min/max, finite values, and normalized encoding.
- [DONE] T070: Implement mesh/primitive validation for semantic formats, topology counts, indices, positions, normals, tangents, colors, joints/weights, morph targets, and extension-altered formats.
- [DONE] T071: Implement material/texture/sampler/image validation for factors, alpha, texture coordinates, slot color spaces, filters/wraps, URI/bufferView rules, MIME/signature agreement, and known unsupported image features.
- [DONE] T072: Implement scene/node/camera validation for transform exclusivity, TRS/matrix validity, hierarchy constraints, camera ranges, and coordinate-related invariants.
- [DONE] T073: Implement skin validation for joint uniqueness/bounds, common roots, scene availability, inverse bind matrices, skinned-node recommendations, and vertex influences.
- [DONE] T074: Implement animation validation for sampler/channel references, target eligibility, time/value accessors, monotonicity, interpolation, normalized rotations, cubic splines, weights, and pointer targets.
- [DONE] T075: Implement configurable resource validation, distinguishing missing optional/unresolved resources, resolver policy denial, malformed data URIs, short buffers, invalid images, and unsupported codecs.
- [DONE] T076: Implement issue aggregation with stable codes, severity overrides/suppression, fail-fast versus collecting modes, issue caps, related locations, and deterministic ordering.
- [DONE] T077: Implement document statistics collection within validation and map every applicable category in `specs/testing-references/glTF-Validator-ISSUES.md` to an implemented rule or an explicitly tracked extension rule.
- [DONE] T078: Implement all `IGltfValidator` entry points, including raw container validation before model construction and strict-mode conversion to `GltfValidationException` only when requested.

### Extension framework and registered extensions

- [DONE] T079: Implement registry descriptors from the downloaded extension snapshot, including status, known object locations, dependencies/conflicts, fallback requirements, schema resources, typed-handler availability, and codec needs.
- [DONE] T080: Implement generic extension/extras/unknown-property preservation, encountered-extension discovery, used/required reconciliation, naming checks, and deterministic emission.
- [DONE] T081: Implement extension-handler dispatch for typed read/write, semantic validation, resource/reference enumeration, and index remapping, with safe fallback for handlers unknown to the core.
- [DONE] T082: Implement typed `KHR_texture_transform`, `KHR_lights_punctual`, `KHR_node_visibility`, `KHR_node_hoverability`, `KHR_node_selectability`, and `EXT_mesh_gpu_instancing` parsing/writing/validation/evaluation metadata.
- [DONE] T083: Implement typed parsing/writing/validation for ratified material extensions: anisotropy, clearcoat, dispersion, emissive strength, IOR, iridescence, sheen, specular, transmission, unlit, variants, and volume.
- [DONE] T084: Implement typed release-candidate `KHR_materials_diffuse_transmission` handling with its snapshot version/status isolated so future registry revisions do not silently change semantics.
- [DONE] T085: Implement `KHR_animation_pointer` JSON Pointer parsing, animatable-target type/shape resolution, dependency/reference tracking, sampling integration, and validation.
- [DONE] T086: Implement `KHR_mesh_quantization` expanded attribute-format validation, dequantization/quantization metadata behavior, bounds rules, and extension used/required management.
- [DONE] T087: Implement `KHR_draco_mesh_compression`, release-candidate `KHR_meshopt_compression`, and ratified `EXT_meshopt_compression` metadata, fallback and required-extension rules, bufferView/reference remapping, conflicts, and dispatch through `IGltfCompressionCodec` without embedding a codec.
- [DONE] T088: Implement `KHR_texture_basisu` and `EXT_texture_webp` typed source selection, core fallback rules, MIME/resource validation, and dispatch through `IGltfImageCodec` without embedding native codecs.
- [DONE] T089: Implement `KHR_xmp_json_ld` packet/assignment/reference handling and JSON-LD payload preservation without attempting network context resolution.
- [DONE] T090: Implement snapshot-versioned typed metadata support for `KHR_gaussian_splatting` and entry-point preservation/validation for `KHR_interactivity`, with executable runtime behavior limited to registered caller hooks.
- [DONE] T091: Load and apply available schemas for every listed multi-vendor, vendor, archived, and additional registry extension while preserving unsupported semantics and references losslessly.
- [DONE] T092: Implement archived `KHR_materials_pbrSpecularGlossiness`, `KHR_techniques_webgl`, and `KHR_xmp` read/write preservation, warnings, and no-default-emission policy.
- [DONE] T093: Implement required-extension capability checks that distinguish metadata support, semantic support, resource decode support, and transform-remap support before read, evaluation, conversion, or mutation.

### Editing, transforms, and optimization hooks

- [DONE] T094: Implement deep document cloning with independent core lists, property JSON, extension models, resource bytes, unknown GLB chunks, and preserved scalar presence.
- [DONE] T095: Implement document merge with deterministic list/resource append order and complete remapping of core and registered-extension references.
- [DONE] T096: Implement reference traversal, reverse dependencies, reusable `GltfIndexRemap`, compaction, and removal safety for every root object collection.
- [DONE] T097: Implement transactional edit/transform working copies so failed validation, missing hooks, or internal errors leave the original document and output resources unchanged.
- [DONE] T098: Implement prune of unreachable objects/resources from selected scenes with options for animation/skin/extension/extras retention and an exact removal report.
- [DONE] T099: Implement deterministic value-based deduplication for buffers, bufferViews, accessors, images, textures, samplers, materials, meshes, and extension payloads without `IEquatable` or LINQ.
- [DONE] T100: Implement canonical object ordering/reindexing based on dependency-safe stable keys while updating all core and registered-extension references.
- [DONE] T101: Implement exact weld and unweld transforms for compatible primitives, preserving all attributes, morph targets, indices, topology, and material boundaries.
- [DONE] T102: Implement compatible primitive joining/splitting and repeated-mesh GPU instancing creation with explicit exclusions for skins, animation, morph state, unknown extensions, and differing materials/layouts.
- [DONE] T103: Implement node-transform application, selective transform baking, hierarchy flattening, center/ground, winding flip, unit conversion, and axis conversion with animation/skin safety checks.
- [DONE] T104: Implement position min/max and bounds regeneration plus area-weighted normal generation for supported triangle modes, including degenerate geometry diagnostics.
- [DONE] T105: Implement lossless animation resampling cleanup and redundant key removal for STEP/LINEAR/CUBICSPLINE channels under configured tolerances.
- [DONE] T106: Implement core numeric quantize/dequantize transforms, legal component selection, error measurement, normalized encoding, and `KHR_mesh_quantization` declaration updates.
- [DONE] T107: Implement dense/sparse optimization, buffer consolidation/partition, resource embed/extract, and deterministic packing as transform steps.
- [DONE] T108: Implement `IGltfTransformHook` dispatch for tangent/UV generation, reorder, simplification/LOD, Draco/Meshopt, image conversion, palette, and custom steps, including capability negotiation and unavailable status.
- [DONE] T109: Implement `GltfTransformer` ordered plan execution, validate-before/after, clone/in-place choice, unknown-extension safety gate, per-step reports, rollback, and final hashes/size deltas.
- [DONE] T110: Implement transform-report aggregation with changed object/resource identifiers, skipped reasons, issues, extension changes, codec attribution, and deterministic ordering.

### Conversion and deterministic output workflows

- [DONE] T111: Implement a resource-layout planner for preserve/external/data-URI/bufferView/GLB-BIN modes, detecting impossible combinations before producing bytes.
- [DONE] T112: Implement `.gltf` to GLB conversion with resolution, single-buffer consolidation, alignment, optional image embedding, URI removal/retention policy, and extension fallback checks.
- [DONE] T113: Implement GLB to `.gltf` conversion with stable BIN/image extraction or data URIs, MIME-aware names, buffer partition choices, and correct rewritten references.
- [DONE] T114: Implement same-container normalization/repack paths and in-memory `ConvertDocument` without unnecessary decode/re-encode of opaque image or compression bytes.
- [DONE] T115: Implement atomic file conversion/output with explicit temporary sibling paths, successful final replacement, cleanup behavior compatible with allowed source syntax, and resource collision policy.
- [DONE] T116: Implement conversion manifests with effective URIs, resource kinds, MIME types, byte lengths, content hashes, embedded/external status, diagnostics, and codec/extension decisions.
- [DONE] T117: Implement every `IGltfConverter` entry point by composing reader, resolver, validator, optional transforms, resource planner, writer, and transactional output.
- [DONE] T118: Verify repeated deterministic writes/conversions of the same owned document produce byte-identical primary outputs, resource bytes, names, manifests, diagnostics ordering, and hashes across cultures.

### Complete `src/Api` functional test coverage

- [DONE] T119: Create test fixture builders, temporary-resource workspaces, hostile stream implementations, deterministic byte comparers, resolver/codec/extension/transform test doubles, and Khronos asset-corpus adapters under `tests/`.
- [DONE] T120: Test every public data model's documented defaults, optional scalar presence, mutable field ownership, extension/extras/unknown JSON retention, and independence after deep clone and merge.
- [DONE] T121: Test every public enum/static constant against normative numeric/string values and exercise each custom exception through the public failure path that promises it.
- [DONE] T122: Test all `IGltfReader`/`GltfReader` file, stream, bytes, and container methods for JSON glTF, BOM/UTF-8 cases, malformed JSON, limits, caller stream ownership, resolution modes, and collecting/strict validation.
- [DONE] T123: Test all GLB reader/container paths with valid empty/BIN assets and exhaustive header/chunk order, length, padding, truncation, duplicate, unknown-chunk, trailing-byte, and non-seekable-stream cases.
- [DONE] T124: Test all `IGltfWriter`/`GltfWriter` methods for compact/indented JSON, omitted/default fields, scalar presence, unknown JSON, stream ownership, GLB padding/lengths/chunks, preflight failures, and byte-identical deterministic output.
- [DONE] T125: Test URI classification/resolution, percent escapes, queries/fragments, file roots/traversal denial, custom resolver dispatch/cache, RFC 2397 percent data, RFC 4648 Base64 edge cases, size limits, and canonical data-URI output.
- [DONE] T126: Test every `IGltfAccessorCodec` read/create method across all component and accessor types, normalized extrema/rounding, tight/interleaved layouts, offsets/strides, matrix padding, alignment, min/max, and checked overflow.
- [DONE] T127: Test sparse accessors with absent/present bases, every legal index component, overlay boundaries, invalid ordering/duplicates/out-of-range indices, materialization, sparse generation, and binary round trips.
- [DONE] T128: Test complete core-model JSON and GLB round trips with every root/object field, all primitive modes/semantics, morph targets, PBR/texture slots, image source forms, cameras, skins, and animations.
- [DONE] T129: Test draft-04 schema validation against every normative core schema rule with exact stable issue code/severity/JSON pointer and deterministic issue ordering.
- [DONE] T130: Test semantic validation of buffers, views, accessors, resolved lengths, overlaps, targets, alignment, finite/min/max data, normalized ranges, sparse data, and compressed/unresolved states.
- [DONE] T131: Test mesh validation for topology, indices, standard/custom attributes, quantized formats, normals/tangents/colors, joint/weight sets, morph targets, material references, and default material behavior.
- [DONE] T132: Test material, texture, sampler, image, and camera validation/effective behavior, including PBR defaults/channels/color spaces, alpha, MIME/header mismatch, core and extension image fallbacks, and finite projection matrices.
- [DONE] T133: Test scene, transform, coordinate, hierarchy, bounds, skin, inverse-bind, joint influence, and animation validation with valid and invalid cross-object combinations.
- [DONE] T134: Test every `IGltfExtensionHandler` operation and every typed ratified/release-candidate extension for parse/write round trip, schema/semantic validation, used/required declarations, fallback, resources, reference remap, and snapshot status.
- [DONE] T135: Test generic multi-vendor/vendor/draft/archived/future extension and extras preservation, schema application where present, unknown required errors, optional warnings, deterministic keys, and refusal of unsafe remapping transforms.
- [DONE] T136: Test compression and image codec integration with provided test codecs plus missing/failed codec paths for Draco, Meshopt, BasisU/KTX2, WebP, opaque passthrough, and transactional non-mutation.
- [DONE] T137: Test all `IGltfSceneEvaluator` operations for TRS/matrix world transforms, shared scene roots, negative scales, camera projections, indexed/sparse bounds, morph policy, skin palettes, and invalid graph handling.
- [DONE] T138: Test all `IGltfAnimationEvaluator` operations for exact/between/outside keys, clamp/loop, STEP/LINEAR/CUBICSPLINE, vector/scalar/quaternion/weights/pointer targets, pose isolation/application, and durations.
- [DONE] T139: Test every `IGltfDocumentEditor` operation for empty creation, deep clone, multi-document merge, all-index remapping, resource collisions, compaction, unknown-extension safety, and original-document independence.
- [DONE] T140: Test every `IGltfInspector` method for dependency/reverse-reference results and exact document statistics across scenes, meshes, animation, buffers/images, sparse/interleaved accessors, unresolved resources, and extensions.
- [DONE] T141: Test all built-in lossless transform steps individually and in ordered plans: prune, dedup, canonicalize, weld/unweld, animation cleanup, dense/sparse, packing, consolidation/partition, and resource embed/extract.
- [DONE] T142: Test geometry/scene transforms and hooks for bake/flatten/center/ground, units/axes/winding, normals, join/split/instancing, quantization error bounds, hook capability/failure, reports, validate-before/after, and rollback.
- [DONE] T143: Test every `IGltfConverter` method and all container/resource-mode combinations for glTF↔GLB and same-format repacks, stable naming, manifests/hashes, missing resources, extension/codec decisions, collisions, and atomic-output failure behavior.
- [DONE] T144: Add adversarial tests for deep/huge JSON, integer arithmetic overflow, malicious URI traversal, Base64 expansion, oversized GLB/resource declarations, cyclic graphs, invalid UTF-8, non-finite accessor values, and issue-count limits.
- [DONE] T145: Run core reader/writer/validator/converter tests over Khronos Sample Assets and glTF Asset Generator cases, pin fixture revisions, and assert expected success/failure categories without adding production dependencies.
- [DONE] T146: Add differential tests under `tests/` using permitted external libraries/tools such as SharpGLTF and the Khronos Validator to compare core round trips, decoded accessor values, bounds, and validation categories while retaining normative fixtures as the final authority.
- [DONE] T147: Add culture, platform path, stream chunking, repeated-run, and parallel independent-instance tests proving deterministic results and absence of shared mutable public static state.
- [DONE] T148: Exercise every public API method's success, expected-frequent failure result, strict exception, invalid argument, missing resource, missing codec, and unsupported-extension behavior; use coverage reports to close all reachable `src/Api` and public-operation branches.
