# Public API contract

This document fixes the shape of the public API before implementation. The
contract targets the `svg` namespace in the `svg` assembly. A name appearing here
is reserved for the stated responsibility; implementation tasks may add members,
but must not change the meaning of a published member.

Member declarations below are signature notation. Implementations are ordinary,
block-bodied C# methods and constructors. The public surface is physically and
semantically divided under `src/Api` into contracts, data, constants, enums,
exceptions, and interface implementations. It uses only:

- public interfaces with instance methods only;
- public implementation classes whose instance methods exactly implement their interfaces;
- documented public fields on data classes with no public methods;
- constants-only public static data classes;
- documented constructors and ordinary instance methods on implementation classes;
- `Func<...>` and `Action<...>` for short-lived predicates, notifications, and callback adapters; and
- concrete .NET collection classes and arrays for multiple values.

Each implementation class declares at most one constructor. Alternative creation
modes use separate factory interfaces and implementations, such as
`ISvgCatalogFactory` and `SvgCatalogFactory`; public static factory methods are
not part of the API.

The public surface has no properties, structs, records, custom delegates, events,
operators, class inheritance other than interface implementation and direct
exception bases, generic declarations, nullable value types, `ref`/`out`
parameters, async methods, or iterators. Public interfaces include reusable host
services and the contracts for public operation classes. They have instance
methods only, do not inherit interfaces, and have names beginning with `I`
followed by an upper-case letter. An optional reference field, parameter, or return is annotated with
`?`, and its documentation states when `null` is used. Methods returning multiple
values use a named result class. Variant values use an enum `Kind` field and
explicit payload fields on a class.

Data classes expose fields and constructors only. Any methods described on a
field-bearing type later in this document are internal implementation helpers;
public behavior is supplied by an interface implementation. Former static
operation overloads use distinct instance-method names, generally with a
`With...` suffix that identifies their parameters.

## Shared conventions

- Callers retain ownership of supplied streams, readers, XML readers, byte
  arrays, callbacks, and option objects unless a member explicitly says it copies
  them.
- Public operations are synchronous. Long-running operations accept a
  `CancellationToken` field on their option class and check it at bounded
  intervals.
- Options are mutable input classes. Results and model objects are
  mutable-by-contract only where their type documentation permits it.
- Collections returned in result fields are snapshots. Editable document
  collections are owned by their document and changed through `SvgDocumentApi`
  or `SvgNodeApi`.
- Recoverable input problems are returned as `SvgDiagnostic` entries.
  Unrecoverable API, XML, resource, and rendering failures use the direct
  exception classes defined by the diagnostics contract.
- All offsets and counts are zero-based. Source line and column numbers are
  one-based, with zero meaning unavailable.

## Editable document

| Type | Public contract |
| --- | --- |
| `SvgNodeKind` | Enum tags document, element, text, CDATA, comment, processing-instruction, and doctype nodes. |
| `SvgElementKind` | Enum tags recognized SVG elements and has `Unknown` for preserved extension content. |
| `SvgElementCatalog`, `SvgElementDefinition` | The default catalog contains every current SVG element exactly once. Definitions expose semantic categories, child-kind/cardinality and text/foreign-content models, element-specific plus global/presentation attribute applicability and defaults, and direct/container/text/referenced/resource/host/never render modes. |
| `SvgLegacyCatalog`, `SvgLegacyFeatureDefinition`, `SvgLegacyAnalysisResult` | The default legacy catalog classifies every recognized SVG 1.1 element, unqualified legacy attribute, and XLink attribute as implemented, preserve-only, deprecated, or unsupported. Analysis returns document-ordered located occurrences with explicit compatibility, preservation, and unsupported diagnostics. |
| `SvgDocument` | Fields: `SvgNode DocumentNode`, `SvgNode RootElement`, `string BaseUri`, `long Version`, `List<SvgDiagnostic> Diagnostics`. Constructor creates an empty document. Methods return read-only snapshots of nodes and attributes. |
| `SvgNode` | Fields: `SvgNodeKind Kind`, `SvgElementKind ElementKind`, qualified-name strings, `string Value`, `SvgNode Parent`, `SvgDocument Document`, `List<SvgNode> Children`, `List<SvgAttribute> Attributes`, `SvgSourceSpan Source`, `bool Dirty`, `long Version`. Constructor accepts a node kind and qualified-name parts. |
| `SvgAttribute` | Fields: qualified-name strings, `string Value`, `SvgNode Owner`, `SvgSourceSpan Source`, `bool Dirty`. Constructor accepts qualified-name parts and a value. |
| `SvgDocumentApi` | Constructors accept no global state. Ordinary methods create documents/elements/non-element nodes, import or adopt a node, begin a change set, diff documents, and apply a patch. |
| `SvgNodeApi` | Ordinary methods append, insert, replace, remove, move, clone, normalize text, read/write text content, manage attributes, resolve namespaces, inspect ancestry/siblings, and test deep equality. Mutations return explicit result classes where they can fail. |
| `SvgQuery` | Ordinary methods find by ID, class, qualified name, namespace, source span, selector, or `Func<SvgNode, bool>`. Multi-result methods return `List<SvgNode>` snapshots in document order. |
| `SvgChangeSet` | Fields: `SvgDocument Document`, `List<SvgChange> Changes`, `bool Completed`. Ordinary methods commit, roll back, and create an inverse change set. |
| `SvgPatch` | Fields: ordered patch operations and diagnostics. Ordinary methods validate preconditions, apply atomically, and create an inverse patch result. |

