# Serialization, processing, accessibility, and versioning

This guide defines the library's output-stability contract, explains the
serialization modes and document-processing safety model, records accessibility
defaults, and describes how applications should migrate between package
versions.

The short version is:

- Use `SourcePreserving` when retaining eligible authored spelling matters.
- Use `Canonical` when deterministic normalized output matters.
- Use `Compact` when smaller normalized output matters.
- Use named processing presets instead of assembling an unreviewed job list.
- Preview transformations with `DryRun` and inspect every reported change.
- Treat accessibility and `viewBox` preservation as explicit policy gates.
- Pin the exact package version when byte-for-byte output is an external
  contract.

## Serialization stability contract

For the same document version, options, and library version, serialization is
deterministic and culture-independent. Namespace repair, generated prefixes,
attribute ordering, typed-value formatting, CSS formatting, UTF-8 output, data
URL encoding, and SVGZ settings do not depend on dictionary iteration order,
the current culture, or a wall clock.

This determinism has deliberate limits:

- Source-preserving output depends on retained raw tokens and each node's dirty
  state. It is best-effort lexical preservation, not an unconditional byte copy.
- Canonical output is this library's stable normalized SVG/XML format. It is not
  W3C Canonical XML and must not be used as an XML-signature canonicalization
  algorithm.
- Compact output changes spelling and whitespace but does not remove document
  nodes or run optimization jobs.
- Serialization does not freeze a mutable document. `SvgWriteResult.SourceVersion`
  records the version that was written; compare it with `document.Version` before
  treating a delayed result as current.
- Canonical and compact spelling may improve between library versions as parser,
  specification, or bug fixes are released. Exact cross-version bytes are not
  promised.
- Semantically equivalent documents can serialize differently when prefixes,
  retained namespace declarations, declaration/doctype policies, or other
  explicit options differ.

Every successful write reports `WrittenNodeCount`, `WrittenAttributeCount`, and
`CharacterCount`. It also returns ordered source-map entries classifying output
regions as original, rewritten, or generated. In source-preserving mode,
`ReusedSourceRegionCount` and `ReserializedRegionCount` make fallback visible.
On an in-memory serialization failure, `Text` and the source map are cleared.
Caller-provided UTF-8 and UTF-16 spans are atomic: an undersized span receives no
partial output and reports `DestinationTooSmall` plus `RequiredLength`.

Streams and text writers remain caller-owned and are not flushed or disposed by
the library. Handle destination I/O failures according to the behavior of the
caller-provided destination.

## Serialization modes

`new SvgWriteOptions()` selects `Canonical`, preserves an authored XML
declaration when possible, omits doctypes, uses self-closing empty elements,
double quotes, namespace/expanded-name attribute ordering, no indentation, and
exact typed-value formatting.

### What “lossless” means

The public writer mode for the lossless workflow is named `SourcePreserving`.
The editable document is lossless with respect to well-formed authored SVG/XML
content: it retains namespaces, unknown/vendor nodes and attributes, lexical
tokens, source order, and source locations when loaded with source preservation.
An unchanged load/write can therefore reproduce its eligible XML regions
exactly.

Lossless does not mean that every output is automatically byte-identical. XML
reader input has no retained raw lexical tokens; programmatically constructed or
edited content has no unchanged source spelling; namespace repair may be needed;
and declaration, doctype, or formatting policy can intentionally alter output.
For an unchanged document, compare `written.Text` with the original decoded text
when whole-file character identity is required. Byte identity additionally
depends on the original encoding and byte-order mark. Use the
reuse/reserialization counts and source map to explain differences.

| Mode | Intended use | What is stable | Important caveats |
| --- | --- | --- | --- |
| `SourcePreserving` | Editors, format-preserving transforms, and reviewable patches. | Eligible unchanged raw XML tokens are copied exactly. Changed/generated regions are namespace-repaired and reserialized. | Requires source retention and raw tokens. Programmatic nodes, `XmlReader` input, dirty nodes/attributes, namespace conflicts, and incompatible output policies fall back to serialization. |
| `Canonical` | Reproducible artifacts, caches, diffs, and tests. | Deterministic namespace repair, attribute ordering, escaping, CSS formatting, and exact canonical spelling for supported typed values. | Normalized output is not lexically equal to the source and is not W3C Canonical XML. |
| `Pretty` | Human-readable normalized XML. | Canonical value spelling plus deterministic structural indentation and configured line endings. | Mixed content is not reformatted as element-only content. Formatting choices increase output size. |
| `Compact` | Delivery artifacts where size matters. | Deterministic compact XML, CSS, separators, colors, and number spelling without structural indentation. | This is a writer mode, not an optimizer; comments, metadata, unused definitions, and other nodes remain unless a processor removed them first. |

