# UniversalHtml

UniversalHtml is a zero-dependency .NET 10 library for browser-compatible HTML parsing, traversal, sanitization, serialization, and conversion to plain text or Markdown.

The project is in its initial `0.1.0-alpha.1` development cycle. The public API is available for evaluation but remains unstable and is not recommended for production use. The planned feature set and implementation order are maintained in [`todo.md`](todo.md), and all production code follows [`syntax.md`](syntax.md).

## Project guidance

- [`docs/architecture.md`](docs/architecture.md) defines component boundaries, security boundaries, and explicit non-goals.
- [`docs/supported-standards.md`](docs/supported-standards.md) records the standards baselines, review dates, and conformance-claim policy.
- [`conformance-report.json`](conformance-report.json) publishes exact fixture pass/fail/skip counts; [`docs/conformance.md`](docs/conformance.md) explains their scope and verification.
- [`tools/FuzzRunner`](tools/FuzzRunner) provides deterministic, budgeted grammar fuzzing and minimized replay artifacts for HTML, selectors, CSS, and URLs.
- [`docs/diagnostics.md`](docs/diagnostics.md) defines stable diagnostic identity, severity, source spans, and bounded collection.
- [`CONTRIBUTING.md`](CONTRIBUTING.md) provides the development workflow and the complete production/test syntax checklist.

## Install and quick start

UniversalHtml targets .NET 10. Add the package when consuming a published
build, or reference `src/html.csproj` while working from this repository:

```shell
dotnet add package UniversalHtml --prerelease
```

The package and assembly are named `UniversalHtml`; the current alpha C#
namespace is `html`:

```csharp
using html;

HtmlApi api = new HtmlApi();
HtmlSerializerApi serializer = new HtmlSerializerApi();
HtmlDocument document = api.ParseString(
    "<!doctype html><title>Tea</title><main><h1>Green tea</h1><p>Fresh &amp; bright.</p></main>");

HtmlNode heading = document.QuerySelector("main > h1");
string title = document.GetTitle();
string text = heading == null ? "" : heading.GetTextContent();
string correctedHtml = serializer.SerializeNode(document.GetRoot());
```

`HtmlApi.ParseString(string)` is the shortest document-parsing entry point. Use
the options operation whenever diagnostics, completion, limits, source tracking, or
byte-decoding metadata matter:

```csharp
using System.Threading;
using html;

HtmlFactory factory = new HtmlFactory();
HtmlApi api = new HtmlApi();
HtmlParseOptions options = new HtmlParseOptions();
options.SetBaseUrl("https://example.test/articles/");
options.SetLimits(factory.CreateUntrustedLimits());

HtmlParseResult parsed = api.ParseStringWithOptions(
    "<article><a href=tea>Tea</a></article>",
    options,
    CancellationToken.None);

if (!parsed.IsSuccess() || !parsed.IsComplete())
{
    throw new InvalidOperationException("HTML parsing did not complete.");
}

HtmlDocument document = parsed.GetDocument();
HtmlNode link = document.QuerySelector("article a");
string absoluteUrl = link.GetAbsoluteUrl("href").GetUrl();
```

Malformed HTML normally produces a browser-corrected tree plus diagnostics; it
is not, by itself, an exceptional condition. Always inspect the result status
before using bounded or security-sensitive output.

## Common workflows

### Parse fragments in their real context

HTML fragments are context-sensitive. Table cells, options, raw-text content,
SVG, MathML, and templates can produce different trees from the same source.
The string shortcut creates an HTML context element by tag name:

```csharp
HtmlParseResult parsed = new HtmlApi().ParseFragment(
    "<tr><td>Tea</td></tr>",
    "table");

HtmlNode fragment = parsed.GetFragment();
string fragmentHtml = new HtmlSerializerApi().SerializeChildren(fragment);
```

Use `HtmlFactory.CreateFragmentContext(element)` when the context must carry a
namespace, template-content relationship, or form ancestry:

```csharp
HtmlApi api = new HtmlApi();
HtmlFactory factory = new HtmlFactory();
HtmlDocument owner = api.ParseString("");
HtmlNode contextElement = owner.CreateElement(
    "http://www.w3.org/2000/svg",
    "svg");
HtmlFragmentContext context = factory.CreateFragmentContext(contextElement);

HtmlParseResult parsed = api.ParseFragmentWithOptions(
    "<circle cx=10 cy=10 r=5 />",
    context,
    new HtmlParseOptions(),
    CancellationToken.None);
```

The returned `HtmlParseResult` has a fragment and no document result.

### Traverse, query, and mutate the DOM

`HtmlDocument` owns the tree and creates nodes. Child, sibling, ancestor,
descendant, attribute, and query methods return snapshots rather than live
collections. A node can have only one parent; insertion reparents it, rejects
cycles, and enforces document-child invariants.

```csharp
HtmlDocument document = new HtmlApi().ParseString(
    "<main><p class=lead>Hello</p><ul><li>Tea</li></ul></main>");
HtmlNode main = document.QuerySelector("main");
HtmlNode lead = main.QuerySelector("p.lead");

lead.SetAttribute("data-state", "ready");
lead.AddClass("featured");
lead.SetTextContent("Welcome");

HtmlNode item = document.CreateElement("li");
item.Append(document.CreateText("Coffee"));
main.QuerySelector("ul").Append(item);

HtmlNode[] listItems = main.QuerySelectorAll("li");
string innerHtml = main.GetInnerHtml();
```

Convenience selector methods throw `ArgumentException` for an invalid or
unsupported selector. Compile caller-supplied selectors explicitly when the
failure must remain data:

```csharp
HtmlSelectorCompileResult compiled = new HtmlSelectorsApi().Compile(
    "article:has(> h2) a[href]");

if (compiled.IsSuccess())
{
    HtmlNode[] matches = compiled.GetSelector().FindAll(document.GetRoot());
}
else
{
    HtmlSelectorCompileError[] errors = compiled.GetErrors();
}
```

Compiled selectors, compiled conversion options, and compiled sanitizer
policies can be reused. Freeze a completed document with `Freeze()` before
sharing it for concurrent reads. Mutable documents are single-writer objects.

### Serialize corrected or mutated trees

`HtmlSerializerApi.SerializeNode` writes a node including its wrapper;
`SerializeChildren` writes only its children. Presets cover default, canonical,
source-case-preserving, compact, and pretty output.

```csharp
HtmlSerializerOptions output = new HtmlFactory().CreatePrettySerializerOptions();
output.SetIndentation("  ");
output.SetNewLine("\n");
output.SetSortAttributes(true);

string html = new HtmlSerializerApi().SerializeNodeWithOptions(
    document.GetRoot(),
    output,
    CancellationToken.None);
```

String serialization throws `HtmlSerializationLimitException` rather than
returning an unsafe partial string. The `TextWriter` overload may already have
received a safe prefix when the exception is thrown, so discard that destination
when a complete document is required.

### Sanitize untrusted HTML

Choose a preset, customize its mutable builder, compile it once, and reuse the
compiled policy. `TextOnly`, `BasicFormatting`, `RichText`, and `HtmlOnly` are
conservative starting points; SVG, MathML, inline styles, and custom elements
remain disabled unless explicitly enabled.

```csharp
HtmlFactory factory = new HtmlFactory();
HtmlSanitizerPolicy builder = factory.CreateRichTextSanitizerPolicy();
builder.AllowUrlScheme("https");
builder.AllowUrlHost("cdn.example.test");
builder.SetAllowRelativeUrls(false);
builder.ForceAttributeTokens(
    "a",
    "rel",
    new string[] { "noopener", "noreferrer" });

HtmlCompiledSanitizerPolicy policy = builder.Compile();
HtmlSanitizeResult sanitized = new HtmlSanitizerApi().SanitizeHtml(
    "<p>Hello <a href='https://cdn.example.test/tea'>tea</a></p>",
    policy,
    new HtmlParseOptions(),
    CancellationToken.None);

if (!sanitized.IsSuccess())
{
    throw new InvalidOperationException("Sanitization did not complete safely.");
}

string safeHtml = sanitized.GetHtml();
HtmlSanitizerChange[] changes = sanitized.GetReport().GetChanges();
```