## Loading, resources, diagnostics, and limits

| Type | Public contract |
| --- | --- |
| `SvgProcessingMode` | Enum values `Dynamic`, `Static`, and `SecureStatic`. Script is never executed in any mode. |
| `SvgLoader` | Ordinary `Load` methods accept `string`, `ReadOnlySpan<char>`, `ReadOnlySpan<byte>`, `byte[]`, `Stream`, `TextReader`, `XmlReader`, or a filesystem path plus `SvgLoadOptions`, and return `SvgLoadResult`. URI loading is a distinct method and always uses the configured callback. |
| `ISvgResourceProvider`, `SvgResourceProvider` | The interface supplies one synchronous approved resource request and can retain host state across loading and resolution. The concrete adapter wraps a `Func<SvgResourceRequest, SvgResourceResponse>` for concise one-off configuration. |
| `SvgLoadOptions` | Fields: `SvgProcessingMode Mode`, `string BaseUri`, `string ContentType`, `SvgSecurityLimits Limits`, `SvgResourcePolicy ResourcePolicy`, `ISvgResourceProvider ResourceProvider`, `CancellationToken CancellationToken`, and strict/preservation flags. Constructor installs secure defaults. |
| `SvgLoadResult` | Fields: `SvgDocument Document`, `List<SvgDiagnostic> Diagnostics`, resource summary, detected media type/encoding/container strings, and `bool Success`. |
| `SvgInputLimits`, `SvgTreeLimits`, `SvgCssLimits`, `SvgReferenceLimits`, `SvgResourceLimits`, `SvgGeometryLimits`, `SvgFontLimits`, `SvgFilterLimits`, `SvgRasterLimits`, `SvgTimelineLimits` | Public integer and long fields bound input/gzip, XML trees, CSS, reference expansion, resources, geometry, fonts, filters, raster/PNG work, and timelines. Each constructor installs explicit secure defaults. |
| `SvgSecurityLimits` | Fields contain one independently allocated instance of every limit group. `Validate` checks groups and positive fields in declaration order and returns `SvgSecurityLimitValidation` identifying the first invalid group, field, value, and message without throwing. |
| `SvgResourceRequest` | Fields: normalized and source URI strings, requested media kinds, provenance spans, redirect/reference depth, and `CancellationToken`. |
| `SvgResourceResponse` | Fields: `byte[] Bytes`, `Stream Stream`, declared/actual media metadata, final URI, cacheability, and diagnostics. Exactly one data source is used. |
| `SvgResourcePolicy` | Fields describe allowed schemes, origins, file roots, redirects, data media types, and size/count limits. Ordinary methods decide a request before and after resolution and return a decision result. |
| `SvgResourceCache` | Constructor accepts bounded capacities. Ordinary methods get, add, remove, and clear normalized resource entries and return snapshots of cache statistics. |
| `SvgDiagnosticSeverity` | Enum values `Info`, `Warning`, and `Error`. |
| `SvgDiagnosticBehavior` | Enum values `Strict` and `Compatible`. Strict processing stops after its first error; compatible processing continues after recoverable errors. |
| `SvgDiagnosticCode` | Enum of stable diagnostic codes; numeric values are only appended and never reassigned. `SvgDiagnosticCodeNames.Get` maps them to stable `SVG`-prefixed text without reflection. |
| `SvgDiagnostic` | Fields: severity, code, message, primary `SvgSourceSpan`, related spans, resource/instance paths, and bag-assigned sequence number. |
| `SvgDiagnosticBag` | Constructor accepts a nonnegative retention bound and behavior. Ordinary methods add diagnostics, report stop state, clear state, and return an independently allocated deterministic snapshot. Overflow retains one truncation diagnostic and counts every omitted entry. |
| `SvgDiagnosticOrdering` | Its interface-backed ordinary comparison orders located diagnostics by nested source mapping, then severity, code, message, instance path, and sequence; unlocated diagnostics follow located diagnostics. |
| `SvgSourceUri` | Fields: preserved URI text, parent resource, requesting span, stable ordinal resource path/index, and depth. The root constructor and `CreateNested` factory establish provenance without normalizing URI text. |
| `SvgSourcePosition` | Fields: zero-based UTF-16 offset and one-based line/column coordinates, with explicit unavailable sentinels. Methods compare positions only when they share an available coordinate system. |
| `SvgSourceSpan` | Fields: `SvgSourceUri Source`, start/end offsets, and start/end line and column positions. `FromPositions` creates a span from endpoint objects; half-open span methods test containment and overlap and create an unknown span. |
| `SvgSourceMapping` | Interface-backed ordinary methods reconstruct nested-resource provenance and compare spans in deterministic root/resource/source order. |
| `SvgException`, `SvgXmlException`, `SvgResourceException`, `SvgRenderingException` | Direct sibling `System.Exception` subclasses for unrecoverable API, XML, resource, and rendering failures. Each full constructor and its convenience `Create` factories preserve messages and inner exceptions; public diagnostic fields and `AddDiagnostic` retain an optional primary diagnostic plus ordered related diagnostics. |