All normalized modes repair namespaces and XML escaping. Unsupported or unknown
well-formed content remains present unless a separate processing or sanitization
operation changes it. Exact formatting is the default: fractional rounding is
disabled and `MaximumAbsoluteError` is zero for every value family. Enabling
`UseFractionalPrecision` opts into bounded numeric approximation and should be
reviewed like any other tolerance-bearing transformation.

### Maximum lexical preservation

Load from text or bytes with source retention, avoid direct collection mutation,
and write with source-preserving policies. This example prohibits doctypes at
load time, so omitting them at write time cannot change accepted input.

```csharp
SvgLoadOptions loadOptions = new SvgLoadOptions();
loadOptions.PreserveSource = true;
loadOptions.PreserveUnknown = true;
loadOptions.DtdPolicy = SvgDtdPolicy.Prohibit;

SvgLoadResult loaded = SvgLoader.Load(svgText, loadOptions);
if (!loaded.Success)
{
    return;
}

SvgWriteOptions writeOptions = new SvgWriteOptions();
writeOptions.Mode = SvgWriteMode.SourcePreserving;
writeOptions.DeclarationPolicy = SvgXmlDeclarationPolicy.Preserve;
writeOptions.DoctypePolicy = SvgWriteDoctypePolicy.Omit;
writeOptions.AttributeOrder = SvgWriteAttributeOrder.Source;
writeOptions.PreserveNamespaceDeclarations = true;

SvgWriteResult written = SvgWriter.Write(loaded.Document, writeOptions);
if (!written.Success)
{
    return;
}

bool fullyReused = written.ReserializedRegionCount == 0;
```

`fullyReused` describes writer regions, not semantic validation. Editing one
attribute can force its complete start tag to be regenerated while unchanged
children and end tags remain reusable. Inspect `SourceMap` when exact provenance
matters.

To retain a doctype lexically, loading must explicitly use
`SvgDtdPolicy.PreserveOnly` and writing must use
`SvgWriteDoctypePolicy.Preserve`. The library retains the declaration as raw
metadata without entity expansion. Do not enable this merely to chase byte
identity for untrusted input.

### Canonical and compact artifacts

Serialization itself does not mutate the document, so the same snapshot can be
written in several forms:

```csharp
SvgWriteOptions canonicalOptions = new SvgWriteOptions();
canonicalOptions.Mode = SvgWriteMode.Canonical;
SvgWriteResult canonical = SvgWriter.Write(document, canonicalOptions);

SvgWriteOptions compactOptions = new SvgWriteOptions();
compactOptions.Mode = SvgWriteMode.Compact;
SvgWriteResult compact = SvgWriter.Write(document, compactOptions);

if (!canonical.Success || !compact.Success)
{
    return;
}
```

Use canonical output for stable review and cache keys inside one pinned library
version. Use compact output after processing when transfer size is the goal. For
semantic cache identity across versions, hash an application-owned semantic
model or pin the writer version; do not assume canonical text will never evolve.

## Document processing safety model

Processors operate before serialization and can change the editable document.
The processor is transaction-backed: a failed job, policy violation, limit
failure, or convergence failure rolls back attempted changes. A successful
non-dry run commits in place, refreshes indexes when configured, and invalidates
versioned caches. `SvgSanitizer` differs: it returns an independent sanitized
document and does not mutate its source.

Each processing job declares:

- whether it permits semantic loss;
- its impact severity (`None`, representation, approximation, or destructive);
- whether it preserves accessibility; and
- whether it preserves authored `viewBox` semantics.

`SvgProcessOptions.AllowSemanticLoss`, `PreserveAccessibility`, and
`PreserveViewBox` are execution gates. A job whose declarations violate the
selected policy is rejected before mutation. These gates trust job metadata: a
custom callback must classify itself honestly and must use the supplied mutation
API so changes can be tracked and rolled back.

`new SvgProcessOptions()` contains no jobs and is intentionally permissive for
custom pipelines: semantic loss is allowed and accessibility/`viewBox`
preservation are not required. Prefer a named preset for application defaults.