`HtmlSanitizerApi.SanitizeNode` deep-clones by default. Use
`SanitizeInPlace` only when the caller intentionally gives the sanitizer
ownership of a mutable tree. Reports are bounded audit data, not the success
decision. Read the full [sanitizer security contract](#sanitizer-security-contract)
before rendering output, and do not mutate or reinterpret sanitized HTML after
the operation.

### Convert HTML to plain text

Plain-text conversion collapses ordinary HTML whitespace, preserves semantic
block boundaries, and excludes `head`, script, style, template, hidden, and
inert content by default. It supports lists, links, images, form controls,
preformatted text, tables, wrapping, selector rules, and explicit output limits.

```csharp
HtmlTextOptions textOptions = new HtmlTextOptions();
textOptions.SetLinkStyle(HtmlTextLinkStyle.TextAndUrl);
textOptions.SetImageStyle(HtmlTextImageStyle.AltText);
textOptions.SetTableStyle(HtmlTextTableStyle.Tsv);
textOptions.SetWrapColumn(80);

HtmlTextResult text = new HtmlTextConverterApi().ConvertDocumentWithOptions(
    document,
    textOptions,
    CancellationToken.None);

if (!text.IsSuccess())
{
    throw new InvalidOperationException("Text conversion failed.");
}

string value = text.GetText();
```

`IsSuccess()` means the requested conversion ran successfully.
`IsComplete()` distinguishes complete output from a safely terminated result,
and `WasTruncated()` identifies configured limit termination. The truncation
marker, wrapping column, line ending, blank-line policy, link/image/table style,
form-control inclusion, and selector-based rules are all explicit options.

### Convert HTML to Markdown

The Markdown presets are `CreateCommonMark()`, `CreateGitHubFlavored()`, and
`CreateReadable()`. GFM tables, task lists, strikethrough, autolinks, definition
lists, footnotes, subscript, superscript, and trusted raw HTML are controlled
independently. Raw HTML is disabled unless explicitly trusted.

```csharp
HtmlMarkdownOptions markdownOptions =
    new HtmlFactory().CreateGitHubMarkdownOptions();
markdownOptions.SetLinkStyle(HtmlMarkdownLinkStyle.Reference);
markdownOptions.SetEnableTrustedRawHtml(false);

HtmlMarkdownCompileResult compilation =
    markdownOptions.CompileWithDiagnostics();
if (!compilation.IsSuccess())
{
    throw new InvalidOperationException("Markdown options are invalid.");
}

HtmlMarkdownResult markdown = new HtmlMarkdownConverterApi().ConvertDocumentWithOptions(
    document,
    compilation.GetOptions(),
    CancellationToken.None);

string value = markdown.GetMarkdown();
```

The Markdown result has the same success, completeness, truncation, and bounded
diagnostic contract as plain text. Generated Markdown is a conversion, not a
sanitizer. If another renderer will turn it into HTML, apply that renderer's
security controls and sanitize the final HTML for its actual render context.

### Extract metadata and readable content

Metadata extraction returns normalized winners and bounded provenance candidates.
Article extraction alone returns an unsanitized cloned content tree; use the
combined pipeline when renderable article HTML is required.

```csharp
HtmlDocument document = new HtmlApi().ParseString(sourceHtml);
HtmlMetadata metadata = new HtmlMetadataExtractorApi().Extract(document);

HtmlArticleResult extraction = new HtmlArticleExtractorApi().ExtractWithCancellation(
    document,
    new HtmlArticleOptions(),
    CancellationToken.None);

if (extraction.IsSuccess())
{
    HtmlArticle article = extraction.GetArticle();
    string title = article.GetTitle();
    HtmlNode untrustedContent = article.GetContent();
}
```

### Run the combined pipeline

The pipeline presets share one resource budget across decoding, parsing,
metadata/article extraction, sanitization, serialization, plain text, and
Markdown. Available presets are untrusted rich text, article, search index, and
LLM ingestion.

```csharp
HtmlFactory factory = new HtmlFactory();
HtmlProcessingOptions processing = factory.CreateArticleProcessingOptions();
processing.SetSanitizerPolicy(
    factory.CreateRichTextSanitizerPolicy().Compile());

HtmlProcessingResult result = new HtmlApi().ProcessString(
    sourceHtml,
    processing,
    CancellationToken.None);

if (!result.IsSuccess() || !result.IsComplete())
{
    HtmlDiagnostic[] diagnostics = result.GetDiagnostics();
    throw new InvalidOperationException("The HTML pipeline did not complete.");
}

string safeArticleHtml = result.GetHtml();
string articleText = result.GetText();
string articleMarkdown = result.GetMarkdown();
HtmlProcessingStageTiming[] timings = result.GetStageTimings();
```

An output getter returns `null` when its stage was not requested or did not
produce that output. Inspect `GetStageStatus(stage)` and the per-stage
diagnostics when handling optional or stopped stages. The pipeline refuses a
configuration that requests renderable HTML while sanitization is disabled.

## Low-level tokenizer and parser

Use the tokenizer when the application needs lexical tokens without a DOM. Feed
owned chunks in order; the span is consumed during `Feed` and is not retained.
After the final chunk, call `Read` until it reports `Complete` or
`LimitExceeded`. Each result also carries diagnostics emitted since the prior
read.

```csharp
HtmlTokenizerOptions tokenizerOptions =
    new HtmlFactory().CreateUntrustedTokenizerOptions();
HtmlTokenizer tokenizer = new HtmlTokenizer(tokenizerOptions);
tokenizer.Feed("<p>Hello &amp; goodbye</p>".AsSpan(), true);

bool reading = true;
while (reading)
{
    HtmlTokenReadResult read = tokenizer.Read();
    HtmlTokenReadStatus status = read.GetStatus();

    if (status == HtmlTokenReadStatus.TokenAvailable)
    {
        HtmlToken token = read.GetToken();
        HtmlTokenKind kind = token.GetKind();
        string name = token.HasName() ? token.GetName() : "";
    }
    else if (status == HtmlTokenReadStatus.Complete)
    {
        reading = false;
    }
    else if (status == HtmlTokenReadStatus.LimitExceeded)
    {
        throw new InvalidOperationException("Tokenization limit exceeded.");
    }
    else
    {
        throw new InvalidOperationException("The tokenizer needs more input.");
    }
}
```

For truly incremental input, call `Feed(chunk, false)`, drain available tokens
until `NeedMoreInput`, then feed the next chunk. Mark exactly one final chunk
with `isFinal: true`. `Reset()` reuses the tokenizer with its copied options.
Set `SetCdataAllowed(true)` only when a surrounding foreign-content tree builder
has established that CDATA is valid at the current adjusted node.

`HtmlParser` incrementally coordinates the tokenizer and browser-style tree
construction:

```csharp
HtmlParser parser = new HtmlParser(new HtmlParseOptions());
parser.Feed("<table><tr>".AsSpan(), false);

HtmlParseProgress progress = parser.Read();
while (progress.GetStatus() == HtmlParseProgressStatus.TokenProcessed)
{
    progress = parser.Read();
}

parser.Feed("<td>Tea".AsSpan(), true);
HtmlParseResult result = parser.Complete();
HtmlDocument document = result.GetDocument();
```

`Read()` processes at most one tokenizer output and reports cumulative processed
token count. `Complete()` supplies an empty final chunk when needed and drains
all remaining work. `Reset()` starts a new document parse with the originally
copied settings. For fragment parsing, use the `HtmlApi.ParseFragment` facade so
the context-specific tokenizer state and tree-builder stacks are initialized
correctly.

## Public API reference

The SDK build emits XML API documentation as `UniversalHtml.xml` beside the
assembly. That file contains the signature-level contract for every public
member. The catalog below maps the complete public surface by workflow; all of
these types currently live in the `html` namespace.

| Area | Entry points and public data/configuration types |
|---|---|
| One-call facade and factories | `HtmlApi`, `IHtmlApi`; `HtmlFactory`, `IHtmlFactory`; `HtmlParseOptions`, `HtmlFragmentContext`, `HtmlParseResult` |
| Limits, sources, diagnostics | `HtmlLimits`; `HtmlSourcePosition`, `HtmlSourceSpan`; `HtmlDiagnostic`, `HtmlDiagnosticCode`, `HtmlDiagnosticSeverity` |
| Decoding and entities | `HtmlEncodingDetectorApi`, `HtmlEncodingDetectionResult`, `HtmlEncodingConfidence`, `HtmlEncodingSource`; `HtmlByteDecoderApi`, `HtmlDecodedInput`, `HtmlDecodingError`; `HtmlEntitiesApi`, `HtmlEntityResult`, `HtmlEntityContext`, `HtmlEntityEncoding` |
| Tokenization and parsing | `HtmlTokenizer`, `HtmlTokenizerOptions`, `HtmlTokenReadResult`, `HtmlTokenReadStatus`, `HtmlToken`, `HtmlTokenKind`; `HtmlParser`, `HtmlParseProgress`, `HtmlParseProgressStatus` |
| DOM and traversal | `HtmlDocument`, `HtmlNode`, `HtmlNodeKind`, `HtmlDocumentMode`, `HtmlAttribute`; `HtmlTreeWalkResult`, `HtmlTreeWalkStatus`, `HtmlTreeVisitPhase` |
| Selectors | `HtmlSelectorsApi`, `HtmlSelector`, `HtmlSelectorCompileResult`, `HtmlSelectorCompileError`, `HtmlSelectorCompileErrorKind` |
| URLs | `HtmlUrlsApi`, `HtmlUrlSchemeResult`, `HtmlUrlResult`; `HtmlDomUrlsApi`, `HtmlUrlAttributeKind` |
| HTML microsyntaxes | `HtmlTokenListsApi`; `HtmlSrcsetApi`, `HtmlSrcsetResult`, `HtmlSrcsetCandidate`; `HtmlRefreshApi`, `HtmlRefreshResult`; `HtmlNumbersApi`, `HtmlIntegerResult`, `HtmlDimensionResult`, `HtmlDimensionUnit`, `HtmlOrderedListMarkerStyle` |
| Serialization | `HtmlSerializerApi`, `HtmlSerializerOptions`, `HtmlSerializerNameMode`, `HtmlSerializerQuoteMode`, `HtmlSerializationLimitException` |
| Sanitization | `HtmlSanitizerApi`, `HtmlSanitizeResult`; `HtmlSanitizerPolicy`, `HtmlCompiledSanitizerPolicy`, `HtmlSanitizerUrlPolicy`, `HtmlSanitizerUrlAttributeRule`; `HtmlSanitizerReport`, `HtmlSanitizerChange`, `HtmlSanitizerChangeKind`, `HtmlSanitizerChangeReason`; `HtmlBlockedElementAction`, `HtmlSanitizerAttributeAction`, `HtmlSanitizerAttributeNormalization`, `HtmlSanitizerElementName`, `HtmlSanitizerBlockedElementRule`, `HtmlSanitizerElementAttributeRule`, `HtmlSanitizerForcedAttributeRule`, `HtmlSanitizerAttributeRule`, `HtmlSanitizerAttributeValueRule`, `HtmlSanitizerAttributeTokenRule` |
| Plain text | `HtmlTextConverterApi`, `HtmlTextOptions`, `HtmlCompiledTextOptions`, `HtmlTextResult`; `HtmlTextRule`, `HtmlCompiledTextRule`, `HtmlTextRuleAction`, `HtmlTextLinkStyle`, `HtmlTextImageStyle`, `HtmlTextTableStyle` |
| Markdown | `HtmlMarkdownConverterApi`, `HtmlMarkdownOptions`, `HtmlCompiledMarkdownOptions`, `HtmlMarkdownCompileResult`, `HtmlMarkdownResult`; `HtmlMarkdownRule`, `HtmlCompiledMarkdownRule`, `HtmlMarkdownRuleAction`, `HtmlMarkdownHeadingStyle`, `HtmlMarkdownLinkStyle`, `HtmlMarkdownUnknownElementAction`, `HtmlMarkdownOrderedListNumbering` |
| Metadata and readability | `HtmlMetadataExtractorApi`, `HtmlMetadata`, `HtmlMetadataCandidate`, `HtmlMetadataField`, `HtmlMetadataSource`; `HtmlArticleExtractorApi`, `HtmlArticleOptions`, `HtmlArticleResult`, `HtmlArticle` |
| Combined pipeline | `HtmlProcessorApi`, `HtmlProcessingOptions`, `HtmlProcessingResult`, `HtmlProcessingStage`, `HtmlProcessingStageStatus`, `HtmlProcessingStageTiming` |

The main ownership and failure conventions are:

- Mutable option and policy getters return independent copies. Call the
  corresponding setter to apply a changed copy.
- `Compile()` validates and snapshots reusable configuration. Later builder
  changes do not modify the compiled object.
- Arrays are snapshots owned by the caller and are empty rather than `null`.
- Reference getters return `null` only where their XML documentation states the
  absence condition. See [null-return documentation](#null-return-documentation-convention).
- Results expose success, completeness, truncation, diagnostics, and dropped
  diagnostic counts where applicable. Never infer success from nonempty output.
- Full operations copy settings and accept `CancellationToken` last. Cancellation
  throws `OperationCanceledException`; ordinary malformed HTML is diagnostic
  data, while programmer errors use argument or invariant exceptions.
- `HtmlNode.Clone(deep)` preserves the source owner document;
  `HtmlDocument.Import(node, deep)` creates a node owned by the target document.
  Mutation invalidates source spans on changed values.
- Convenience query methods compile selector text on each call. Use
  `HtmlSelectorsApi.Compile` for reuse or recoverable selector errors.

## Migration and compatibility notes

The `0.1.0-alpha.1` API is an evaluation surface, not a stable compatibility
promise. Pin the exact prerelease package version, review the release notes on
every update, and keep golden tests for serialized HTML, sanitizer output, text,
and Markdown that your application treats as a contract. Before moving to a new
version, recompile saved selector, sanitizer, text, Markdown, and pipeline
configurations instead of persisting their internal representation.

The current package/assembly name is `UniversalHtml`, while the current alpha
C# namespace is `html`. Treat a future namespace alignment as a source-breaking
alpha migration and avoid namespace aliases in public signatures if that would
make the transition harder.

For callers moving from common HTML libraries, the closest workflow mappings
are:

| Existing workflow | UniversalHtml workflow |
|---|---|
| Load or parse a document | `HtmlApi.ParseString` or `HtmlApi.ParseStringWithOptions` |
| Parse an HTML fragment | `HtmlApi.ParseFragment` with the actual insertion context |
| CSS query | `QuerySelector`, `QuerySelectorAll`, or reusable `HtmlSelector` |
| XPath query | No XPath layer; use supported CSS selectors or bounded traversal helpers |
| Read/write DOM properties | `Get...`, `Set...`, and explicit mutation methods on `HtmlDocument` and `HtmlNode` |
| Enumerate live node collections | Snapshot arrays such as `GetChildren()` and `QuerySelectorAll()` |
| Read/write `InnerHtml` | `GetInnerHtml()` for reading; build/parse nodes and use `Empty`, `Append`, or related mutation methods for writing |
| Clean with a safelist | Build `HtmlSanitizerPolicy`, call `Compile()`, then run the complete `HtmlSanitizerApi` operation |
| Convert to text or Markdown | `HtmlTextConverterApi` or `HtmlMarkdownConverterApi` with compiled reusable options |
| Fetch and parse a URL | Fetch outside this library, then pass the trusted response URL/base and bytes or stream to parsing |

UniversalHtml deliberately has no HTTP client, XPath, XML/XHTML parser, script
execution, layout, browser events, or interchangeable parser backends. Do not
port code that relied on those behaviors by silently approximating them.

## Namespace and package identity

- The NuGet package ID, assembly name, product name, and project root namespace are `UniversalHtml`.
- Every current production type belongs directly to the file-scoped `html` C# namespace. The library does not use public subnamespaces. Namespace alignment remains an alpha migration concern described above.
- Public type names begin with `Html` or `IHtml`. Initialisms use .NET casing within identifiers: `Html`, `Css`, `Json`, `Url`, `Uri`, and `Utf`.

## Public API naming conventions

The restricted production syntax intentionally exposes behavior through methods rather than properties, indexers, events, or library-defined generic types. Narrow interfaces describe incremental parsing and tokenization and the compile boundary shared by mutable and immutable text and Markdown options. Concrete classes remain the construction and result types.

Every production type, constructor, method, and field has explicit `public` or `internal` visibility. Public classes, interfaces, constructors, methods, fields, enums, and enum members are the supported package API and carry XML documentation; implementation-only types and members are `internal` and are not part of that API. Public documentation uses only the XML elements permitted by [`syntax.md`](syntax.md) and describes mechanics, ownership, limits, null behavior, and security considerations in plain English.

Result, diagnostic, exception, and inspection-only rule types and reusable
compiled configurations are public only when callers need to inspect, catch,
or pass them as part of a supported workflow. Their constructors and factory
helpers remain `internal` when instances must come from a parser, converter,
detector, serializer, sanitizer, or compile operation. Intermediate compiled
snapshots used only between library stages remain `internal`. Option, policy,
fragment-context, DOM value, and other caller-authored inputs expose public
construction methods only where direct construction is part of the supported
workflow. When named factories cover every supported construction state, those
factories are the public entry points and the duplicate constructor remains
`internal`. Publicly immutable rule and value objects do not expose defensive
`Copy()` helpers; mutable options and policy builders do.

Sanitizer stage helpers for individual element, attribute, URL, inline-style, nested-content, template-marker, and DOM-clobbering decisions are implementation details. Callers use `HtmlSanitizerApi` or the `HtmlApi` facade with a compiled policy; stage helpers remain `internal` so partial sanitization cannot be mistaken for the complete security operation.

- Types, methods, and enum members use `PascalCase`; parameters and local variables use `camelCase`.
- State accessors use `Get...`; boolean observations use `Is...`, `Has...`, `Can...`, or past-tense forms such as `WasTruncated`.
- Mutations use `Set...`, `Add...`, `Remove...`, `Insert...`, `Replace...`, or a specific tree verb such as `Append`, `Prepend`, `Wrap`, or `Unwrap`.
- Factory methods use `Create...`; copying operations use `Copy` or `Clone`; conversions use `To...` for convenience methods and `Convert` on dedicated converter classes.
- A `Try...` method returns a concrete result object with explicit success and value methods. It never uses `ref`, `out`, tuples, or exceptions for an expected negative result.
- Collection-returning methods return concrete snapshot arrays. They return an empty array when there are no values and never return `null`.
- Full parsing, serialization, sanitization, and conversion overloads place `CancellationToken` last. Short convenience overloads delegate to safe documented defaults.
- Exceptions represent null arguments, programmer errors, violated tree invariants, or unrecoverable resource exhaustion. Malformed HTML and other expected input failures use result and diagnostic objects.

## Null-return documentation convention

Nullable reference annotations are enabled for the library. Every public method that can return `null` uses a nullable reference return type and includes the exact sentence `Returns null when ...` in its API documentation, completing the condition precisely. A return is non-null unless both its signature and documentation describe the nullable result.

Snapshot arrays and result diagnostic arrays never return `null`. A possibly absent string, node, encoding, or other reference may return `null` only when absence is the natural answer. When `null` could be confused with failure, the API also provides a concrete `Try...` result object whose methods distinguish success, absence, truncation, and diagnostics. Public methods reject a `null` argument with `ArgumentNullException` unless the argument documentation explicitly permits it.

`HtmlNode.GetOwnerDocument()`: Returns null when the node is the document root.
`HtmlNode.GetParent()`: Returns null when the node has no parent.
`HtmlNode.GetFirstChild()` and `GetLastChild()`: Returns null when the node has no
children. `HtmlNode.GetPreviousSibling()` and `GetNextSibling()`: Returns null
when the requested sibling does not exist. `HtmlNode.GetPreviousElementSibling()`
and `GetNextElementSibling()`: Returns null when the requested element sibling
does not exist. `HtmlNode.GetTemplateContent()`: Returns null when the node is
not an HTML template element.
`HtmlDocument.GetDocumentElement()`: Returns null when the document has no
element child. `HtmlDocument.GetHead()` and `GetBody()`: Returns null when the
corresponding HTML element is absent. `HtmlDocument.GetBaseElement()`: Returns
null when the document has no HTML `base` element with an `href` attribute.
`HtmlDomUrls.GetBaseElement(...)`: Returns null under the same condition.
`HtmlNode.GetAttribute(...)`: Returns null
when the requested attribute is absent. Other DOM string and snapshot-array
getters return empty values rather than `null`.

## Versioning policy

UniversalHtml follows Semantic Versioning for NuGet package versions.

- During `0.y.z` development, the API is unstable. The minor component may contain breaking changes, while the patch component contains compatible corrections only.
- Starting with `1.0.0`, a major release contains incompatible public API or documented contract changes, a minor release adds backward-compatible behavior, and a patch release contains backward-compatible fixes.
- Pre-release labels such as `alpha`, `beta`, and `rc` identify increasing stability. A pre-release version provides compatibility only within that exact version.
- Public compatibility includes callable signatures, documented null and exception behavior, diagnostic codes, deterministic serialization/conversion contracts, and sanitizer preset guarantees. Security fixes may tighten sanitizer output in a patch release; such changes are documented but are not treated as compatibility breaks.
- `AssemblyVersion` uses the package version's major and minor components with build and revision set to zero. `FileVersion` uses the numeric package core, and the informational version carries the full package version, including any pre-release label.

The changelog will identify breaking changes, security-relevant behavior changes, deprecations, and migrations before the first stable release.

## Sanitizer security contract

The sanitizer APIs and presets are implemented and covered by the current adversarial fixtures, but they remain under development and have not completed independent security review. No API in this alpha release should therefore be treated as production-ready protection for untrusted markup. Parsing, DOM mutation, querying, serialization, and extraction are not sanitization.

The sanitizer's security guarantee is defined for one complete operation with four fixed inputs: attacker-controlled HTML or DOM content, a trusted compiled policy, a trusted parse/render context, and a trusted base URL when relative URLs are permitted. A successful result is intended to prevent constructs blocked by that policy from surviving browser reparsing when the result is inserted unmodified into the same supported HTML context. The guarantee does not claim that allowed content is truthful, accessible, private, visually harmless, free of tracking, or unable to request an allowed external resource.

The security invariant is about the returned result, not the input spelling or
the number of reported changes. For a successful operation, every retained
node, attribute, URL, inline-style declaration, namespace, and nested-markup
value has passed the same compiled policy and the result has passed canonical
serialization checks. String-input sanitization additionally repeats canonical
serialization, HTML fragment reparsing, and policy application until the output
is stable. A failure, cancellation, or security-relevant limit produces no
renderable output. This guarantee applies only to behavior implemented and
allowed by that exact policy version; it is not a proof that a customized
policy is appropriate for an application's threat model.

### Threat model and non-goals

Assume an attacker controls every byte or character of the input, including malformed tags, character references, names, attribute values, URLs, inline CSS, comments, template contents, foreign SVG/MathML content, duplicate or reordered data, and inputs designed to exhaust time, memory, diagnostics, or output budgets. Sanitization must account for browser error recovery, mutation-XSS reparsing, namespace confusion, tag poisoning, raw-text boundaries, encoded or control-obfuscated protocols, DOM-clobbering names, nested markup such as explicitly allowed `srcdoc`, and dangerous CSS or URL values.

The attacker may know the complete policy, make repeated requests, compare
outputs, and choose content that is later copied between documents. The
sanitizer does not depend on policy secrecy. It treats URL navigation and
resource loading, CSS values, identifier/name collisions, customized elements,
template content, and foreign namespaces as separate capabilities instead of
assuming that an allowed element or attribute name makes its value safe.

The trusted application controls the policy and every extension, callback, forced value, URL base, fragment context, and rendering destination. A policy that deliberately allows active content, an unsafe scheme, unrestricted CSS, foreign content, custom elements, or attacker-controlled policy configuration has widened or removed the guarantee. The sanitizer does not defend against vulnerabilities in trusted application code or the browser, compromised dependencies outside this package, unsafe HTTP headers, or later interpretation by a server-side or client-side template language. Neutralization of markers such as `{{...}}`, `${...}`, and `<%...%>` is an explicit opt-in feature, not a default promise.

A Content Security Policy, sandboxed frame, Trusted Types policy, or similar browser control is useful defense in depth but does not replace sanitization. Conversely, sanitization does not configure those controls.

### Trust boundaries and parsing assumptions

Untrusted source first crosses the decoding and HTML parsing boundary. The resulting browser-corrected DOM remains untrusted: parsing determines what the markup means but does not determine whether it is safe. Sanitization evaluates the corrected DOM rather than scanning source text, so entity decoding, tokenizer recovery, implicit elements, namespace switching, foster parenting, and template-content construction occur before policy decisions.

Document input must be parsed as an HTML document. Fragment input must be parsed with the same HTML, SVG, MathML, template, table, select, raw-text, or other context that will govern its eventual insertion. Choosing a generic `div` context for markup that will later be inserted into a different parsing context invalidates the guarantee. XML, XHTML, SVG-as-XML, and other non-HTML parsers are outside the supported boundary.

Compiled sanitizer policies, configured base URLs, and host-supplied forced values are trusted inputs and must not be derived from the attacker unless the application validates them independently. Sanitizer reports and diagnostics describe decisions but do not make security decisions; their text and retained source values remain untrusted data and must be escaped before display. Report truncation never widens the policy or changes whether a construct is kept.

### Supported rendering context

Sanitized HTML is supported only as markup for an HTML document or the matching HTML element-content fragment in a browser `text/html` context. It is not an escaping function for an HTML attribute, URL, JavaScript, JSON, CSS, raw-text element, comment, XML/XHTML document, SVG/XML document, or template-language source. Place output in those contexts only through a separate encoder or validator designed for that exact grammar.

The parse context, base URL semantics, and render context are part of the security decision. If relative URLs are allowed, applications must keep the trusted base stable or convert accepted URLs to an approved absolute form. Explicitly allowed nested-markup containers such as `srcdoc` require recursive sanitization for their own context. Inserting a sanitized fragment into a different context, wrapping it in attacker-controlled markup, or allowing a surrounding document to reinterpret its URLs is unsupported.

| Destination | Supported use |
|---|---|
| HTML document parsed and rendered as `text/html` | Supported when document input was sanitized as a document and output is inserted unchanged. |
| HTML element content | Supported when the fragment was parsed and sanitized for that exact element/namespace context and is inserted unchanged. |
| DOM insertion without converting back to source | Supported for the returned sanitized clone when it remains in the same owner, namespace, base-URL, and element context. |
| HTML attribute, comment, element name, or raw-text element body | Unsupported; use a context-specific encoder and do not treat sanitizer output as a scalar value. |
| JavaScript, JSON, CSS, URL, SQL, shell, or template-language source | Unsupported; HTML sanitization is not encoding or validation for another grammar. |
| XML, XHTML, SVG/XML, MathML/XML, email MIME source, or non-browser HTML consumer | Unsupported unless that consumer is separately analyzed and the content is processed for its grammar and behavior. |
| Markdown or rich-text editor input | Unsupported as a terminal guarantee because a later renderer/editor may reinterpret or introduce markup; sanitize the final HTML afterward. |

HTML email clients and embedded web views vary in supported markup and security
behavior. A preset can be a useful allowlist input to such a pipeline, but this
library's guarantee remains limited to the browser `text/html` contexts above.

### Safe-use rules

Before rendering attacker-controlled content:

1. Select the least-capable built-in preset that meets the product need. Treat
   every added element, attribute, scheme, host, CSS property, namespace,
   custom element, and nested-markup feature as a security review item.
2. Keep the mutable policy builder, forced values, URL allowlists, base URL,
   parse options, fragment context, and limits under trusted application
   control. Compile the policy once and reuse that immutable snapshot.
3. Parse document input as a document. For fragments, use the actual eventual
   insertion element and namespace rather than a convenient generic context.
4. Prefer absolute allowed URLs or set a trusted base and enable explicit
   rewriting. Do not let an attacker change the base between sanitization and
   rendering. Restrict hosts and ports when external navigation or loading is
   not intentionally open-ended.
5. Leave URL credentials, protocol-relative URLs, `data:` URLs, inline CSS,
   foreign namespaces, templates, custom elements, customized built-ins,
   comments, doctypes, and `data-*`/`aria-*` attributes disabled unless their
   complete downstream behavior has been reviewed. An empty host allowlist in
   a URL policy means hosts are unrestricted after the scheme check; it does
   not mean that no hosts are allowed.
6. Use `HtmlSanitizerApi.SanitizeHtml` or a sanitizing `HtmlApi.ProcessString` preset as the
   complete operation. Do not call parsing, serialization, URL normalization,
   CSS tokenization, report inspection, or article extraction as a substitute.
7. Accept output only when `HtmlSanitizeResult.IsSuccess()` is `true`. For a
   combined pipeline, also require `HtmlProcessingResult.IsSuccess()` and the
   sanitizer stage status to indicate completion. Never render a partial tree,
   partial writer buffer, diagnostic excerpt, or failed result.
8. Insert the returned HTML or DOM unchanged into the reviewed destination.
   Apply CSP, sandboxing, Trusted Types, safe cookies, and safe response headers
   as independent defense-in-depth controls where appropriate.
9. Escape diagnostics, report names, metadata, URLs, and all other extracted
   strings for the context in which the application displays them. They remain
   attacker-influenced even when the sanitized HTML itself succeeded.
10. Re-sanitize after any operation that can change markup, namespaces,
    attributes, URL meaning, or parsing context. Prefer making sanitization the
    final content transformation.

Store or log the package version and a stable identifier for the application
policy with sanitized records when reproducibility matters. Previously stored
sanitized HTML is not automatically re-evaluated after a preset or browser
behavior changes; applications should plan a re-sanitization migration for
stored content when security release notes require it.

### Built-in security presets

All preset factories return independent mutable builders. `Compile()` creates
the immutable policy used by an operation. The presets are ordered by content
capability, but they are not interchangeable claims of safety for every product:

| Preset | Intended retained content | Important defaults |
|---|---|---|
| `CreateTextOnly()` | Text content without retained markup | Default-deny elements and attributes; dangerous containers still follow their safe blocked action rather than exposing their source text as markup. |
| `CreateBasicFormatting()` | Conservative paragraphs, pre/code, and inline emphasis/annotation | No link, image, media, table, form, SVG, MathML, custom-element, template, comment, doctype, or inline-style capability. Only `dir`, `lang`, and `title` are globally retained. |
| `CreateRichText()` | Common article/rich-text structure, links, images, lists, and tables | Allows `http`, `https`, `mailto`, and `tel`; relative and fragment references are allowed, protocol-relative URLs and credentials are rejected, and hosts are unrestricted after scheme checks. Active content, forms, media players, inline styles, foreign namespaces, custom elements, templates, comments, and doctypes remain disabled. |
| `CreateHtmlOnly()` | The rich-text set plus selected ordinary HTML media/form-display elements | Still excludes SVG, MathML, scripts, executable content, forms that submit, inline styles, custom elements, templates, comments, and doctypes. It is broader than `RichText` and should not be chosen merely because input is called HTML. |

No preset enables `style` attributes merely because CSS properties are added;
the relevant attribute and property rules must both be deliberately configured.
No preset allows `<style>` or script execution. Allowing SVG or MathML requires
both foreign-namespace permission and appropriate namespace-aware element and
attribute rules. Allowing custom elements does not audit the JavaScript behavior
of their definitions; only use them when those definitions and upgrade timing
are trusted.

### Security preset versioning

The preset factory name identifies an evolving security profile, not a frozen
serialized allowlist. Within a fixed package version it is deterministic, and a
compiled policy is an immutable snapshot. Across versions:

- A patch release may remove or further restrict elements, attributes, URL/CSS
  values, namespace handling, or output when required to close a vulnerability.
  Such tightening is security-compatible even when output changes.
- Widening a stable preset with a new capability requires at least a minor
  release after `1.0`; during `0.y.z` development it may occur in a minor alpha
  release. Release notes must identify the added capability and migration risk.
- A vulnerability fix never silently preserves unsafe output for compatibility.
  Applications that require an exact allowlist should build and test an explicit
  policy, pin the package version, and still review security fixes rather than
  copying an old vulnerable preset indefinitely.
- Preset changes do not mutate already compiled policies in a running process
  and do not rewrite previously stored output. Restart/recompile policies and
  re-sanitize stored content when release guidance calls for it.
- Changes to the threat model, supported rendering contexts, fail-closed rules,
  or the meaning of a security guarantee are documented as security-relevant
  contract changes even when callable API signatures do not change.

Applications should regression-test both allowed and blocked examples for every
custom policy. Tests should assert the security property (for example, that no
unapproved URL survives), not only one incidental serialized spelling.

### Fail-closed limits and completion

Sanitization is successful only when parsing and every security-relevant sanitizer, URL, CSS, tree, and output stage completes within its configured limits. A parse failure, incomplete or truncated security decision, invariant violation, cancellation, or exceeded security-relevant limit produces no renderable sanitized HTML or DOM. Callers must check the explicit result status and must never recover a partial buffer or partially transformed tree as safe output.

Diagnostic and audit-report retention limits are informational exceptions to that rule: exhausting them may truncate the report, but processing continues under the same policy and the result records that reporting was truncated. Resource limits protect availability and fail closed when a complete security decision cannot be made; they are not substitutes for element, attribute, URL, CSS, and namespace policy.

### Unsafe post-processing warning

> The security guarantee ends at the returned sanitized result. Changing that DOM or HTML can make it unsafe. Treat sanitization as the last security-relevant transformation before insertion.

Do not concatenate untrusted markup, decode entities, perform string replacements, inject template output, add or rewrite attributes, move nodes into a different namespace or fragment context, change URL-base semantics, or pass the result through a parser, Markdown renderer, minifier, or editor that can introduce or reinterpret markup. Serializing an unchanged sanitized DOM with the library's deterministic HTML serializer is within the boundary; arbitrary rewriting is not. If downstream processing can change markup or policy-relevant values, run the sanitizer again after that processing with the final context and trusted policy.

This warning includes seemingly harmless operations such as linkification,
emoji replacement with markup, syntax highlighting, template interpolation,
DOM decoration, adding `target`, resolving URLs under a different base, or
moving sanitized nodes into SVG/MathML. If such a step is necessary, perform it
first and sanitize the resulting final HTML in its final context.

### Sanitizer audit reports are informational

Sanitizer change reports explain completed policy decisions; they do not make,
approve, veto, or widen those decisions and must not be used as a security
oracle. A report-retention limit can discard entries without changing the
sanitized tree. Total, dropped, and per-reason counts continue to increase when
retention is exhausted, and `WasTruncated()` records that the retained array is
only a prefix.

Reason enum values and `UHS` identifiers are stable. Source spans identify the
changed source construct when one is honestly available and are explicitly
unknown for synthesized or aggregate findings. Attacker-controlled values are
represented by a fixed redaction marker rather than copied into the report;
oversized or control-bearing names are omitted and marked as redacted. Report
strings and names are still untrusted display data and must be escaped for their
output context. Limit and cancellation flags describe operation status; callers
must use the sanitizer result status—not report contents—to decide whether any
output is renderable.

### Vulnerability disclosure policy

Security reports are welcome for sanitizer bypasses, browser-reparse
differences, unsafe preset defaults, URL/CSS obfuscation bypasses, namespace or
DOM-clobbering issues, fail-open limit behavior, sensitive-data exposure, and
other defects that cross a documented security boundary. General parser
conformance bugs without a security impact may use the normal issue tracker.

Do not publish a suspected vulnerability, exploit, malicious fixture, or
unpatched bypass in a public issue. Use the repository host's private
vulnerability-reporting or security-advisory channel. If no private reporting
channel is visible, contact the package/repository owner through the host or
package registry and request a private channel without including exploit
details in the first public message. Include the affected package version and
runtime, policy or preset, exact parse/render context, minimal reproducer,
observed browser behavior, impact, and any known workarounds. Remove real user
data, credentials, and unrelated secrets from the report.

The maintainer will acknowledge receipt when practicable, reproduce and triage
the report, coordinate a fix and regression case, and publish affected versions,
mitigation, and upgrade guidance after a release is available. Timelines depend
on severity and maintainer availability; reporters should not assume a response
deadline unless one is agreed privately. Coordinated disclosure and credit are
preferred, but credit may be declined. Good-faith testing must use systems and
data the reporter owns or is authorized to test, avoid privacy violations and
service disruption, and stop after establishing the minimum evidence needed.

Until the independent review gate is complete, releases must continue to state
that sanitizer protection is pre-release and not production-ready. Closing a
single report or passing the adversarial corpus does not remove that release
gate.

## Standards, supported subsets, and deviations

The versioned baselines below define the behavior reviewed for
`0.1.0-alpha.1`. A living-standard date is a repository review snapshot, not a
claim that the external standard stopped changing on that date. Generated data
also carries pinned revisions and SHA-256 checksums; normal builds never fetch
the standards or fixture sources.

| Area | Exact baseline used by this release | Supported scope |
|---|---|---|
| HTML parsing, serialization, and character references | WHATWG HTML Living Standard reviewed 2026-08-09; generated HTML names, doctype/foreign adjustments, and `entities.json` reviewed or snapshotted 2026-08-10 | Browser-style HTML tokenization and corrected document/fragment trees, HTML/SVG/MathML integration in `text/html`, deterministic HTML serialization, and the complete pinned named-reference table. |
| DOM | WHATWG DOM Living Standard reviewed 2026-08-09 | Server-side node ownership, traversal, text content, cloning, normalization, mutation validity, and static querying. |
| Input encodings | WHATWG Encoding Living Standard reviewed 2026-08-09; `indexes.json` snapshot dated 2026-05-21 and `encodings.json` reviewed 2026-08-10 | HTML labels, sniffing precedence, replacement behavior, and all canonical decoder families listed below. |
| URLs | WHATWG URL Living Standard reviewed 2026-08-09 | Lexical scheme deobfuscation and the URL discovery, relative resolution, normalization, and sanitizer-policy operations exposed by this library. |
| CSS selectors | Selectors Level 4 reviewed 2026-08-09 | The deterministic, tree- and markup-observable subset listed below. |
| Inline CSS sanitization | CSS Syntax Level 3 Candidate Recommendation Draft, 24 December 2021 | Recovery-oriented tokenization and value checking only for explicitly allowed inline declarations; no general stylesheet or CSSOM support. |
| CommonMark output | CommonMark 0.31.2 | HTML-to-Markdown output and escaping behavior for supported constructs; no Markdown parser. |
| GitHub Flavored Markdown output | GFM 0.29.0.gfm.13, reviewed 2026-08-09 | Optional table, task-item, strikethrough, autolink, and tag-filtering output behavior. |

The provenance files are
[`tools/GenerateTables/inputs/standards.json`](tools/GenerateTables/inputs/standards.json),
[`tools/GenerateTables/inputs/manifest.json`](tools/GenerateTables/inputs/manifest.json),
and [`tests/data/fixture-manifest.json`](tests/data/fixture-manifest.json).
See [`docs/supported-standards.md`](docs/supported-standards.md) for the baseline
update policy.

### Supported and unsupported encodings

Encoding labels are trimmed of HTML whitespace and matched ASCII-case
insensitively. The complete pinned WHATWG label table maps aliases to these 40
canonical decoder names:

```text
Big5, EUC-JP, EUC-KR, GBK, gb18030, IBM866, ISO-2022-JP,
ISO-8859-2, ISO-8859-3, ISO-8859-4, ISO-8859-5, ISO-8859-6,
ISO-8859-7, ISO-8859-8, ISO-8859-8-I, ISO-8859-10, ISO-8859-13,
ISO-8859-14, ISO-8859-15, ISO-8859-16, KOI8-R, KOI8-U, macintosh,
replacement, Shift_JIS, UTF-8, UTF-16LE, UTF-16BE, windows-874,
windows-1250, windows-1251, windows-1252, windows-1253, windows-1254,
windows-1255, windows-1256, windows-1257, windows-1258,
x-mac-cyrillic, x-user-defined
```

This includes the standard's single-byte families plus Big5, EUC-JP,
EUC-KR, GBK/gb18030, ISO-2022-JP, Shift_JIS, UTF-8, UTF-16, the
`replacement` decoder, and `x-user-defined`. Malformed sequences are replaced
with U+FFFD and reported with byte ranges. BOM, explicit transport label, meta
prescan, and default selection follow the documented HTML precedence, including
the standard's alias behavior: labels such as ASCII and ISO-8859-1 select
`windows-1252`, while ISO-2022-CN, ISO-2022-KR, and related unsafe labels select
the `replacement` decoder.

An unknown label is unsupported and produces a failed/unsupported decoded
result; it is never passed to an operating-system code-page registry. UTF-7,
UTF-32, CESU-8, BOCU-1, SCSU, arbitrary iconv names, and encodings absent from
the pinned WHATWG label table are not HTML-label options. The public stream
parse options can instead accept a trusted caller-supplied .NET `Encoding`;
that explicit override uses the supplied BCL decoder with replacement fallback
and is an application choice outside the HTML web-label conformance claim.

The library decodes input only. It does not provide a general legacy-encoding
encoder; HTML serialization produces .NET characters and can use ASCII-only
character references when requested. String and `TextReader` inputs are already
decoded and therefore perform newline/null preprocessing but no encoding
sniffing.

### Supported and unsupported selectors

Supported selector syntax is:

- Strict selector lists; forgiving nested lists for `:is()` and `:where()`;
  strict nested lists for `:not()`; and relative-selector lists for `:has()`.
- Universal and type selectors with unspecified, no-namespace (`|`), or
  any-namespace (`*|`) matching; ID and class selectors; and CSS escapes.
- Attribute presence and `=`, `~=`, `|=`, `^=`, `$=`, and `*=` operators,
  including explicit `i` and `s` modifiers and HTML-defined default value case
  rules.
- Descendant, child (`>`), adjacent-sibling (`+`), and general-sibling (`~`)
  combinators.
- `:root`, `:scope`, `:empty`, `:first-child`, `:last-child`, `:only-child`,
  `:first-of-type`, `:last-of-type`, and `:only-of-type`.
- `:nth-child()`, `:nth-last-child()`, `:nth-of-type()`, and
  `:nth-last-of-type()` with signed `An+B`, `odd`, and `even` syntax.
- `:is()`, `:where()`, `:not()`, non-nested `:has()`, `:lang()`, and `:dir()`.
- Markup-observable `:any-link`, `:link`, `:enabled`, `:disabled`, `:checked`,
  `:indeterminate`, `:required`, `:optional`, `:read-only`, `:read-write`,
  `:placeholder-shown`, and `:default` behavior where the answer can be derived
  from the static tree.

The column combinator (`||`) is parsed and rejected with a stable unsupported
feature diagnostic because column association is not a simple DOM relationship.
The Selectors Level 4 `of S` clause for `:nth-child()` and
`:nth-last-child()` is not implemented and is rejected as invalid syntax.
Pseudo-elements, including legacy single-colon spellings of `before`, `after`,
`first-letter`, and `first-line`, are unsupported. Named namespace prefixes are
parsed but fail compilation because the API has no caller namespace-resolver;
use unspecified, empty, or wildcard namespace syntax instead.

Browser/user/layout state pseudo-classes are unsupported and fail compilation.
The explicitly classified set is `:active`, `:autofill`, `:buffering`,
`:current`, `:defined`, `:focus`, `:focus-visible`, `:focus-within`,
`:fullscreen`, `:future`, `:host`, `:hover`, `:in-range`, `:invalid`,
`:local-link`, `:modal`, `:muted`, `:open`, `:out-of-range`, `:past`,
`:paused`, `:picture-in-picture`, `:playing`, `:popover-open`, `:seeking`,
`:stalled`, `:state`, `:target`, `:target-within`, `:user-invalid`,
`:user-valid`, `:valid`, `:visited`, and `:volume-locked`. Unknown
pseudo-classes and functional pseudo-classes are invalid rather than silently
matching nothing. Nested `:has()` is rejected, and all compilation/matching is
bounded by selector complexity, result, traversal, and `:has()` work limits.

### Deliberate deviations and non-browser boundaries

UniversalHtml implements server-side HTML workflows, not a browsing context.
The following differences are deliberate and are not counted as promises of
full browser-engine conformance:

- Script elements are retained only as inert data when policy permits; no
  JavaScript executes, and `document.write` or script-driven DOM mutation is
  not modeled. The scripting flag selects parsing branches such as `noscript`
  but does not enable execution.
- DOM events, mutation observers, custom-element lifecycle, shadow DOM,
  browsing/navigation history, form submission, rendering, layout, style
  cascade, computed style, media queries, and accessibility-tree computation
  are absent.
- There is no XML/XHTML parser, DTD/entity expansion, XPath, XSLT, or SVG/XML
  parsing mode. SVG and MathML support is their integration into HTML parsed as
  `text/html`.
- URL handling uses security-oriented lexical checks and BCL URI operations for
  the exposed subset after validation; it is not a general browser URL object,
  origin model, network fetcher, or complete WHATWG URL API.
- CSS support is limited to tokenizing and validating allowed inline style
  declarations for sanitization. `<style>`, at-rules, selectors in CSS,
  stylesheet loading, cascade, and a general CSSOM are outside scope.
- Selector matching has no layout, interaction, history, media, shadow-tree,
  or caller namespace-resolver state, as detailed above.
- Markdown is generated from corrected HTML. UniversalHtml does not parse
  Markdown and does not claim that arbitrary Markdown renderers reproduce the
  original HTML tree.
- Readability and metadata extraction are bounded heuristics, not browser or
  standards conformance and not sanitization. Sanitization is a separate final
  policy stage for renderable pipeline HTML.
- Streaming input is synchronous. The library never fetches URLs, loads
  subresources, executes content, or supplies browser security headers.

Known fixture failures are not deliberate deviations. They remain failures in
the conformance report until production behavior is corrected; they are never
reclassified as skips merely to improve a score.

### Conformance snapshot and scores

The canonical report is [`conformance-report.json`](conformance-report.json),
dated 2026-08-11 for package `0.1.0-alpha.1` and `net10.0`. Its overall status is
`partial`. The score below is `passed / cases`; skipped cases remain in the
denominator. Counts are authoritative and percentages are rounded to two
decimal places.

| Fixture suite | Cases | Passed | Failed | Skipped | Score | Status |
|---|---:|---:|---:|---:|---:|---|
| html5lib tokenizer, revision `224991ec10db04f056a89eed8b0bd8695fd2950e` | 7,036 | 6,967 | 65 | 4 | 99.02% | Partial |
| html5lib tree construction, revision `9329e64694e7835d0dcff9811e22856ef6ad16f9` | 3,553 | 3,395 | 154 | 4 | 95.55% | Partial |
| WHATWG named entities, snapshot 2026-08-10 | 2,231 | 2,231 | 0 | 0 | 100.00% | Pass |
| Selected deterministic WPT selectors, revision `9bc6e2404bff5349e48d7962b0a495582bc5ade8` | 58 | 58 | 0 | 0 | 100.00% | Pass |
| Selected CommonMark 0.31.2 generation cases | 6 | 6 | 0 | 0 | 100.00% | Pass |
| Selected GFM 0.29.0.gfm.13 generation cases | 5 | 5 | 0 | 0 | 100.00% | Pass |
| Repository adversarial sanitizer corpus | 36 | 36 | 0 | 0 | 100.00% | Pass |
| **Total** | **12,925** | **12,698** | **219** | **8** | **98.24%** | **Partial** |

The four tokenizer skips require optional XML-infoset coercion, which is not
HTML tokenization. The four tree-construction skips require actual script
execution. The 219 known failures are retained as failures: 65 tokenizer cases
(including 53 CDATA-state cases) and 154 tree-construction cases. A green unit
test run confirms that observed accounting matches this snapshot; it does not
turn a partial suite into full conformance. The selector, CommonMark, and GFM
fixtures are deliberately scoped subsets, so 100% means every selected case
passes, not every test published by those standards projects.

Verify the snapshot offline after running the behavioral tests:

```shell
dotnet test html.slnx --no-restore
dotnet run --project tools/ConformanceReport -- --verify
```

The verifier checks fixture revisions and counts, report arithmetic and status,
sanitizer-corpus checksum, and every permitted skip ID and reason. See
[`docs/conformance.md`](docs/conformance.md) for the accounting model.

## Resource limits

Every workflow is bounded independently of cancellation. `HtmlLimits.CreateDefault()` provides generous finite limits for general inputs, while `HtmlLimits.CreateUntrusted()` lowers every limit for hostile or unknown inputs. Neither preset disables a limit.

The exact `0.1.0-alpha.1` preset values are:

| Limit | `CreateDefault()` | `CreateUntrusted()` |
|---|---:|---:|
| Input bytes | 67,108,864 (64 MiB) | 8,388,608 (8 MiB) |
| Normalized input characters | 33,554,432 | 4,194,304 |
| HTML tokens | 2,000,000 | 250,000 |
| Attributes per tag | 1,024 | 256 |
| Characters per token | 4,194,304 | 262,144 |
| Characters per name | 4,096 | 1,024 |
| Characters per attribute value | 1,048,576 | 262,144 |
| DOM nodes | 1,000,000 | 100,000 |
| Tree/traversal/nested depth | 1,024 | 256 |
| Selector complexity units | 4,096 | 1,024 |
| Selector results | 1,000,000 | 100,000 |
| `:has()` relationship checks | 10,000,000 | 250,000 |
| Inline CSS tokens | 100,000 | 25,000 |
| Inline CSS declarations | 10,000 | 2,000 |
| URL characters | 1,048,576 | 16,384 |
| Decoded `data:` URL bytes | 16,777,216 (16 MiB) | 1,048,576 (1 MiB) |
| Expanded table cells | 1,000,000 | 100,000 |
| JSON-LD depth | 128 | 64 |
| JSON-LD properties | 100,000 | 10,000 |
| Tree/entry visits | 10,000,000 | 1,000,000 |
| Retained diagnostics/report changes | 10,000 | 1,000 |
| Output characters | 67,108,864 | 8,388,608 |

Limits cover input bytes and characters, tokens, attributes, token/name/value lengths, nodes and depth, selector complexity/results/`:has()` work, CSS tokens/declarations, URL and data-URI sizes, expanded table cells, JSON depth/properties, tree visits, diagnostics, and output characters. Setters reject zero and negative values. `Copy()` returns an independent mutable configuration, and a running operation snapshots its configuration so later caller mutations cannot widen its budget.

A shared operation budget accumulates work across pipeline stages, rejects work before a counter would exceed its maximum, and records the first exceeded category. Failed accounting does not consume capacity. Cancellation is checked at every consume or measured-value checkpoint; resource limits remain active even when the cancellation token cannot be canceled.

The presets are availability ceilings, not recommended upload sizes and not a
security policy. Applications should choose lower values based on expected
content and service capacity. Sanitization fails closed when a security-relevant
limit prevents a complete decision; text and Markdown converters may instead
return explicitly marked, safely truncated output. Diagnostic/report retention
can truncate audit detail without changing the underlying decision.

## Performance methodology and current claims

This alpha release publishes no throughput, latency, allocation, or peak-memory
baseline and makes no claim that one platform or workflow meets a particular
performance target. The planned benchmark harness, measured baselines,
profile-guided optimization, and regression thresholds were explicitly skipped
for this release. Unit tests, fuzz work budgets, conformance counts, elapsed
pipeline-stage timings, and resource-limit tests are correctness and
availability evidence; they are not substitutes for repeatable benchmarks.

Any future performance number published by this project must be produced by a
committed dependency-free harness and include enough information to reproduce
and interpret it:

1. Build the exact commit and package in `Release` for the stated .NET SDK and
   runtime version; record OS, architecture, CPU, logical-core count, memory,
   power mode, and relevant environment settings.
2. Use committed corpora with stable checksums. Report input bytes/characters,
   shape, encoding, and whether the case is tiny, typical, large, malformed, or
   adversarial. Do not combine unlike cases into an unexplained average.
3. Measure parsing from string, bytes, `Stream`, and incremental chunks
   separately. Likewise separate selectors, serialization, sanitization, text,
   Markdown, article extraction, and the combined pipeline.
4. Run correctness checks first. Warm the runtime and reusable compiled
   configuration, then execute enough measured repetitions to disclose sample
   count, warmup count, total duration, and run-to-run variance. Report cold
   startup separately when relevant.
5. Record wall-clock elapsed time, throughput in input units per second, and
   managed allocated bytes per operation. Measure peak working set or process
   memory separately and label its sampling method; do not present it as GC
   allocation. Report at least median and a tail/variance statistic rather than
   only the fastest run.
6. Use an otherwise idle machine, avoid network and fixture-generation work in
   the timed region, retain finite production limits, and disclose cancellation,
   diagnostics, source/byte tracking, serializer mode, policy, and converter
   options. Comparing unsafe or unbounded settings with safe defaults is not a
   valid speedup claim.
7. Compare results only across compatible hardware/runtime configurations or
   clearly normalize and label the difference. A regression threshold must
   allow documented environmental variance and must never override correctness,
   fail-closed behavior, or resource ceilings.

Until such artifacts and measurements are committed, performance comparisons
should be treated as application-specific experiments. Consumers should
benchmark their real documents, policies, selectors, output modes, concurrency,
and limits on their deployment hardware.

## Source locations and diagnostics

Source character offsets are zero-based UTF-16 code-unit offsets into the original input. Normalized character offsets are zero-based offsets after newline preprocessing; retaining both makes a folded CRLF observable as one normalized character covering two original characters. Lines and columns are one-based and follow normalized newlines. Original byte offsets are available only when byte tracking is enabled during decoding. Source spans are end-exclusive. Unknown positions and unavailable byte offsets use explicit negative/zero sentinels and corresponding `IsKnown()` or `HasByteOffset()` methods rather than `null`.

Diagnostic enum values are explicitly assigned and are never reused for a different meaning. Stable text identifiers use `UH` followed by a five-digit numeric code. `Information` records useful nonfailure context, `Warning` records corrected or suspicious input, `Error` records a recoverable operation error, and `Fatal` records a condition that prevents safe continuation. Ordinary HTML parse errors may therefore coexist with a usable parse result.

Diagnostic collectors return snapshot arrays and share the operation's diagnostic budget across stages. Once the budget is exhausted, additional diagnostics are counted as dropped, the collector reports truncation, and no additional messages are retained. Collector truncation never changes the underlying parser or security decision.

## Character and newline rules

HTML-critical character classification is implemented with explicit ordinal ranges. ASCII-insensitive comparison folds only `A` through `Z`; it never applies the current culture or Unicode case mappings. HTML whitespace is exactly tab, LF, form feed, CR, and space. Unicode whitespace, controls, surrogates, scalar validity, and noncharacters are classified independently so callers cannot accidentally substitute a broader Unicode rule for an HTML grammar rule.

Invalid Unicode scalar values become U+FFFD. HTML input preprocessing additionally replaces U+0000 with U+FFFD. Supplementary scalars are converted with validated UTF-16 surrogate helpers.

Newline preprocessing converts lone CR and CRLF to LF while preserving lone LF. A CRLF therefore contributes one normalized character and two original UTF-16 code units, which is reflected by the source-location mapping described above.

## Owned buffers and scanning

Core character and byte buffers copy caller spans into owned storage, grow geometrically only up to an explicit maximum, and validate capacity arithmetic before allocation. A failed multi-character, scalar, or byte append leaves the logical length and content unchanged. Snapshot array methods return independent copies; read-only span views remain valid only until the next buffer mutation.

Output character buffers and input byte buffers can consume the shared operation budget. Clearing or truncating such a buffer reclaims local storage positions but does not refund work already charged to the shared budget. This prevents repeated write-and-rewind cycles from bypassing configured limits. Cancellation is checked before any budgeted append changes buffer content.

The forward span reader also copies its input rather than retaining a caller span. It uses integer reads where `-1` means end-of-input, keeping an actual U+0000 distinguishable from EOF, and failed conditional consumes never advance the cursor.

## Numeric microsyntaxes

Numeric scanners recognize ASCII syntax directly and return concrete result objects containing success, overflow/range status, value, and consumed length. They do not use current-culture number rules or `TryParse`/`out` APIs. Exact HTML integer helpers accept an optional minus sign but not a plus sign; non-negative integers accept digits only. Hexadecimal scanning is prefix-free so callers control whether syntax such as `0x` or a character-reference marker is present.

Floating-point scanning follows the HTML valid-number grammar, including leading decimal points and signed exponents while excluding leading plus signs, incomplete fractions/exponents, NaN, and infinity. Pixel-density descriptors additionally require a finite value greater than zero followed by lowercase `x`.

The `An+B` scanner accepts ASCII-case-insensitive `odd` and `even`, signed coefficients and offsets, and CSS-permitted whitespace around the offset separator. Coefficients and offsets outside the 32-bit signed range report overflow instead of wrapping.

## Generated lookup data

Committed lookup sources are produced by the standalone `tools/GenerateTables` project using only the .NET SDK and base class libraries. Normal library builds do not run the generator or access the network. Generator inputs are local, provenance-labeled, and pinned by SHA-256 in `tools/GenerateTables/inputs/manifest.json`.

Run `dotnet run --project tools/GenerateTables -- --check` to verify checksums and committed output, or use `--write` after an intentional reviewed input update. Generated headers record the generator version, recreation command, input revision/checksum, and upstream standard provenance. See [`tools/GenerateTables/README.md`](tools/GenerateTables/README.md) for the update procedure.
