# YamlDotNet Removal Inventory

This historical inventory records the behavior preserved during replacement
of `YamlSourceParser`. The replacement is complete: runtime parsing now uses
the built-in scanner and token parser, and the library has no YamlDotNet
package or code dependency. The normative authority is YAML 1.2.2 in
`specs/yaml-1.2.2.md`; the executable examples are from the pinned
`data-2022-01-17` YAML Test Suite archive.

## Former routing boundary

Before removal, `YamlParser.ParseScannerResult` selected `YamlSourceParser`
when the scanner result retained non-empty source text, parser options were
non-null, and `IncludeComments` was false. `ParseTokens`, an empty-source
scanner result, a null options argument, and comment-preserving parsing used
the built-in token parser.

The public source-backed call paths were:

1. `IYamlPipeline.ParseScannerResult` and `ParseBytes`.
2. `Yaml.ComposeText` and `ComposeBytes`.
3. Every processor text/byte parse operation through those compose helpers.
4. Processor load and deserialize operations through their parse operations.

The focused caller regression uses fixture `4MUZ/02`. T025 unified these paths,
and T026 verified source/token parity against the full pinned suite.

## Dependency and behavior inventory

| Area | Former dependency or adapter behavior | Preserved replacement behavior | Focused fixtures |
| --- | --- | --- | --- |
| Parser driver | `YamlDotNet.Core.Parser` and `MoveNext` produce the source-backed event stream. | Produce the same YAML event structure from the built-in scanner/parser. | `FQ7F`, `ZF4X`, `M7NX` |
| Event translation | YamlDotNet stream, document, sequence, mapping, scalar, and alias events are converted to public `YamlEvent` kinds. | Preserve ordering, implicit empty scalars, block/flow collection style, and all scalar styles. | `G4RS`, `4WA9`, `RTP8` |
| Properties | `AnchorName` and `TagName` are copied to event anchor/tag fields; YamlDotNet expands declared handles. | Preserve anchors and aliases and expand local, verbatim, primary, secondary, and declared tag handles according to YAML 1.2.2 chapter 6. | `3GZX`, `7FWL`, `C4HZ` |
| Source marks | YamlDotNet zero-based indices and one-based lines/columns are copied to public spans. Explicit document marker spans are found in the original source. | Retain accurate original-source spans even when parsing multiline or repaired grammar. | `RTP8` plus the explicit marker/span regression |
| Flow-colon normalization | Newlines before a flow-mapping value indicator are removed before YamlDotNet parsing. | Accept a flow mapping colon on the following line as grammar, without rewriting source. | `4MUZ/00`, `4MUZ/01`, `4MUZ/02` |
| Whitespace-only tab normalization | Lines containing only tabs are removed before YamlDotNet parsing. | Treat legal tab-only separation lines as separation and retain original positions. | `DK95/03`, `DK95/04`, `DK95/05` |
| Terminal keep normalization | A terminal whitespace-only line after a keep-chomp block scalar is normalized to retain the required final empty line. | Implement block scalar indentation and keep chomping directly. | `6FWR`, `JEF9/01` |
| Scalar correction | Built-in scanner block-scalar values replace a YamlDotNet value when the scanner retains one additional required newline. | Implement literal/folded content, indentation, and chomping directly and emit the fixture values. | `L24T/00`, `6FWR` |
| Compact grammar repair | Exact compact empty mapping forms are synthesized because YamlDotNet rejects them. | Parse compact empty mappings generally, including empty keys and values. | `UKK6/00`, `M2N8/00` |
| Adjacent commented documents | A narrow two-document form is sent to the built-in token parser instead of YamlDotNet. | Parse adjacent documents and trailing comments through the unified parser. | `L383` |
| Strict directive rejection | A directive comment without separating white space is rejected before YamlDotNet. | Diagnose the YAML separation violation synchronously. | `MUS6/00` |
| Strict simple-key rejection | An implicit mapping key in a flow sequence cannot put its value indicator on a later line. | Enforce the YAML 1.2.2 simple-key single-line restriction. | `DK4H` |
| Strict tab rejection | Structural indentation/separation tab cases that YamlDotNet accepts are rejected before dependency parsing. | Distinguish forbidden indentation tabs from legal content/separation tabs. | `DK95/01`; legal controls `DK95/03` through `DK95/05` |
| Failure bridge | YamlDotNet exceptions escape a worker task and are flattened into a structured parser diagnostic. | Return bounded synchronous diagnostics without dependency exceptions escaping public APIs. | `CQ3W`, `55WF`, `CML9` |

## Non-behavioral and obsolete helpers

`NormalizeLeadingBlockBlankLines`, `NormalizeAdjacentCommentedDocuments`, and
`FindPreviousDocumentMarker` have no callers. They are not replacement
requirements and can be removed with `YamlSourceParser`. `AddEvent` receives
parser options but does not use them. YamlDotNet directive events are not
translated; directives and trivia are currently available only on the
built-in token-parser path. T023 owns unified directive handling, while T024
owns removal of the task-based failure bridge.

## Replacement acceptance boundary

The focused regressions now live in `YamlBuiltInRoutingTests` as a small, named
guard set. The full 402-case suite remains the final behavioral authority and
is exercised by `YamlTestSuiteConformanceTests`; source-backed scanner results
and standalone tokens pass without an adapter.