## SVG and CSS values

Every value class has a tag or unit enum where variants exist, explicit payload
fields, a constructor for programmatic creation, and no implicit conversions.

| Type | Public contract |
| --- | --- |
| `SvgNumber` | Finite `double Value` plus source and validity fields. |
| `SvgLength` | Numeric value, length unit, percentage state, source, and validity fields; evaluation is an ordinary method accepting a context class. |
| `SvgAngle` | Numeric value, angle unit, source, and validity fields; conversion methods return a new value. |
| `SvgTime` | Numeric value, time unit, indefinite state, source, and validity fields. |
| `SvgColor` | Color-space tag, channel fields, alpha, current-color state, source, and validity fields; conversion/interpolation return new colors. |
| `SvgPaint` | Paint-kind tag plus color, URL, context paint, fallback list, source, and validity fields. |
| `SvgTransform` | Transform-kind tag, numeric parameters, source, and validity fields; list composition returns `SvgMatrix`. |
| `SvgViewBox` | `double X`, `Y`, `Width`, and `Height` fields plus source and validity fields. |
| `SvgAspectRatio` | Alignment, meet/slice, defer, source, and validity fields; viewport fitting returns `SvgMatrix`. |
| `SvgPathCommand` | Command-kind tag, relative/absolute state, numeric payload array, source offset, and validity fields. |
| `SvgPathData` | Ordered `List<SvgPathCommand> Commands`, source, validity, and parse diagnostics. Methods return canonical or transformed copies rather than exposing internal dense buffers. |
| `SvgTiming` | Timing-kind tag plus offset, sync/event/repeat/access-key strings and values, source, and validity fields. |
| `SvgValueParser` | Ordinary parse methods accept strings or spans, limits, and compatibility mode, returning typed parse-result classes with value, success, next/error offset, and diagnostics. |
| `SvgValueFormatter` | Ordinary methods format each typed value using a formatting-options class and return strings. |

## CSS and computed style

