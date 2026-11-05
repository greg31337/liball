# Standards and support matrix

This matrix covers every identifier returned by
`SvgCapabilities.GetFeatureIdentifiers()`. Its status column is the static
release-wide value returned by `SvgCapabilities.GetLibraryReport()` for this
build. The linked tests are representative entry points, not an exhaustive list
of all coverage.

## Reading the statuses

| Status | Meaning |
| --- | --- |
| `Implemented` | The portable managed library implements the declared profile, subject to the listed limits. |
| `CallbackRequired` | The library represents and routes the feature, but the host must explicitly supply the external capability. |
| `PreserveOnly` | Authored data round-trips when preservation is enabled, but is not computed or rendered as that feature. |
| `PolicyDisabled` | A feature available in principle is blocked by the options for a particular document. This is a dynamic status and is not used by the static library report. |
| `Unsupported` | The behavior is intentionally or currently unavailable. |

`SvgCapabilities.AnalyzeDocument()` is occurrence-based rather than
release-wide. Its report can therefore differ from this table: an external
resource can be `CallbackRequired`, `Implemented` after a host capability is
declared, or `PolicyDisabled` under the active resource policy. Script and legacy
occurrences can be `PreserveOnly`, while malformed or unavailable behavior is
`Unsupported`.

The primary profile is [SVG 2](../specs/svg2.html#intro-AboutSVG), with
[SVG 1.1 compatibility](../specs/svg11.html#intro-AboutSVG), an explicit
preserve-only legacy profile, and explicit host callbacks. “Implemented” does not
mean unlimited input or a live browser environment: every parser, resolver,
geometry operation, animation sample, and render is bounded by its option and
security-limit objects.

## Foundations, loading, and the editable document

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.foundation.source-mapping` | `Implemented` | [XML documents](../specs/xml-1.0.html#sec-documents), [SVG document structure](../specs/svg2.html#struct-NewDocument) | [SvgSourceMapTests](../tests/SvgSourceMapTests.cs), [SvgSourceSpanTests](../tests/SvgSourceSpanTests.cs) | UTF-16 source spans and nested-resource provenance are retained where source preservation is enabled; generated nodes have generated rather than authored spans. |
| `svg.feature.foundation.diagnostics` | `Implemented` | [SVG error processing](../specs/svg2.html#conform-ErrorProcessing) | [SvgDiagnosticTests](../tests/SvgDiagnosticTests.cs), [SvgExceptionTests](../tests/SvgExceptionTests.cs) | Diagnostics are bounded, deterministic snapshots. Compatible recovery is reported; unrecoverable API or I/O failures can still cross the direct exception boundary. |
| `svg.feature.foundation.security-limits` | `Implemented` | [SVG secure static mode](../specs/svg2.html#conform-secure-static-mode) | [SvgSecurityLimitsTests](../tests/SvgSecurityLimitsTests.cs), [SvgHostileInputTests](../tests/SvgHostileInputTests.cs) | Defaults bound input, decompression, trees, CSS, references, resources, geometry, fonts, filters, raster work, and timelines. Hosts should tighten them for smaller workloads. |
| `svg.feature.foundation.capabilities` | `Implemented` | [SVG feature strings and conditional processing](../specs/svg2.html#struct-ConditionalProcessing) | [SvgCapabilitiesTests](../tests/SvgCapabilitiesTests.cs), [SvgFeatureAnalysisTests](../tests/SvgFeatureAnalysisTests.cs) | Identifiers and release statuses are explicit and reflection-free. Per-document analysis reports only dynamically observed features and active policy/host state. |
| `svg.feature.loading` | `Implemented` | [SVG processing modes](../specs/svg2.html#conform-processing-modes), [XML well-formed documents](../specs/xml-1.0.html#sec-well-formed) | [SvgLoaderTests](../tests/SvgLoaderTests.cs), [SvgXmlLoaderTests](../tests/SvgXmlLoaderTests.cs), [SvgContainerReaderTests](../tests/SvgContainerReaderTests.cs) | Loads SVG/XML and SVGZ from supported .NET inputs. DTDs are prohibited by secure defaults; URI loading and nested resources never fetch implicitly. |
| `svg.feature.document.editable` | `Implemented` | [SVG document structure](../specs/svg2.html#struct-NewDocument), [XML namespaces](../specs/xml-namespaces-1.0.html#sec-namespaces) | [SvgDocumentModelTests](../tests/SvgDocumentModelTests.cs), [SvgDomCoverageTests](../tests/SvgDomCoverageTests.cs) | Namespace-aware tagged nodes preserve unknown, vendor, comment, CDATA, and processing-instruction content. This is an editable data model, not a live browser DOM. |
| `svg.feature.document.mutation` | `Implemented` | [SVG DOM elements](../specs/svg2.html#types-ElementsInTheSVGDOM), [qualified names](../specs/xml-namespaces-1.0.html#ns-qualnames) | [SvgMutationTests](../tests/SvgMutationTests.cs), [SvgChangeSetTests](../tests/SvgChangeSetTests.cs), [SvgPatchTests](../tests/SvgPatchTests.cs) | Mutation APIs maintain ownership, ordering, indexes, versions, and source-dirty state. Directly changing public collections bypasses those invariants and is unsupported. |

## CSS and computed style

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.css.parsing` | `Implemented` | [CSS tokenization and parsing](../specs/css-syntax-3.html#tokenizing-and-parsing), [SVG styling](../specs/svg2.html#styling-StylingUsingCSS) | [SvgCssTokenizerTests](../tests/SvgCssTokenizerTests.cs), [SvgCssParserTests](../tests/SvgCssParserTests.cs), [SvgCssValidatorTests](../tests/SvgCssValidatorTests.cs) | Bounded parsing covers the declared stylesheet, declaration, at-rule, value, and media/support-query profile. Unknown well-formed CSS can be retained even when it has no computed effect. |
| `svg.feature.css.selectors` | `Implemented` | [Selectors syntax](../specs/selectors-4.html#syntax), [namespaces](../specs/selectors-4.html#namespaces) | [SvgSelectorTests](../tests/SvgSelectorTests.cs), [SvgSelectorMatcherTests](../tests/SvgSelectorMatcherTests.cs), [SvgSelectorPseudoTests](../tests/SvgSelectorPseudoTests.cs) | Matching is bounded and namespace-aware. Dynamic pseudo-classes use explicit host state; pseudo-elements and browser-only state do not create a live rendering object. |
| `svg.feature.css.cascade` | `Implemented` | [CSS cascading](../specs/css-cascade-5.html#cascading), [SVG presentation attributes](../specs/svg2.html#styling-PresentationAttributes) | [SvgCascadeTests](../tests/SvgCascadeTests.cs), [SvgComputedStyleTests](../tests/SvgComputedStyleTests.cs), [SvgStyleResolverTests](../tests/SvgStyleResolverTests.cs) | Implements origins, importance, layers, inheritance, variables, presentation attributes, and typed computed values for the property catalog. Viewport, media, font, and interaction inputs are caller snapshots. |

## Geometry, paint, and effects

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.geometry.shapes` | `Implemented` | [SVG basic shapes](../specs/svg2.html#shapes-Introduction), [geometry properties](../specs/svg2.html#geometry-geometry-properties) | [SvgShapeResolverTests](../tests/SvgShapeResolverTests.cs), [SvgShapePathConverterTests](../tests/SvgShapePathConverterTests.cs) | Rect, circle, ellipse, line, polyline, and polygon geometry is implemented with typed lengths and transforms. Invalid or non-finite geometry is diagnosed and bounded. |
| `svg.feature.geometry.paths` | `Implemented` | [SVG path data](../specs/svg-paths.html#PathData), [SVG 2 path grammar](../specs/svg2.html#paths-PathData) | [SvgPathParserTests](../tests/SvgPathParserTests.cs), [SvgPathFormatterTests](../tests/SvgPathFormatterTests.cs), [SvgPathBuilderTests](../tests/SvgPathBuilderTests.cs) | Standard moveto, close, line, cubic, quadratic, and elliptical-arc commands are supported. Draft Catmull-Rom and bearing commands are outside the declared profile. |
| `svg.feature.geometry.processing` | `Implemented` | [distance along a path](../specs/svg-paths.html#DistanceAlongAPath), [stroke geometry](../specs/fill-stroke-3.html#stroke-geometry) | [SvgPathMetricsTests](../tests/SvgPathMetricsTests.cs), [SvgPathBooleanTests](../tests/SvgPathBooleanTests.cs), [SvgStrokeOutlinerTests](../tests/SvgStrokeOutlinerTests.cs) | Metrics, bounds, transforms, flattening, intersections, Boolean operations, simplification, and stroke outlines are tolerance- and work-bounded rather than symbolic exact arithmetic. |
| `svg.feature.paint.servers` | `Implemented` | [SVG gradients](../specs/svg2.html#pservers-Gradients), [SVG patterns](../specs/svg2.html#pservers-Patterns), [paint values](../specs/fill-stroke-3.html#paint) | [SvgPaintServerResolverTests](../tests/SvgPaintServerResolverTests.cs), [SvgPaintSamplerTests](../tests/SvgPaintSamplerTests.cs), [SvgPatternSamplerTests](../tests/SvgPatternSamplerTests.cs) | Solid paints, context paint, linear/radial gradients, stops, spread modes, transforms, and patterns are resolved and sampled. Unsupported CSS image-layer features remain preserved CSS without paint output. |
| `svg.feature.effects.markers` | `Implemented` | [SVG markers](../specs/svg2.html#painting-Markers) | [SvgMarkerGeometryTests](../tests/SvgMarkerGeometryTests.cs), [SvgDrawingBoundsTests](../tests/SvgDrawingBoundsTests.cs) | Start, mid, and end placement, orientation, marker units, view boxes, bounds, and rendering are implemented under geometry limits. |
| `svg.feature.effects.clipping` | `Implemented` | [CSS clipping paths](../specs/css-masking-1.html#clipping-paths), [SVG clipping paths](../specs/css-masking-1.html#svg-clipping-paths) | [SvgClipMaskRendererTests](../tests/SvgClipMaskRendererTests.cs), [SvgEffectResolverTests](../tests/SvgEffectResolverTests.cs) | Geometry and nested clip paths support user-space and object-bounding-box units. Reference depth and raster work are bounded. |
| `svg.feature.effects.masking` | `Implemented` | [CSS masking model](../specs/css-masking-1.html#masking), [SVG mask element](../specs/css-masking-1.html#MaskElement) | [SvgClipMaskRendererTests](../tests/SvgClipMaskRendererTests.cs), [SvgEffectResolverTests](../tests/SvgEffectResolverTests.cs) | Alpha and luminance masks, units, regions, nested resources, opacity, and compositing are implemented. Work and offscreen surfaces are bounded. |
| `svg.feature.effects.filters` | `Implemented` | [filter element](../specs/filter-effects-1.html#FilterElement), [filter primitives](../specs/filter-effects-1.html#FilterPrimitivesOverview) | [SvgFilterGraphTests](../tests/SvgFilterGraphTests.cs), [SvgFilterResolverTests](../tests/SvgFilterResolverTests.cs), [SvgFilterRendererTests](../tests/SvgFilterRendererTests.cs) | The standard primitive graph declared by the catalog is implemented with color-space and region handling. Primitive count, kernel, blur, turbulence, area, recursion, and surface allocation are bounded. |

## Images, text, and animation

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.images` | `Implemented` | [SVG image element](../specs/svg2.html#embedded-ImageElement), [embedded content](../specs/svg2.html#embedded-Overview) | [SvgImageResolverTests](../tests/SvgImageResolverTests.cs), [SvgImageSamplerTests](../tests/SvgImageSamplerTests.cs), [SvgNestedDocumentLoaderTests](../tests/SvgNestedDocumentLoaderTests.cs) | Placement, intrinsic sizing, aspect ratio, nested SVG, and managed PNG sampling are implemented. Other raster formats require host handling. |
| `svg.feature.images.png` | `Implemented` | [PNG concepts and datastream](../specs/png-3.html#4Concepts), [PNG error handling](../specs/png-3.html#x4-10-error-handling) | [SvgPngCodecTests](../tests/SvgPngCodecTests.cs), [SvgPngEncoderTests](../tests/SvgPngEncoderTests.cs) | Portable probing, bounded decoding, filtering, color conversion, and deterministic encoding are implemented. APNG animation is not part of the declared PNG profile. |
| `svg.feature.images.host-codec` | `CallbackRequired` | [SVG image processing](../specs/svg2.html#embedded-ImageElement), [CSS image values](../specs/css-images-4.html) | [SvgImageResolverTests](../tests/SvgImageResolverTests.cs), [SvgFeatureAnalysisTests](../tests/SvgFeatureAnalysisTests.cs) | JPEG, GIF, WebP, AVIF, and other formats require caller-provided acquisition/metadata and host decoding. Metadata alone does not supply pixels to the managed rasterizer; transcode to PNG for core rendering. |
| `svg.feature.text.layout` | `Implemented` | [SVG text layout](../specs/svg2.html#text-TextLayout), [text on a path](../specs/svg2.html#text-TextLayoutPath), [Unicode bidi](../specs/unicode-bidi-uax9.html) | [SvgTextLayoutTests](../tests/SvgTextLayoutTests.cs), [SvgTextPathLayoutTests](../tests/SvgTextPathLayoutTests.cs), [SvgBidiTests](../tests/SvgBidiTests.cs) | Unicode decoding, bidi, line breaking, positioning, baselines, text paths, and deterministic glyph geometry are implemented for the declared portable profile. Advanced shaping can require a callback. |
| `svg.feature.text.opentype` | `Implemented` | [OpenType core specification](../specs/opentype.html#core-specification), [WOFF2](../specs/woff2.html) | [SvgOpenTypeReaderTests](../tests/SvgOpenTypeReaderTests.cs), [SvgTrueTypeGlyphDecoderTests](../tests/SvgTrueTypeGlyphDecoderTests.cs), [SvgCffOutlineDecoderTests](../tests/SvgCffOutlineDecoderTests.cs), [SvgWebFontDecoderTests](../tests/SvgWebFontDecoderTests.cs) | Managed readers cover the declared TrueType, CFF, SVG-font, WOFF/WOFF2, mapping, variation, and baseline layout profile under font limits. Unsupported tables/lookup types are reported. |
| `svg.feature.text.host-shaping` | `CallbackRequired` | [OpenType advanced typographic extensions](../specs/opentype.html#core-specification), [SVG fonts and glyphs](../specs/svg2.html#text-FontsGlyphs) | [SvgOpenTypeLayoutTests](../tests/SvgOpenTypeLayoutTests.cs), [SvgFontDatabaseTests](../tests/SvgFontDatabaseTests.cs) | Unsupported scripts or layout tables use an explicit synchronous shaping callback; absent, declined, or invalid responses retain deterministic baseline glyphs. No platform shaper is loaded implicitly. |
| `svg.feature.animation.smil` | `Implemented` | [SMIL animation model](../specs/smil-animation.html#AnimationModel), [SVG animated processing mode](../specs/svg2.html#conform-animated-mode) | [SvgAnimationTimelineTests](../tests/SvgAnimationTimelineTests.cs), [SvgAnimationInterpolationTests](../tests/SvgAnimationInterpolationTests.cs), [SvgMotionAnimationTests](../tests/SvgMotionAnimationTests.cs) | Timing, syncbases, repeat, restart, events, interpolation, motion, additive/accumulate composition, and discard are sampled at caller time. There is no implicit wall clock or browser event loop. |
| `svg.feature.animation.css` | `Implemented` | [CSS animations](../specs/css-animations-1.html#animations), [keyframes](../specs/css-animations-1.html#keyframes) | [SvgCssAnimationTests](../tests/SvgCssAnimationTests.cs), [SvgAnimationCompositionTests](../tests/SvgAnimationCompositionTests.cs) | Keyframes and animation longhands are sampled against an explicit environment and caller time. Browser events and autonomous playback remain host responsibilities. |

## Interaction, accessibility, scene, and rendering

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.interaction` | `Implemented` | [SVG hit testing](../specs/svg2.html#interact-hit-testing), [SVG links and references](../specs/svg2.html#linking-URLReference) | [SvgHitTestingTests](../tests/SvgHitTestingTests.cs), [SvgInteractionStateTests](../tests/SvgInteractionStateTests.cs), [SvgNavigationTests](../tests/SvgNavigationTests.cs) | Geometry/scene hit testing, selector state, focus metadata, and navigation request construction are implemented. The library never navigates, runs a focus UI, or owns a live event loop. |
| `svg.feature.accessibility` | `Implemented` | [SVG accessibility features](../specs/svg2.html#access-AccessibilityAndSVG), [SVG accessibility tree mapping](../specs/svg-aam-1.0.html#x1-4-the-accessibility-tree-and-the-dom-tree), [graphics roles](../specs/graphics-aria-1.0.html#x4-graphics-roles) | [SvgAccessibilityTests](../tests/SvgAccessibilityTests.cs), [SvgAccessibilityTreeTests](../tests/SvgAccessibilityTreeTests.cs), [SvgAccessibilityValidatorTests](../tests/SvgAccessibilityValidatorTests.cs) | Names, descriptions, roles, states, reading/focus order, bounds, and validation are implemented. Mapping into a platform accessibility API is a host responsibility. |
| `svg.feature.scene` | `Implemented` | [SVG rendering tree](../specs/svg2.html#render-RenderingTree), [painters model](../specs/svg2.html#render-PaintersModel) | [SvgSceneBuilderTests](../tests/SvgSceneBuilderTests.cs), [SvgSceneDeterminismTests](../tests/SvgSceneDeterminismTests.cs), [SvgRenderCommandTests](../tests/SvgRenderCommandTests.cs) | Produces deterministic backend-neutral nodes, resources, layers, bounds, and command streams. Scenes are versioned snapshots and become stale after source edits. |
| `svg.feature.rendering.raster` | `Implemented` | [painting shapes and text](../specs/svg2.html#render-PaintingShapesAndText), [compositing and blending](../specs/compositing-1.html#csscompositingandblending) | [SvgRendererTests](../tests/SvgRendererTests.cs), [SvgRasterizerTests](../tests/SvgRasterizerTests.cs), [SvgPixelGoldenIntegrationTests](../tests/SvgPixelGoldenIntegrationTests.cs) | Portable deterministic premultiplied RGBA and PNG output is implemented without native assets. This is a software raster backend; GPU, PDF, canvas, and platform drawing targets consume scene/command data externally. |

## Host integration, conformance, processing, and output

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.host.foreign-content` | `CallbackRequired` | [SVG foreignObject](../specs/svg2.html#embedded-ForeignObjectElement), [foreign namespaces](../specs/svg2.html#struct-ForeignNamespaces) | [SvgEmbeddedContentResolverTests](../tests/SvgEmbeddedContentResolverTests.cs), [SvgFeatureAnalysisTests](../tests/SvgFeatureAnalysisTests.cs) | Foreign XML and `foreignObject` subtrees are preserved. HTML/layout/rendering semantics require an explicit host capability; the core does not run an HTML engine. |
| `svg.feature.resources.external` | `CallbackRequired` | [SVG URL references](../specs/svg2.html#linking-URLReference), [URI resolution](../specs/rfc3986-uri.txt) | [SvgResourcePolicyTests](../tests/SvgResourcePolicyTests.cs), [SvgResourceResolverTests](../tests/SvgResourceResolverTests.cs), [SvgStyleSheetImportTests](../tests/SvgStyleSheetImportTests.cs) | Every non-data external acquisition requires policy approval and a synchronous caller resource provider. Active policy can report `PolicyDisabled`; no network or arbitrary file I/O occurs implicitly. |
| `svg.feature.validation` | `Implemented` | [SVG document conformance](../specs/svg2.html#conform-DocumentConformanceClasses), [SVG error processing](../specs/svg2.html#conform-ErrorProcessing) | [SvgValidatorTests](../tests/SvgValidatorTests.cs), [SvgReferenceValidatorTests](../tests/SvgReferenceValidatorTests.cs), [SvgValidationSanitizerCoverageTests](../tests/SvgValidationSanitizerCoverageTests.cs) | Bounded validation covers structure, typed values, CSS, references, resources, security, and accessibility. Successful validation can still return `Valid == false`; it does not rewrite input. |
| `svg.feature.sanitization` | `Implemented` | [SVG secure static mode](../specs/svg2.html#conform-secure-static-mode), [script execution](../specs/svg2.html#interact-ScriptElementExecution) | [SvgSanitizerTests](../tests/SvgSanitizerTests.cs), [SvgSecurityValidatorTests](../tests/SvgSecurityValidatorTests.cs) | Produces an independent document with unsafe nodes, attributes, CSS, and disallowed resources removed and exact changes reported. It complements rather than replaces secure loading and resource policy. |
| `svg.feature.processing` | `Implemented` | [SVG authoring tools](../specs/svg2.html#conform-ConformingSVGAuthoringTools), [path serialization](../specs/css-syntax-3.html#serialization) | [SvgDocumentProcessorTests](../tests/SvgDocumentProcessorTests.cs), [SvgProcessPresetTests](../tests/SvgProcessPresetTests.cs), [SvgSafeCleanupTests](../tests/SvgSafeCleanupTests.cs), [SvgLossyProcessJobTests](../tests/SvgLossyProcessJobTests.cs) | Ordered jobs are bounded, convergent, deterministic, and exactly reported. Safe presets preserve accessibility/viewBox semantics; aggressive jobs require explicit semantic-loss permission and tolerances. |
| `svg.feature.generation` | `Implemented` | [conforming SVG generators](../specs/svg2.html#conform-ConformingSVGGenerators), [SVG namespace](../specs/svg2.html#struct-Namespace) | [SvgBuilderTests](../tests/SvgBuilderTests.cs), [SvgCanvasTests](../tests/SvgCanvasTests.cs), [SvgPaintBuilderTests](../tests/SvgPaintBuilderTests.cs), [SvgSpriteBuilderTests](../tests/SvgSpriteBuilderTests.cs) | Document, path, paint, filter, canvas, and sprite builders generate editable namespace-aware content. Sticky builder errors must be checked by the caller. |
| `svg.feature.writing` | `Implemented` | [XML well-formed documents](../specs/xml-1.0.html#sec-well-formed), [namespace declarations](../specs/xml-namespaces-1.0.html#ns-decl) | [SvgXmlWriterTests](../tests/SvgXmlWriterTests.cs), [SvgSourcePreservingWriterTests](../tests/SvgSourcePreservingWriterTests.cs), [SvgDeterministicWriterTests](../tests/SvgDeterministicWriterTests.cs), [SvgWriterDestinationTests](../tests/SvgWriterDestinationTests.cs) | Source-preserving, canonical, pretty, compact, UTF-8, SVGZ, and data-URL outputs are bounded and namespace-repairing. Exact source reuse applies only to unchanged eligible regions. |

## Script and legacy preservation boundaries

| Capability | Status | Specification sections | Representative tests | Preservation and known limitations |
| --- | --- | --- | --- | --- |
| `svg.feature.script.preservation` | `PreserveOnly` | [SVG script element](../specs/svg2.html#interact-ScriptElement), [script execution](../specs/svg2.html#interact-ScriptElementExecution) | [SvgFeatureAnalysisTests](../tests/SvgFeatureAnalysisTests.cs), [SvgSanitizerTests](../tests/SvgSanitizerTests.cs), [SvgRoundTripFixtureTests](../tests/SvgRoundTripFixtureTests.cs) | Script elements and event-handler attributes can round-trip as inert authored data. Sanitization removes them; no API evaluates their code. |
| `svg.feature.script.execution` | `Unsupported` | [SVG script execution](../specs/svg2.html#interact-ScriptElementExecution) | [SvgSecurityValidatorTests](../tests/SvgSecurityValidatorTests.cs), [SvgFeatureAnalysisTests](../tests/SvgFeatureAnalysisTests.cs) | Intentionally unsupported in dynamic, static, and secure-static modes. Script URLs are diagnosed and never navigated or evaluated. |
| `svg.feature.legacy.preservation` | `PreserveOnly` | [SVG 1.1 concepts](../specs/svg11.html#concepts-Concepts), [SVG 2 legacy content](../specs/svg2.html#conform-features) | [SvgLegacyCatalogTests](../tests/SvgLegacyCatalogTests.cs), [SvgElementPropertyConformanceTests](../tests/SvgElementPropertyConformanceTests.cs), [SvgRoundTripFixtureTests](../tests/SvgRoundTripFixtureTests.cs) | Recognized SVG 1.1 compatibility behavior is mapped where declared. Deprecated, vendor, SVG Tiny/1.2, and unknown content can round-trip but is never silently claimed as rendered. |

## Element and property detail

The capability rows are the stable public reporting granularity. Element and CSS
property coverage is data-driven:

- `SvgElementCatalog.CreateDefault()` publishes every current element's content
  model, attributes, categories, and render mode. The element-by-element mapping
  is exercised by
  [SvgElementPropertyConformanceTests](../tests/SvgElementPropertyConformanceTests.cs)
  and [SvgElementCatalogTests](../tests/SvgElementCatalogTests.cs).
- `SvgPropertyCatalog.CreateDefault()` publishes every supported property's
  grammar, initial value, inheritance, percentage basis, animation type,
  presentation-attribute mapping, and required capability. Property-by-property
  parsing and validation are exercised by
  [SvgElementPropertyConformanceTests](../tests/SvgElementPropertyConformanceTests.cs),
  [SvgPropertyCatalogTests](../tests/SvgPropertyCatalogTests.cs), and
  [SvgValueGrammarConformanceTests](../tests/SvgValueGrammarConformanceTests.cs).

For one input document, call `SvgCapabilities.AnalyzeDocument()` and inspect its
source-located `SvgFeatureEntry` values. That report is the authoritative answer
for whether the document's actual features are implemented, callback-required,
preserved, policy-disabled, or unsupported under the supplied host environment.
