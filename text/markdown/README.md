# markdown

Dependency-free .NET Markdown parsing, inspection, transformation, and rendering.

## Public API

The supported API consists of the documented `public` types and members. Parser rules, scanners, stacks, render contexts other than `HtmlRenderContext`, and other implementation helpers are `internal` and may change without compatibility guarantees. Public symbols include XML documentation describing their behavior, inputs, results, and relevant mechanics.

## Simple Rendering

Create the `IMarkdown` service when you want to render a Markdown string directly:

```csharp
using markdown;

string source = "# Title\n\nHello *world*.\n\nSee [docs](/docs).";
IMarkdown markdown = new Markdown();

string html = markdown.RenderHtmlSource(source);
string normalizedMarkdown = markdown.RenderMarkdownSource(source);
string plainText = markdown.RenderPlainTextSource(source);
```

`markdown.RenderHtmlSource(source)` returns:

```html
<h1>Title</h1>
<p>Hello <em>world</em>.</p>
<p>See <a href="/docs">docs</a>.</p>
```

`markdown.RenderMarkdownSource(source)` returns normalized Markdown with a final line ending:

```markdown
# Title

Hello *world*.

See [docs](/docs).
```

`markdown.RenderPlainTextSource(source)` returns:

```text
Title

Hello world.

See docs.
```

If you need the parsed document before rendering, parse once and pass the document to a renderer:

```csharp
MarkdownDocument document = markdown.ParseSource(source);
string htmlFromDocument = markdown.RenderHtmlDocument(document);
```

## Pipeline Configuration

Use `IMarkdownPipelineBuilder` for dialect selection and extension registration. Use the public data fields on `MarkdownOptions` for output and safety settings:

```csharp
IMarkdown markdown = new Markdown();
IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
builder.UseGfm();
MarkdownOptions options = builder.GetOptions();
options.AllowRawHtml = true;
options.SoftBreakMode = MarkdownSoftBreakMode.Space;
options.LineEnding = "\r\n";
builder.SetOptions(options);
MarkdownPipeline pipeline = builder.Build();

string source = "~~old~~\nnew\n\n<span>raw</span>";
string html = markdown.RenderHtmlSourceWithPipeline(source, pipeline);
```

The configured pipeline renders GFM strikethrough, preserves allowed raw HTML, turns soft breaks into spaces, and writes CRLF line endings:

```html
<p><del>old</del> new</p>
<p><span>raw</span></p>
```

The service can also create a pipeline directly from public option data:

```csharp
MarkdownOptions safeOptions = new MarkdownOptions();
safeOptions.AllowRawHtml = false;
safeOptions.MaximumInputSize = 1_000_000;
safeOptions.MaximumNestingDepth = 64;
safeOptions.MaximumReferenceDefinitions = 1_000;
safeOptions.MaximumOutputSize = 5_000_000;
MarkdownPipeline safePipeline = markdown.CreatePipelineFromOptions(safeOptions);
```

## AST Transformations

Transforms run after parsing and built-in processing. Implement `IMarkdownTransform` to inspect or rewrite the public AST data before rendering:

```csharp
public class RewriteTextTransform : IMarkdownTransform
{
    public string GetName()
    {
        return "rewrite-docs-text";
    }

    public void Apply(MarkdownDocument document, MarkdownTransformContext context)
    {
        List<MarkdownNode> pending = new List<MarkdownNode>();
        pending.Add(document.Root);
        while (pending.Count > 0)
        {
            MarkdownNode node = pending[pending.Count - 1];
            pending.RemoveAt(pending.Count - 1);
            if (node.Kind == MarkdownNodeKind.Text)
            {
                node.Literal = node.Literal.Replace("docs", "guides");
            }

            pending.AddRange(node.Children);
        }
    }
}

IMarkdown markdown = new Markdown();
IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
builder.AddTransform(new RewriteTextTransform());
MarkdownPipeline pipeline = builder.Build();

string html = markdown.RenderHtmlSourceWithPipeline("Read the docs.", pipeline);
```