| Type | Public contract |
| --- | --- |
| `SvgStyleSheet` | Fields: ordered rule list, origin/layer metadata, source, version, and diagnostics. |
| `SvgCssRule` | Rule-kind tag plus selectors, declarations, child rules, at-rule name/prelude, source, and validity fields. |
| `SvgCssDeclaration` | Name, original tokens/text, parsed value, important flag, source order, source, and validity fields. |
| `SvgSelector` | Selector tree/list data, specificity components, source, validity, and diagnostics. |
| `SvgStyleEnvironment` | Viewport, media, language, reduced-motion, caller stylesheet, interaction state, capabilities, and cancellation fields. |
| `SvgCascade`, `SvgCascadeOptions`, `SvgCascadeResult` | Ordinary resolve methods cascade ordered or collected sheets for one element. Options supply property metadata, limits, selector state, media state, an optional parent result, and built-in SVG user-agent-default control; results expose resolved specified values, computed custom-property token streams, transitive variable dependencies, winning-declaration provenance, considered candidates, presentation-attribute compatibility recovery, and diagnostics. |
| `SvgComputedStyle`, `SvgComputedValue`, `SvgComputedStyleConverter` | Tagged typed longhand table, custom-property token table, source node/version, dependency/provenance data, SVG 1.1 compatibility state, and diagnostics. Ordinary methods get named color, paint, length, number, keyword, and transform values; conversion applies bounded property-family parsing, SVG inheritance, current-color resolution, deferred context paint, and invalid-computed-value recovery. |
| `SvgCssParser` | Ordinary methods parse a stylesheet, declaration list, style attribute, selector, media query, or supports query and return named result classes. |
| `ISvgDynamicSelectorState`, `SvgDynamicSelectorState` | The interface supplies versioned link, hover, active, focus, target, and host-language state as one cohesive cacheable host service. The concrete implementation retains the existing delegate-based convenience fields and increments its revision through `MarkChanged`. |
| `SvgStyleResolver`, `SvgStyleResolverOptions`, `SvgComputedStyleTree` | Ordinary methods compute one node or a document with ancestor-aware inheritance and a bounded computed-style cache. Options supply stylesheet, selector, viewport/media, font-revision, and capacity inputs; version snapshots refresh stale entries automatically, explicit dependency invalidation covers tree/attribute/class-ID/stylesheet/custom-property/viewport/media/font/interaction changes, and statistics expose hits, misses, invalidations, and evictions. |

