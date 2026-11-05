# Universal SVG library development plan

Status: proposed plan, 2026-08-17. Task state uses `[OPEN]`, `[IN PROGRESS]`,
`[BLOCKED]`, and `[DONE]`. Task identifiers are stable and should not be reused.

## Product definition

Build a managed .NET 10 library that can securely load SVG/SVGZ, preserve and edit
the source document, resolve it into a deterministic graphics model, perform
geometry/style/document processing, generate new SVG, serialize canonical or
compact output, evaluate declarative animation at a caller-selected time, and
render the static result to a portable RGBA buffer or PNG. “Universal” means the
same core works on every .NET 10 platform without native graphics dependencies;
it does not mean executing arbitrary script or silently fetching the network.

The semantic target is the static and declarative-animation portions of SVG 2,
with SVG 1.1 compatibility and graceful preservation of unknown/vendor content.
Three representations remain deliberately separate:

1. A lossless editable XML/SVG document retains namespaces, unknown content, and
   source-order information.
2. A computed document contains parsed values, cascaded styles, resolved
   references, viewports, text layout, and diagnostics without destroying source
   authoring data.
3. A backend-neutral scene contains normalized paths, paints, images, glyphs,
   clips, masks, filters, and compositing commands for rendering or export.

### Hard constraints

- `src/` uses only the .NET base class libraries; no NuGet, native, platform UI,
  or graphics dependency is permitted there.
- All implementation syntax must obey `syntax.md`. In particular, production
  code uses classes/enums/interfaces/public fields/ordinary methods rather than
  properties, structs, records, class inheritance outside direct exceptions and
  interface implementation, generic declarations, `ref`/`out`, `try`/`catch`,
  async, iterators, pattern syntax, or LINQ.
- Public APIs use small interfaces for reusable host services and .NET
  `Func<...>`/`Action<...>` for short-lived predicates, notifications, and
  adapters. Options and results are classes with documented public fields. No
  operation requires subclassing.
- Span-based overloads and internal span parsing should be used for numbers,
  colors, path data, transforms, points, CSS tokens, Base64, and output formatting
  when they reduce copying and remain legal under `syntax.md`.
- External packages are allowed only in `tests/`, for test runners, browser or
  reference-renderer comparison, image comparison, and fixture tooling.
- Do not add syntax-policy/source-scanning tests. Do not add benchmarks or
  performance test projects. Performance is addressed through design reviews,
  bounded algorithms, and ordinary functional tests for large inputs.

### Explicit boundaries

- Script elements, event-handler attributes, and script URLs can be preserved,
  queried, emitted, or removed by sanitization, but production code never
  executes JavaScript or another scripting language.
- External file/network access is disabled by default and always mediated by
  explicit load policy plus a caller resource provider. Parsing never performs implicit
  I/O.
- PNG and nested SVG are portable built-in image codecs. JPEG, GIF, WebP, AVIF,
  and platform-specific formats are represented and can be decoded through a
  caller callback; portable decoders may be added later without changing the API.
- The library owns SVG/CSS layout, OpenType outline access, and a deterministic
  baseline shaper. Advanced script shaping and additional font containers can be
  supplied by callbacks until their managed implementations are complete.
- Browser navigation, focus UI, accessibility platform bridges, audio/video
  playback, HTML layout inside `foreignObject`, and a live browser event loop are
  host responsibilities. The library preserves those nodes and exposes geometry,
  semantics, and dispatch data.

## Research conclusions