The rendered HTML includes the transformed text:

```html
<p>Read the guides.</p>
```

## Typed Extension Points

Reusable extensions can be ordinary classes implementing one of the focused public contracts:

- `IMarkdownBlockRule` and `IMarkdownInlineRule` extend parsing through controlled parse contexts.
- `IMarkdownTransform` changes or inspects the completed AST.
- `IHtmlRenderRule` can handle selected nodes before the built-in HTML renderer.
- `IMarkdownNodeVisitor` receives tree-walking enter and leave calls.

Each rule supplies a stable name. Custom block and inline rules are inserted before the built-in paragraph and text fallbacks, and an inline rule reports any additional characters that can trigger it so plain-text scanning stops at the right positions. HTML rules run in registration order until one handles the node.

For example, a reusable HTML rule can render headings while allowing every other node to use normal rendering:

```csharp
public class HeadingHtmlRule : IHtmlRenderRule
{
    public string GetName()
    {
        return "example.heading";
    }

    public bool TryRender(HtmlRenderContext context, MarkdownNode node)
    {
        if (node.Kind != MarkdownNodeKind.Heading)
        {
            return false;
        }

        IMarkdown markdown = new Markdown();
        context.Writer.Write("<title>");
        markdown.RenderHtmlChildren(context, node);
        context.Writer.Write("</title>");
        markdown.WriteHtmlLineEnding(context);
        return true;
    }
}

IMarkdown markdown = new Markdown();
IMarkdownPipelineBuilder builder = markdown.CreatePipelineBuilder();
builder.AddHtmlRenderRule(new HeadingHtmlRule());
MarkdownPipeline pipeline = builder.Build();
```

## Event Stream Transformations

Event-stream processing remains an internal implementation facility. Public transforms operate on the data-only AST and can be registered through `IMarkdownPipelineBuilder`.

## Markdown Generation

Markdown generation is available through `IMarkdown.RenderMarkdownSource` and `IMarkdown.RenderMarkdownDocument`. The lower-level writer and fluent builder remain internal implementation facilities.

## Initial Compatibility Report

Status key:

- `Covered`: implemented with local unit coverage.
- `Partial`: implemented for the current subset, with known remaining compatibility work.
- `Planned`: intentionally listed in the project scope, but not yet implemented.

### CommonMark

Status: `Partial`.

Validation:

- CommonMark fixture examples: 5.
- Local coverage includes line reading, indentation, block starts, paragraphs, headings, thematic breaks, block quotes, lists, code blocks, HTML blocks, link reference definitions, inline text, escapes, entities, code spans, autolinks, raw HTML inline parsing, links, images, and emphasis.
- Safety and recovery coverage includes malformed inline fallbacks, malformed block fallbacks, resource limits, fuzz-like generated inputs, and performance smoke tests.

Known limits:

- The bundled fixture is a curated subset, not the full CommonMark 0.31.2 example suite.
- Full CommonMark edge-case parity remains a compatibility goal.

### GFM

Status: `Partial`.

Validation:

- GFM fixture examples: 5.
- Local coverage includes tables, task list items, strikethrough, bare URL and email autolinks, and GFM disallowed raw HTML filtering.

Known limits:

- The bundled fixture is a curated subset, not the full GitHub Flavored Markdown specification suite.
- Full GFM edge-case parity remains a compatibility goal.

### Advanced Extensions

Status: `Covered` for the library-defined extension behavior currently implemented.

Validation:

- Local coverage includes footnotes, definition lists, heading IDs and attributes, front matter, abbreviations, typography, math spans and blocks, diagram blocks, emoji shortcodes, CJK soft breaks, admonitions, custom containers, custom fenced blocks, custom inline delimiters, AST transforms, event stream transforms, source maps, trivia capture, and Markdown generation.

Known limits:

- Advanced extensions are library-defined compatibility surfaces unless a future task ties a feature to an external specification.
- Include directives, partial document parsing, and incremental parsing are still planned work.