Cascade ordering and CSS-wide keyword behavior follow the local
[CSS Cascading and Inheritance Level 5](specs/css-cascade-5.html) snapshot;
SVG presentation-attribute placement follows the local
[SVG 2 styling](specs/svg2.html#Styling) snapshot.

## Geometry and paths

| Type | Public contract |
| --- | --- |
| `SvgPoint` | `double X`, `Y`; constructor rejects non-finite values. |
| `SvgVector` | `double X`, `Y`; constructor rejects non-finite values. |
| `SvgSize` | `double Width`, `Height`; constructor rejects non-finite or negative values. |
| `SvgRect` | `double X`, `Y`, `Width`, `Height` plus empty state; the nonempty constructor and explicit-empty-state `Create` factory validate finite values. |
| `SvgMatrix` | Six affine `double` fields; constructor validates finite values. |
| `SvgGeometry` | Stateless ordinary methods for matrix, point/vector/rectangle, bounds, shape conversion, containment, and transformation operations. |
| `SvgPathBuilder` | Fields store the current path. Ordinary methods move, line, curve, arc, close, append, reset, and return an independent `SvgPathData` snapshot. |
| `SvgPathMetrics` | Constructor accepts a path, tolerance, and limits. Ordinary methods return length, bounds, and named point/tangent/trim result classes. |
| `SvgPathBoolean` | Ordinary methods union, intersect, subtract, or xor two paths with fill rules, tolerance, and limits, returning a result with path and diagnostics. |
| `SvgHitTest` | Ordinary methods query a path, scene, resolved document, source node, or instance and return topmost or ordered-all `SvgHitResult` values. |

## Resolution, scene, fonts, and text

| Type | Public contract |
| --- | --- |
| `SvgResolveOptions` | Fields: viewport, language preferences, `SvgCapabilitySet`, resource policy/callback, style environment, animation time, limits, strictness, and cancellation. |
| `SvgResolvedDocument` | Fields: source document/version, resolved root, scene, diagnostics, feature report, resource summary, and success state. |
| `SvgResolvedNode` | Resolved-node kind, source/instance identity, children, computed style, viewport, transform, geometry, bounds, paints, effects, text/image data, and diagnostics. |
| `SvgReferenceResolver` | Ordinary methods resolve same-document and external references, href inheritance, use instances, and cycles using explicit options and return named results. |
| `SvgCapabilitySet` | Fields contain supported library/host identifiers. Ordinary methods add, remove, test, clone, and return deterministic identifier snapshots. |
| `SvgScene` | Fields: root scene node, shared paint/image/effect/font resources, bounds, source version, and diagnostics. |
| `SvgSceneNode` | Scene-node kind plus source/instance identity, children, transform, opacity/compositing, geometry/text/image payloads, paints/effects, and bounds. |
| `SvgSceneBuilder` | Ordinary build method accepts a resolved document and scene options and returns a scene result. |
| `SvgRenderCommand` | Command-kind tag plus node/resource identity and explicit transform, clip, mask, layer, path, text, image, paint, and bounds payload fields. |
| `SvgCommandEnumerator` | Constructor accepts a scene and options. `MoveNext` is an ordinary method; current command is a public field. The class does not implement `IEnumerable` or `IEnumerator`. |
| `SvgFont` | Font bytes/metadata, parsed tables, metrics, coverage, variations, and diagnostics. Bytes are caller-owned unless the load option requests a copy. |
| `SvgFontFace` | Family/style/weight/stretch, Unicode ranges, sources, variation axes, feature metadata, and source fields. |
| `SvgFontDatabase` | Ordinary methods add/remove caller fonts, set generic mappings, match a face, construct fallback runs, clear caches, and return snapshots. |
| `SvgTextRun` | Source text/range, scalar and cluster data, direction/script/language, requested face/style, and positioning inputs. |
| `SvgGlyphRun` | Font, glyph IDs, clusters, advances, offsets, direction, transform, bounds, and diagnostics. Numeric arrays are dense primitive arrays. |
| `SvgTextLayout` | Lines/chunks/glyph runs, logical and ink bounds, source mappings, and diagnostics. Ordinary methods perform hit testing and extraction. |
| `SvgTextOptions` | Font database, shaping callback, language/direction defaults, feature/variation settings, limits, and cancellation. |

## Rendering, animation, interaction, and accessibility

| Type | Public contract |
| --- | --- |
| `SvgPixelBuffer` | Width, height, stride, premultiplied-RGBA byte array, and ownership fields. Ordinary methods get/set/copy/clear pixels through named color result classes rather than indexers. |
| `SvgRenderOptions` | Viewport, scale, target, background, animation time, image/font/foreign-content callbacks, limits, strictness, and cancellation fields. |
| `SvgRenderResult` | Pixel buffer, PNG bytes when requested, drawing bounds, diagnostics, resource summary, feature report, and success state. |
| `SvgRenderer` | Ordinary methods render documents, resolved documents, scenes, fragments, source nodes, or instances to RGBA, PNG, or command results. |
| `SvgPngCodec` | Ordinary methods probe, decode, and encode PNG data using explicit options and named result classes. |
| `SvgAnimation` | Animation-kind tag, target/property, timing, value/keyframe data, interpolation/additive/accumulate settings, motion data, source, and diagnostics. |
| `SvgAnimationState` | Current caller time, injected event times, overrides, removed nodes, dirty nodes, and version. Ordinary methods inject/clear events and sample. |
| `SvgAnimationSample` | Time, sampled values, removed/dirty node snapshots, diagnostics, and success state. |
| `SvgInteractionState` | Target/link/hover/active/focus identifiers, pointer positions/buttons, reduced-motion state, and version. Ordinary methods update or clear each state. |
| `SvgPointerInput` | Pointer kind, position, button/buttons, modifiers, click count, and caller timestamp fields. |
| `SvgHitResult` | Source node, instance path, scene node, local/world point, fill/stroke/bounds classification, distance, cursor, link, and document-order fields. |
| `SvgNavigationRequest` | Source node, original/resolved URI, target, download metadata, external/internal state, and policy decision fields. The library returns but never executes it. |
| `SvgAccessibilityNode` | Source/instance identity, role, name, description, state/property tables, bounds, focusability/order, hidden/presentation state, and children. |
| `SvgAccessibilityTree` | Root, flat reading/focus order snapshots, diagnostics, and source version. Ordinary methods find by source or instance identity. |

## Validation, processing, generation, writing, and capabilities

| Type | Public contract |
| --- | --- |
| `SvgValidationOptions` | Profile identifiers, severity controls, resource/reference checks, accessibility checks, limits, and cancellation fields. |
| `SvgValidationResult` | Diagnostics, feature report, checked node/reference counts, source version, and success state. |
| `SvgValidator` | Ordinary methods validate a document, node, resolved document, or scene with explicit options. |
| `SvgSanitizeOptions` | Allow/deny fields for script/event URLs, foreign content, resources, imports, fonts, data media types, links, metadata, unknown namespaces, limits, and cancellation. |
| `SvgSanitizeResult` | Sanitized document, ordered changes, diagnostics, feature report, source version, and success state. |
| `SvgSanitizer` | Ordinary methods create a sanitized copy or sanitize an editable document in an explicit change set. |
| `SvgProcessOptions` | Ordered jobs, lossless/lossy mode, tolerances, pass limit, dry-run state, accessibility/ID preservation, limits, and cancellation. |
| `SvgProcessJob` | Job-kind tag, stable identifier, prerequisite identifiers, option payload fields, and optional `Func` callback. |
| `SvgProcessResult` | Document, ordered changes, diagnostics, pass count, changed/dry-run state, feature report, and success state. |
| `SvgProcessor` | Ordinary method runs an ordered job list and returns `SvgProcessResult`. |
| `SvgChange` | Change-kind tag, stable path/source identity, before/after text or node data, semantic-loss state, reason, and source. |
| `SvgBuilder` | Ordinary methods create documents/elements/text and set attributes/styles; mutating builder methods return the same builder class. |
| `SvgPaintBuilder` | Ordinary methods add gradients, patterns, stops, markers, and paint references and return the same builder class. |
| `SvgFilterBuilder` | Ordinary methods add each standard filter primitive and connect named inputs/results, returning the same builder class. |
| `SvgCanvas` | Ordinary save/restore, transform, path, fill/stroke, clip, image, text, layer, and definition methods; a build method returns an editable document. |
| `SvgSpriteBuilder` | Ordinary methods add documents/fragments as symbols with ID isolation and return the same builder; build returns a document plus mapping diagnostics. |
| `SvgWriteMode` | Enum values `SourcePreserving`, `Canonical`, `Pretty`, and `Compact`. Container/encoding choices are independent option fields. |
| `SvgWriteOptions` | Mode, namespace/doctype/declaration, quoting/whitespace, precision, stable ordering, SVG/SVGZ, text/UTF-8/data-URL, limits, and cancellation fields. |
| `SvgWriteResult` | Text or bytes, source map, diagnostics, written counts, media/container metadata, and success state. |
| `SvgWriter` | Ordinary methods write a document or fragment to string, UTF-8 bytes, SVGZ bytes, data URL, `Stream`, `TextWriter`, or `XmlWriter`. Caller destinations remain open. |
| `SvgFeatureStatus` | Enum values `Implemented`, `CallbackRequired`, `PreserveOnly`, `PolicyDisabled`, and `Unsupported`. |
| `SvgFeatureEntry` | Fields: stable feature/profile identifiers, status, detail, optional source, and report sequence. Construction validates identifiers and status without reflection. |
| `SvgCapabilities` | Interface-backed ordinary methods return profile and feature identifier snapshots, a current-library report, and status lookup. The catalog is explicit and uses no runtime reflection. |
| `SvgFeatureReport` | Fields: feature entries and diagnostics. Ordinary methods add validated entries, find by identifier, and return independent deterministic snapshots filtered by status or profile. |

## Compatibility rules

The identifiers `svg.profile.svg2`, `svg.profile.svg11.compat`,
`svg.profile.legacy.preserve`, and `svg.profile.host.callback` have the meanings
defined in `README.md`. Individual feature identifiers begin with `svg.feature.`.
Identifier spelling and enum numeric values become append-only once released.

Adding a field, factory or method overload, enum member, result class, option
class, or separate interface is compatible. Adding a member to an existing public
interface, removing or renaming a member, changing field meaning, changing a
method's ownership rules, or reusing an identifier is breaking. Before the first
stable release, an intentional breaking change must update this document and the
task that implements the affected family in the same change.