| Project | Relevant surface | Design consequence here |
| --- | --- | --- |
| [Apache Batik](https://xmlgraphics.apache.org/batik/using/) | Separate microsyntax parsers, SVG DOM, graphics tree, renderer, generator, transcoder, viewer, and scripting modules; detailed support matrix | Keep parsing, editable DOM, computed tree, scene building, rasterization, generation, and host integration independently testable. |
| [resvg/usvg](https://github.com/linebender/resvg) | Secure static renderer; preprocessing simplifies SVG into a small render tree; strong recursion/edge-case tests and deterministic output | Resolve authoring complexity once into a normalized scene, enforce budgets/cycle checks, and make rendering independent of XML/CSS. |
| [librsvg](https://gnome.pages.gitlab.gnome.org/librsvg/devel-docs/features.html) | Broad SVG 1.1/2 static coverage, explicit external-reference policy, no animation/script/DOM API | Treat resource policy and processing mode as first-class API, while retaining an editable DOM rather than hiding it. |
| [SVG.NET](https://github.com/svg-net/SVG) | .NET object model with read/write/render support and broad element classes | Offer convenient typed value access and document construction, but avoid a subclass-per-element hierarchy because `syntax.md` forbids it. |
| [Svg.Skia](https://github.com/wieslawsoltes/Svg.Skia) | SVG 1.1 plus static SVG 2, CSS, resources, filters, text paths, host-driven animation/interaction | Include modern href precedence, context paint, CSS geometry, `paint-order`, `fr`, `mask-type`, `feDropShadow`, hit testing, and a host-driven clock. |
| [SharpVectors](https://github.com/ElinamLLC/SharpVectors) | SVG DOM, WPF/GDI conversion, viewing, optimized output, conversion customization | Expose backend-neutral scene/export commands and callbacks instead of coupling the core to a UI framework. |
| [CairoSVG](https://cairosvg.org/svg_support/) | SVG-to-raster/vector transcoding with documented support boundaries | Publish a machine-readable capability report and diagnostics rather than silently ignoring unsupported semantics. |
| [SVGO](https://svgo.dev/docs/plugins/) and [svgcleaner](https://github.com/RazrFalcon/svgcleaner) | Ordered optimization passes; removal of metadata/defaults/hidden nodes; path, transform, style, ID, and namespace cleanup; lossless-by-default concerns | Make processing an explicit ordered job list, distinguish safe and lossy profiles, retain `viewBox`/accessibility by default, and report every semantic rewrite. |

Local snapshots and authoritative standards are indexed in
[`specs/SOURCES.md`](specs/SOURCES.md). Research is for feature discovery only;
no external implementation becomes a production dependency.

## Architecture and data flow

`bytes/text -> secure container/XML reader -> lossless SvgDocument -> CSS and value parsing -> computed SvgResolvedDocument -> normalized SvgScene -> processors / hit testing / animation sampling / software renderer -> SVG, RGBA, or PNG`

Every stage returns diagnostics with source location and severity. Strict mode
fails on the first conformance error; compatible mode applies specified recovery;
preserve mode retains unsupported data. Caches are document-owned and invalidated
by edit version numbers. Algorithms that follow references or descendants are
iterative and budgeted so hostile documents cannot cause unbounded recursion,
allocation, work, decompression, or resource expansion.

## Complete feature inventory

### Loading, containers, and security

- Load from `string`, `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, `byte[]`,
  `Stream`, `TextReader`, `XmlReader`, filesystem path, and caller-resolved URI;
  callers retain ownership of supplied streams/readers.
- Detect UTF-8/UTF-16 BOM and XML declarations through built-in XML facilities;
  reject unsupported encodings with a located diagnostic.
- Detect plain SVG/XML, gzip SVGZ, and gzip magic independent of extension; emit
  SVGZ with deterministic compression settings.
- Support full documents and SVG fragments; select a fragment by ID or `view`
  target; honor base URI and media type metadata.
- Secure defaults: prohibit DTD and external entities, no implicit network/file
  access, bounded bytes/chars/nodes/attributes/depth/path segments/CSS tokens,
  bounded gzip expansion ratio, bounded data URLs, resource count/size/time,
  reference depth, filter area, image dimensions, and animation instances.
- Processing modes matching SVG concepts: dynamic metadata preservation, static,
  and secure-static; script execution is always absent.
- Resource policies for same-document, data, relative file, absolute file, HTTP,
  HTTPS, cross-origin, redirect, MIME allow-list, and custom schemes; cancellation
  is a synchronous caller token checked at bounded intervals.
- Resource cache, normalized URI keys, `xml:base`, URI/IRI escaping, fragment
  resolution, `href` over `xlink:href` precedence, cycle detection, and diagnostic
  provenance for nested resources.

### XML and editable document model

- XML 1.0 names, namespaces, namespace declarations, qualified attributes,
  escaped text, character references, CDATA, comments, processing instructions,
  whitespace, XML declaration, doctype preservation without entity expansion,
  `xml:base`, `xml:lang`, and `xml:space`.
- Tagged `SvgNode` model for document, element, text, CDATA, comment, processing
  instruction, and doctype nodes; parent/children, ordered attributes, namespace
  URI/prefix/local name, source spans, dirty/version state, and optional raw token
  retention.
- Preserve unknown namespaces, elements, attributes, CSS declarations, malformed
  but recoverable values, vendor metadata, and SVG 1.2/editor extensions.
- DOM operations: create, append, insert, replace, remove, move, clone, import,
  adopt, normalize adjacent text, attribute CRUD, namespace lookup, ancestry,
  descendants, siblings, document order, text content, and deep equality.
- Indexed lookup by ID, class, element name, namespace, and source span; index
  invalidation after edits; duplicate-ID diagnostics.
- Query selector/query selector all using the shared selector engine; predicate
  and traversal callbacks use built-in `Func`/`Action` types.
- Edit transaction/change set, undoable patch representation, source map, and
  deterministic diff/patch with precondition diagnostics.

### Low-level SVG/CSS value system

- Culture-invariant finite number/integer parsing, exponent notation, separators,
  clamping, error offsets, negative-zero normalization, and shortest round-trip
  formatting.
- Lengths and percentages with `px`, `em`, `ex`, `ch`, `rem`, `vw`, `vh`, `vmin`,
  `vmax`, `cm`, `mm`, `Q`, `in`, `pt`, `pc`; viewport/font/object-bbox contexts;
  unitless SVG compatibility and calc trees.
- Angles (`deg`, `grad`, `rad`, `turn`), times (`s`, `ms`), frequencies,
  resolutions, ratios, integer/number/length/angle/time lists, and keyword-or-value
  grammar helpers.
- CSS-wide values, `var()` custom-property substitution with fallback/cycle
  handling, `calc()`, `min()`, `max()`, `clamp()`, and math dimensional checks.
- Colors: named colors, transparent, currentColor, hex forms, rgb/rgba,
  hsl/hsla, hwb, lab/lch, oklab/oklch, `color(srgb ...)`, device-independent
  conversion, alpha, interpolation space, gamut clipping, and serialization.
- Paint values: none, solid color, context-fill/context-stroke, URL paint server,
  multiple URL fallbacks, and fallback color/none.
- Transform lists and CSS transforms: matrix, translate, scale, rotate/skew,
  transform origin/box, composition, inversion, decomposition, interpolation, and
  SVG-versus-CSS grammar distinctions.
- View boxes, rectangles, preserveAspectRatio including defer/meet/slice/all
  alignments, coordinates, points, pathLength, number pairs, optional pairs,
  dash arrays, URL/IRI/function values, language lists, feature strings, and media
  types.
- Path data M/m, Z/z, L/l, H/h, V/v, C/c, S/s, Q/q, T/t, A/a; repeated/implicit
  commands, arc flags, compact signs, malformed-input recovery, and CSS `d`.
- Timing values: offsets, wallclock preservation, syncbase/eventbase/repeat/access
  keys, indefinite, begin/end lists, key times, key splines, key points, repeat,
  restart, fill, additive, accumulate, and calc modes.

### SVG elements and attributes

- Structural/document: `svg`, `g`, `defs`, `symbol`, `use`, `switch`, `view`, `a`,
  `title`, `desc`, `metadata`, plus nested SVG viewports and conditional processing.
- Shapes: `path`, `rect` including rounded corners, `circle`, `ellipse`, `line`,
  `polyline`, and `polygon`, including SVG 2 geometry properties and `pathLength`.
- Text: `text`, `tspan`, `textPath`; x/y/dx/dy/rotate lists, textLength,
  lengthAdjust, whitespace, direction/writing mode, baseline/alignment, anchor,
  decorations, letter/word spacing, inline-size, and text-on-path method/spacing,
  side, startOffset, and SVG 2 inline `path`.
- Embedded content: `image` with intrinsic sizing/aspect ratio and nested SVG;
  `foreignObject` preserved and exposed to a host callback; SVG 2 `audio`, `video`,
  `iframe`, and `canvas` content preserved and represented as host content but not
  played or laid out by the core.
- Paint servers: `linearGradient`, `radialGradient` including SVG 2 `fr`, `stop`,
  `pattern`, units, transforms, spread methods, view boxes, href inheritance,
  stop color/opacity, and context paint.
- Reusable effects: `marker` including auto/angle/auto-start-reverse orientation,
  marker units/view box/ref point; `clipPath`; `mask` including alpha/luminance;
  nested units/transforms and object-bounding-box behavior.
- Filters: `filter`, `feBlend`, `feColorMatrix`, `feComponentTransfer`, `feFuncR/G/B/A`,
  `feComposite`, `feConvolveMatrix`, `feDiffuseLighting`, `feDisplacementMap`,
  `feDistantLight`, `feDropShadow`, `feFlood`, `feGaussianBlur`, `feImage`,
  `feMerge`, `feMergeNode`, `feMorphology`, `feOffset`, `fePointLight`,
  `feSpecularLighting`, `feSpotLight`, `feTile`, and `feTurbulence`; standard
  inputs, named results, primitive regions/units, color spaces, and chains.
- Declarative animation: `animate`, `set`, `animateMotion`, `mpath`, and
  `animateTransform`; timing, interpolation, motion rotation, paced/spline modes,
  additive/accumulate, repeat/fill/restart, host-triggered event times, and
  `discard` removal semantics at a caller-selected timeline time.
- Style/script: `style` with type/media/title and CSS; `script` and event-handler
  attributes losslessly preserved and sanitizer-visible but never executed.
- Core/global attributes: id, class, style, tabindex, lang/xml:lang, xml:space,
  xml:base, transform, data-*, aria-*, role, conditional processing, all
  presentation attributes, href/xlink attributes, and unknown attributes.
- SVG 1.1 compatibility/preserve support: `altGlyph`, `altGlyphDef`,
  `altGlyphItem`, `animateColor`, `color-profile`, `cursor`, `font`, `font-face`,
  `font-face-format`, `font-face-name`, `font-face-src`, `font-face-uri`, `glyph`,
  `glyphRef`, `hkern`, `missing-glyph`, `tref`, and `vkern`. Implement useful
  legacy font/cursor semantics where practical; otherwise round-trip and diagnose.
- Unknown/SVG Tiny/SVG 1.2/vendor elements such as `flowRoot`, mesh/hatch paint
  experiments, and editor namespaces round-trip without being mistaken for
  renderable SVG 2 content.

### CSS, cascade, and computed style

- CSS tokenization with escapes, comments, strings, bad tokens, Unicode ranges,
  URLs, functions, blocks, source offsets, and standards-compatible error recovery.
- Stylesheet/rule/declaration model retaining unknown syntax and importance;
  `style` attributes, presentation attributes, embedded style sheets, caller
  sheets, user-agent defaults, and caller-resolved `@import`.
- Selectors: universal/type/namespace, ID, class, attribute operators and flags,
  selector lists, descendant/child/adjacent/general sibling combinators,
  `:root`, `:scope`, `:empty`, first/last/only/nth child and type pseudos including
  `of` selectors, `:lang`, `:not`, `:is`, `:where`, `:has`, and host state for
  link/visited/hover/active/focus/target. Unsupported pseudo-elements and dynamic
  selectors remain parsed and non-matching with diagnostics.
- Cascade origins, encapsulation order where applicable, specificity, source
  order, `!important`, inheritance, initial/unset/revert/revert-layer, shorthand
  expansion, invalid-at-computed-value handling, and custom properties.
- `@namespace`, `@import`, `@layer`, `@media`, `@supports`, `@font-face`,
  `@keyframes`, and unknown at-rule preservation; screen/print and
  color/size/resolution/reduced-motion media evaluation from caller environment.
- Computed properties: display, visibility, opacity, overflow, color; fill/stroke
  family, fill/clip rules, paint-order, vector-effect; markers; stop/flood/lighting
  colors; color interpolation/rendering; clip/mask/filter; isolation/blending;
  shape/image/text rendering; transform/origin/box; font shorthand and longhands;
  text spacing/anchor/decoration/baselines/direction/writing/white-space;
  pointer-events and cursor. Preserve and, where SVG 1.1 requires it, compute
  legacy `clip`, `enable-background`, `color-profile`, `kerning`, and glyph
  orientation properties with deprecation diagnostics.
- Style dependency graph and incremental invalidation for class/id/attribute/tree,
  stylesheet, custom-property, viewport, font, media, and interaction-state edits.

### Geometry and path processing

- Double-precision point/vector/size/rectangle/matrix classes with finite-value
  validation and deterministic operations.
- Shape-to-path conversion, absolute/relative conversion, shorthand expansion,
  quadratic/cubic conversion, elliptical-arc center conversion, arc-to-cubic,
  subpath closure, reversal, concatenation, and transform baking.
- Tight fill bounds for line/Bezier/arc extrema; stroke/filter/marker bounds;
  transformed and world bounds; empty/degenerate geometry handling.
- Curve evaluation, derivative/tangent/normal, adaptive flattening, path length,
  point/tangent at length, subpath extraction, trimming, and pathLength scaling.
- Fill containment with nonzero/evenodd rules; stroke containment with caps,
  joins, miters, dashes, vector effects, and tolerance; topmost hit testing.
- Stroke outline expansion, dash placement, marker vertex/tangent/orientation
  calculation, and non-scaling-stroke handling.
- Segment intersections, self-intersection detection, winding classification,
  path boolean union/intersection/difference/xor, clipping, and outline cleanup.
- Simplification with visual tolerance, collinear/redundant command removal,
  precision rounding with error bounds, and canonical/minified path formatting.

### References, resolution, and conditional processing

- ID/fragment resolution, `<use>` shadow-instance expansion, symbol/svg viewport
  behavior, recursion protection, inherited context paint, and source-to-instance
  mapping.
- Gradient/pattern/filter/clip/mask/marker href inheritance with cycle detection,
  defaulting, units conversion, transforms, and fallback behavior.
- `requiredExtensions`, `requiredFeatures`, `systemLanguage`, switch selection,
  media/support queries, processing modes, and host capability/environment input.
- Nested SVG and raster image resolution, data URLs, content sniffing constrained
  by MIME policy, image intrinsic dimensions, preserveAspectRatio, and missing or
  blocked resource fallback.
- Deterministic diagnostic categories for syntax, conformance, unsupported,
  security, resource, layout, rendering, and lossy processing.

### Text and fonts

- Font database populated only from caller-supplied bytes/streams/URIs; generic
  families, family/style/weight/stretch matching, fallback, unicode-range, and
  deterministic ordering.
- SFNT/OpenType parsing for required metrics, cmap, TrueType glyph outlines,
  kerning, variation axes, CFF/CFF2 outlines, GSUB/GPOS/GDEF shaping tables, and
  COLR/CPAL where used; WOFF/WOFF2 and legacy SVG-font input staged independently.
- Unicode scalar decoding, grapheme clusters, script/language itemization,
  bidirectional reordering, mirroring, vertical orientation, line breaking, and
  white-space processing.
- Shaping pipeline with substitutions, positioning, ligatures, marks, kerning,
  advances, fallback spans, variation coordinates, and callback override for
  scripts or font formats not yet handled by the managed engine.
- SVG character positioning lists, chunk anchoring, baselines, writing modes,
  textLength/lengthAdjust, decorations, text on open/closed paths, pathLength
  scaling, side/reversal, and glyph-to-path conversion.
- Text logical/ink/stroke bounds, glyph hit testing, text extraction, selection
  ranges, accessible text order, and font/resource diagnostics.

### Scene, paint, compositing, filters, and rendering

- Immutable-by-convention tagged scene nodes for group/path/image/text plus shared
  paint servers/effects; absolute transforms and precomputed fill/stroke/filter
  bounds; source-node and `<use>` instance identity.
- Viewport/viewBox transforms, nested clipping and overflow, opacity groups,
  isolation, paint order, display/visibility, object-bounding-box coordinate
  systems, and offscreen layer planning.
- Solid/gradient/pattern paints, spread modes, premultiplied alpha, current/context
  paint, sRGB/linearRGB conversions, opacity, blend modes, Porter-Duff compositing,
  clipping, alpha/luminance masks, and filter region propagation.
- Deterministic software rasterizer: RGBA8 premultiplied surfaces, anti-aliased
  fill/stroke scan conversion, image sampling, gradient/pattern sampling, clip
  stacks, layers, bounded tiling, and color-space conversion.
- Complete filter graph evaluation for the listed primitives, including edge
  modes, kernels, turbulence noise, lighting, morphology, displacement, standard
  inputs, named results, primitive subregions, and safe area limits.
- PNG decoding/encoding with palette/grayscale/RGB/RGBA, transparency, filters,
  interlace, metadata needed for color/dimensions, CRC checking, and bounded zlib;
  data URL encode/decode; SVGZ encode/decode.
- Render whole documents, ID fragments, source nodes, and `<use>` instances at
  requested viewport/scale/background/time; compute drawing bounds; emit warnings
  for callback-required images/fonts/foreign content.
- Backend-neutral command enumeration so PDF/XAML/canvas adapters can be built in
  other assemblies without a production dependency.

### Animation, interaction, and accessibility

- Declarative SMIL timeline and CSS keyframe sampling at caller-provided time;
  begin/end instance lists, event injection, repeat, restart, fill, easing,
  interpolation by value type, additive/accumulate, motion paths, and dirty-node
  reporting. No background timer or event loop.
- Interaction state object for target, link, hover, active, focus, pointer, and
  reduced-motion state; geometry-aware pointer-events and cursor resolution;
  topmost/all hit results and ancestor route.
- Link target/fragment resolution and navigation request objects returned to the
  host; no navigation is performed by the library.
- Accessible name/description from title/desc/ARIA, role/state/property retention,
  hidden/presentation handling, logical reading order, focusability/tab order,
  and a platform-neutral accessibility tree.

### Validation, processing, optimization, and generation

- Well-formedness/conformance validation by SVG profile, element content model,
  attribute applicability, value grammar/range, namespaces, references, duplicate
  IDs, CSS, resources, accessibility, and unsupported/preserve-only features.
- Sanitizer profiles for untrusted static images and configurable policies that
  remove scripts/event attributes/script URLs, unsafe foreign content, external
  resources/imports/fonts, unsafe data media types, links, metadata, and unknown
  namespaces while preserving a change report.
- Ordered processor jobs with prerequisites and invalidation; safe default,
  aggressive/lossy opt-in, custom callback job, dry-run, change log, multipass
  convergence cap, and deterministic output.
- Safe jobs: remove comments/editor metadata/unused namespace declarations/default
  attributes/empty containers/invisible unreferenced nodes; clean attributes;
  normalize numbers/colors/paths/transforms; collapse groups; merge adjacent
  paths when semantics match; deduplicate paint servers; remove unused defs;
  inline safe use/style; prefix/minify IDs with full reference rewriting; preserve
  `viewBox`, title/desc/ARIA, and semantic IDs by default.
- Lossy jobs: configurable numeric/curve tolerance, shape-to-path, text-to-path,
  transform baking, image embedding, CSS flattening, crop/fit viewBox, palette or
  opacity simplification, metadata/accessibility removal, and raster fallback.
- Document operations: recolor/theme via currentColor/CSS variables, namespace or
  ID prefixing, merge/sprite/symbol construction, extract fragment, split symbols,
  flatten use/paint/effects, resize/fit/crop, coordinate conversion, and diff/patch.
- Builder APIs for documents/elements/attributes/styles/paths/paints/filters/text;
  fluent behavior is expressed by ordinary methods returning the same class.
- Canvas-like recorder for save/restore, transforms, paths, fill/stroke, clips,
  images, text, layers, and definitions, producing editable SVG rather than a
  platform drawing object.
- Serializer modes: lossless/source-preserving, standards-canonical, pretty,
  compact, and deterministic; namespace repair, escaping, quote/whitespace policy,
  XML declaration/doctype policy, CSS/path/number precision, stable attribute
  ordering, SVG versus SVGZ, stream/text/string/UTF-8 destinations, and data URL.
- Capability report identifies implemented, callback-required, preserve-only,
  disabled-by-policy, and unsupported features for the current options/document.

## Public API completion tasks

- [DONE] T212: Add a public, syntax-compliant standalone value parser/formatter service and result/error classes for finite numbers, integers, coordinates, points, optional pairs, and bounded number/integer/point lists, with string and ReadOnlySpan inputs, exact error offsets, recovery controls, shortest-round-trip output, canonical output, compact output, and caller Span destinations.
- [DONE] T213: Extend the public value service to expose parsing, evaluation, conversion, interpolation, and serialization for lengths and percentages, angles, times, frequencies, resolutions, ratios, pathLength, viewBox, preserveAspectRatio, SVG transforms, CSS transforms/origins/boxes, colors, paints, calc/min/max/clamp trees, and the SVG-specific list grammars that currently require internal parsers or a synthetic document attribute.
- [DONE] T214: Complete the host-content public layer by representing audio, video, iframe, and canvas alongside image and foreignObject, and add synchronous bounded callback request/result contracts that can supply intrinsic sizes, decoded raster pixels, foreign-content layout or raster output, and deterministic diagnostics through resolution, scene construction, and rendering while keeping every callback disabled by default.
- [DONE] T215: Complete the managed OpenType public layer for the planned variable and color-font profile by exposing axis metadata and instantiated metrics/outlines, evaluating TrueType and CFF2 variation data including metric deltas, exposing COLR/CPAL glyph paints and palettes where usable by SVG text rendering, and returning precise callback-required results for unsupported tables or operators.
- [DONE] T216: Expand public capability identifiers and document analysis from broad subsystem claims to every feature family in this inventory, including individual element, CSS property/at-rule/selector, filter primitive, animation value family, image/font format, processor job, and host-callback boundary, with status derived from active options, resource policy, callbacks, and actual document use.
- [DONE] T217: Add exhaustive behavioral tests for the public standalone value layer, covering every accepted grammar and unit, string/span parity, locale independence, boundary and non-finite input, exact error offsets, strict and compatible recovery, conversion contexts, interpolation, canonical/compact formatting, destination sizing, and parse-format-parse invariants.
- [DONE] T218: Audit and complete public loading, XML, editable-document, query, CSS, cascade, computed-style, resource, reference, conditional-processing, validation, and security tests so every exposed operation has success, recovery/failure, configured-limit, cancellation, ownership, source-provenance, mutation, and cache-invalidation coverage where applicable.
- [DONE] T219: Audit and complete public element, viewport, paint-server, effect, embedded/host-content, resolved-document, scene, render-command, and extraction tests, including every current SVG element, every preserve-only legacy/unknown category, href inheritance and cycles, callback present/absent/failure outcomes, and stable source/use-instance identity.
- [DONE] T220: Audit and complete public geometry, path, hit-testing, Unicode, bidi, line-breaking, font, shaping, text-layout, text-path, and glyph-geometry tests, including analytic edge cases, malformed bounded fonts, WOFF/WOFF2, variable-font axes and outlines, COLR/CPAL data, callback fallback, writing modes, clusters, selection, and accessible text order.
- [DONE] T221: Audit and complete public raster, image, PNG, paint sampling, compositing, clip, mask, filter, renderer, animation, interaction, navigation, and accessibility-tree tests with deterministic pixel fixtures for every paint/effect/filter primitive and fixed-time cases for every interpolation, repeat, event, motion, CSS animation, pointer-events, focus, and accessibility outcome.
- [DONE] T222: Audit and complete public sanitization, processing, generation, builder, canvas, sprite, serialization, diff/patch, feature-analysis, and capability-report tests, covering every job and output destination, safe and lossy reports, policy combinations, idempotence, determinism, namespace/reference repair, source preservation, and load-write-load semantic equality.
- [DONE] T223: Use the existing test coverage collector to identify and close untested functional branches under src/Api, and require each public method and each declared capability to have positive, negative, and limit/cancellation tests where those outcomes apply, without adding syntax-policy, source-scanning, benchmark, documentation, or release tests.