### Named presets

| Preset | Policy and operations | Intended use |
| --- | --- | --- |
| `SafeDefault` | Rejects semantic-loss jobs and requires accessibility and `viewBox` preservation. Removes non-semantic comments/editor metadata, safe defaults, empty or invisible unreferenced content, unused namespaces/definitions; deduplicates definitions; safely restructures groups, attributes, paths, and styles. | Conservative cleanup of an editable document. |
| `Canonicalize` | `SafeDefault` followed by exact typed-value canonicalization. | Deterministic normalized source before canonical writing. |
| `Sanitize` | Secure-static sanitization, unused namespace cleanup, empty-container cleanup, and unused-definition cleanup. Requires accessibility and `viewBox` preservation and rejects semantic-loss jobs. | In-place processing pipeline when sanitization is one job; use `SvgSanitizer` when an independent result is preferable. |
| `Sprite` | Safe cleanup, exact canonicalization, ID prefixing, local-reference flattening, and isolated sprite construction. Requires accessibility and `viewBox` preservation and rejects semantic-loss jobs. | Purposeful conversion to reusable symbols; output structure changes even though jobs are not classified as semantically lossy for that target. |
| `Aggressive` | Safe cleanup plus style/custom-property resolution, canonicalization, ID minification, shape/path/transform conversion, text outlines, effect flattening, image embedding, and reference flattening. Explicitly allows semantic-loss jobs while still requiring accessibility and `viewBox` preservation. | Size- or deployment-oriented conversion after reviewing tolerances, callbacks, and the change log. |

The semantic-loss flag is conservative but contextual. Non-lossy ID rewriting
updates references represented inside the supported document model; it cannot
see an identifier stored in application code, a separate HTML document, or an
unsupported external format. Sprite construction intentionally changes the
document's role. Conversely, a geometry conversion may be marked lossy even
when one particular input produces equivalent pixels. Review the target use,
not only the flag.

Built-in jobs classified as lossy include shape-to-path conversion, transform
baking, path simplification/rounding, text-to-path conversion, effect flattening,
recoloring/theme conversion, viewport/coordinate conversion and cropping,
fragment extraction, and document merging. Some are representation changes;
others are approximations or destructive replacements. Their exact error bounds,
providers, and work limits belong in application configuration.

### Preview and audit changes

Every tracked mutation records its pass, job ID, stable document path, mutation
kind, attribute, old/new text, source span, before/after document versions,
semantic-loss flag, severity, and reason. Use dry-run before approving an
aggressive or custom pipeline:

```csharp
SvgProcessOptions previewOptions = SvgProcessPresets.Aggressive();
previewOptions.DryRun = true;
previewOptions.MaximumPasses = 32;
previewOptions.MaximumChanges = 20_000;

SvgProcessResult preview = SvgDocumentProcessor.Process(
    document,
    previewOptions);
if (!preview.Success)
{
    return;
}

foreach (SvgProcessChange change in preview.Changes)
{
    Console.WriteLine(
        change.JobId + ": " + change.Severity + " " + change.Path);
}

// Build a fresh preset because option and job objects are mutable.
SvgProcessOptions applyOptions = SvgProcessPresets.Aggressive();
applyOptions.MaximumPasses = 32;
applyOptions.MaximumChanges = 20_000;
SvgProcessResult applied = SvgDocumentProcessor.Process(document, applyOptions);
if (!applied.Success)
{
    return;
}
```

A successful dry run rolls back attempted mutations, so `Changed` is false even
when `Changes` contains prospective work; `RolledBack` reports whether anything
was attempted. Rebuilding the preset avoids accidental mutation or reuse of
caller-owned option objects between preview and commit.

## Accessibility defaults

Accessibility is a first-class semantic output, not a platform accessibility
bridge. `SvgAccessibility` and `SvgAccessibilityTreeBuilder` return bounded,
platform-neutral snapshots for host integration.

Accessible names use this precedence:

1. `aria-labelledby` referenced text;
2. `aria-label`; then
3. a direct `title` child.

Accessible descriptions use `aria-describedby` before a direct `desc` child.
Relationship lookup is cycle-aware and bounded. Default per-node semantic limits
are one million visited nodes, one million text characters, and 100,000 ID
references. Accessibility-tree defaults separately bound the tree to one million
resolved nodes and one million logical text characters.

The semantic resolver:

- maps recognized explicit roles and treats `none` as `presentation`;
- supplies implicit roles for root/nested SVG, links, images, text, groups,
  `use`, switches, and shapes;
- ignores a presentation role when focusability or global ARIA semantics require
  the object to remain exposed;
- marks inactive, `display:none`, ancestor `aria-hidden=true`, definitions,
  styles, scripts, metadata, titles, and descriptions as hidden;
- maps and validates supported ARIA states/properties; and
- derives focusability from a valid `tabindex` or a resolved link target.

The accessibility tree exposes logical reading order, sequential focus order,
world-space bounds, normalized text, source/instance identities, and diagnostics.
It is versioned: call `IsCurrent()` and rebuild after source edits or resolved
document changes.

Validation reports missing required names/descriptions, broken relationships,
contradictory roles/states, hidden focusable content, keyboard-inaccessible
links, and preservation risks around `title` and `viewBox`. Successful validation
does not manufacture missing authoring data.

Every named process preset sets `PreserveAccessibility = true` and
`PreserveViewBox = true`. Safe cleanup protects `title`, `desc`, metadata, roles,
`tabindex`, ARIA attributes, and referenced semantic subtrees from ordinary
removal. Text-to-path conversion defaults to retaining source text as an
`aria-label` when no explicit label already exists. Applications can disable
that option only as an explicit accessibility decision.

These guarantees preserve the semantics represented by the library; they cannot
verify how a browser, assistive technology, or host platform bridge ultimately
presents the output. Test important content with the actual delivery platform.

## Package version and migration promises

Published package versions follow Semantic Versioning 2.0:

- Patch releases fix defects, conformance, diagnostics, performance, or security
  without intentionally removing a supported public API.
- Minor releases add backward-compatible APIs and feature support. New enum
  values, public option fields, capability entries, or diagnostics may appear.
- Major releases may remove or rename APIs, change signatures, raise the target
  framework, or intentionally revise established behavior that cannot be kept
  compatible.

When practical, an API scheduled for removal is deprecated for at least one
minor release before a major removal. Migration-relevant changes are called out
in release notes. Security fixes may tighten acceptance, default limits, URI or
media policy, or sanitization in a patch release when retaining the old behavior
would be unsafe; such a change is documented even though it can reject input
that an earlier patch accepted.

The following identifiers are durable compatibility surfaces:

- published `svg.profile.*` and `svg.feature.*` identifiers are never reassigned;
  aliases receive distinct identifiers;
- diagnostic code numeric values are append-only and are not reassigned;
- diagnostic code names, processing job IDs, and stable result/error enum member
  meanings are not repurposed.

Feature status is intentionally queryable and can improve as support is added.
Code should call `SvgCapabilities.GetLibraryReport()` instead of assuming a
capability's status forever. Human-readable diagnostic messages, formatting
details, source-map region boundaries, performance, and the exact ordering of
newly introduced diagnostics are not machine contracts unless explicitly
documented as stable. Branch on codes and typed fields rather than message text.

Within a package version, document and snapshot version fields are mutation
guards, not package versions. Editing through `SvgNodeApi`, `SvgDocumentApi`, or
`SvgBuilder` increments document state and invalidates derived caches. Directly
mutating public collections bypasses those invariants and is unsupported.

### Upgrade checklist

1. Read the release notes and update the package under an explicit dependency
   change rather than an unbounded floating version.
2. Build with warnings enabled and replace deprecated APIs before the next major
   version.
3. Treat enums as open sets in persisted data and `switch` statements; retain a
   safe unknown/default path.
4. Query the capability report and compare only stable identifiers/status enums,
   not human-readable detail text.
5. Run load-write-load semantic equality tests for representative documents.
6. Regenerate and review canonical/compact golden files. Pin the previous exact
   package when byte identity cannot change immediately.
7. Dry-run processing presets and compare change kinds, semantic-loss flags,
   severities, accessibility output, view boxes, and rendering goldens.
8. Recheck tightened security defaults and any host callbacks, codecs, fonts,
   caches, cancellation, or resource policies.
9. Rebuild resolved documents, scenes, accessibility trees, and other versioned
   snapshots rather than persisting internal object graphs across versions.

For the general API flow, see [Getting started](getting-started.md). For security
and untrusted-input deployment, see the [security posture and deployment guide](security.md).
For current feature behavior, see the [standards and support matrix](support-matrix.md).
