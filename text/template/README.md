# template

`template` is a small .NET template engine with a native syntax, compatibility
profiles for common template dialects, runtime safety limits, diagnostics, and
tooling for analysis and formatting.

## Quick Start

Create a model with `TemplateValue`, render a template, and read the result:

```csharp
TemplateValueFactory values = new TemplateValueFactory();
Dictionary<string, TemplateValue> model = new Dictionary<string, TemplateValue>();
model["name"] = values.CreateString("Ada");

TemplateRenderer renderer = new TemplateRenderer();
string output = renderer.RenderToString("Hello {{ name }}", values.CreateMap(model));
```

Use a compatibility profile when the input template uses another dialect:

```csharp
TemplateProfileCatalog profiles = new TemplateProfileCatalog();
TemplateOptions options = profiles.CreateHandlebarsOptions();
TemplateRegistry registry = new TemplateRegistry();
TemplateMemberResolver resolver = new TemplateMemberResolver();
TemplateRenderer renderer = new TemplateRenderer();
renderer.Configure(options, registry, resolver);
string output = renderer.RenderToString("Hello {{name}}", values.CreateMap(model));
```

Register templates and partials for includes, renders, imports, inheritance, and
partial calls:

```csharp
TemplateRegistry registry = new TemplateRegistry();
registry.RegisterTemplate("card", "Name: {{ name }}");

TemplateRenderer renderer = new TemplateRenderer();
renderer.Configure(new TemplateOptions(), registry, new TemplateMemberResolver());
string output = renderer.RenderToString("{% include \"card\" %}", values.CreateMap(model));
```

## Syntax Reference

Native templates use these delimiters:

| Construct | Syntax | Notes |
| --- | --- | --- |
| Output | `{{ expression }}` | Encodes according to `TemplateOptions.EscapingMode`. |
| Statement | `{% name arguments %}` | Used for assignment, includes, imports, and control flow. |
| Comment | `{# text #}` | Ignored by rendering. |
| Raw block | `{% raw %}...{% endraw %}` | Emits literal text without scanning nested tags. |
| Trim marker | `{{- value -}}` or `{%- if ok -%}` | Removes adjacent whitespace in profiles that support trimming. |

Common statements and blocks:

| Feature | Example |
| --- | --- |
| Assignment | `{% set title "Hello" %}` or `{% assign title = name %}` |
| Conditional | `{% if ok %}yes{% else %}no{% endif %}` |
| Loop | `{% for item in items limit 10 offset 2 %}{{ item }}{% endfor %}` |
| Include | `{% include "card" %}` |
| Isolated render | `{% render "card" user %}` |
| Partial | `{% partial "badge" %}` |
| Import macros | `{% import "macros" %}` |
| Inheritance | `{% extends "layout" %}{% block body %}...{% endblock %}` |
| Macro | `{% macro greet person %}Hi {{ person.name }}{% endmacro %}` |
| Filter block | `{% filter upper %}hello{% endfilter %}` |
| Call block | `{% call wrapper("*") %}body{% endcall %}` |

Expressions support variables, paths, indexers, literals, arrays, objects,
ranges, arithmetic and logical operators, tests, function calls, and filters:

```text
{{ user.name | upper }}
{{ items[0] }}
{{ count > 0 and enabled }}
{{ name is defined }}
{{ format(name, {"prefix": "@"}) }}
```

## Compatibility Matrix

| Profile | Create options | Autoescape | Escaping mode | Input conversion | Notes |
| --- | --- | --- | --- | --- | --- |
| Native | `TemplateProfileCatalog.CreateNativeOptions()` | No | None | None | Full native syntax. |
| Mustache | `TemplateProfileCatalog.CreateMustacheOptions()` | Yes | HTML | Automatic | Variables, sections, inverted sections, lambdas, partials, and inheritance-style parents. Complex expressions are limited. |
| Handlebars | `TemplateProfileCatalog.CreateHandlebarsOptions()` | Yes | HTML | Automatic | Helpers, block helpers, `each`, hash arguments, inline partials, partial blocks, and dynamic partials. |
| Liquid | `TemplateProfileCatalog.CreateLiquidOptions()` | No | None | Automatic | Assign, capture, filters, raw/comment blocks, include/render, and Liquid loop options. |
| Jinja | `TemplateProfileCatalog.CreateJinjaOptions()` | Yes | HTML | Automatic | Filters, tests, set/capture, autoescape blocks, includes, imports, macros, and inheritance. |
| Django | `TemplateProfileCatalog.CreateDjangoOptions()` | Yes | HTML | Automatic | Uses the Jinja-compatible path with Django profile defaults. |

Use `TemplateCompatibilityAnalyzer` before migration to detect escaping changes,
trim marker differences, unsupported target constructs, and expressions that are
too complex for the target profile.

## Security Model

The renderer is designed to make untrusted templates and data easier to contain,
but callers still need to choose safe defaults for their environment.

Important controls:

| Control | API |
| --- | --- |
| Output encoding | `TemplateOptions.EscapingMode`, `TemplateOptions.AutoEscape` |
| Strict behavior | `TemplateOptions.StrictMode` and the related strictness fields |
| Template length | `TemplateOptions.MaximumTemplateLength` |
| Output length | `TemplateOptions.MaximumOutputLength` |
| Include depth | `TemplateOptions.MaximumIncludeDepth` |
| Loop iterations | `TemplateOptions.MaximumLoopIterations` |
| Expression depth | `TemplateOptions.MaximumExpressionDepth` |
| Macro recursion depth | `TemplateOptions.MaximumRecursionDepth` |
| Template count | `TemplateOptions.MaximumTemplateCount` |
| Render time | `TemplateOptions.MaximumRenderMilliseconds` |
| Member access | `TemplateSecurityPolicy`, `TemplateMemberResolverOptions` |
| Loader roots | `TemplateSecurityPolicy.AllowedLoaderRoots` and `FileTemplateLoader` |

Member resolution blocks dangerous names such as constructors and prototype-style
members by default. Set `TemplateSecurityPolicy.AllowedMemberNames` to a non-empty
allowlist when exposing a narrow model surface. Public method access is disabled
unless explicitly enabled and allowlisted through `TemplateMemberResolverOptions`.
File loaders reject traversal and symlink escapes; use
`FileTemplateLoader.SetSecurityPolicy` to require an explicitly allowed loader root.

Use `TemplateSecurityAuditor` to produce warning diagnostics for unsafe
configuration, relaxed member access, missing member policies, and raw output in
autoescaped profiles.

## API Overview

Only symbols declared `public` are part of the supported package API. Parser,
evaluator, scope, and renderer implementation machinery is declared `internal`.
Every public type and member includes generated XML documentation; use that
symbol documentation for detailed argument, return-value, exception, and
behavior notes.

Rendering:

| API | Purpose |
| --- | --- |
| `TemplateRenderer.RenderToString` | Render text or a parsed document to a string. |
| `TemplateRenderer.RenderToWriter` | Stream rendered output to a `TextWriter`. |
| `TemplateRenderer.RenderToStringAsync` | Render asynchronously with optional cancellation. |
| `TemplateRenderer.RenderToWriterAsync` | Stream asynchronously with optional cancellation. |
| `TemplateRenderer.GetLastDependencies` | Read dependencies loaded by the last render. |

Parsing and diagnostics:

| API | Purpose |
| --- | --- |
| `TemplateLexer.Lex` | Tokenize a configured `TemplateSource`. |
| `TemplateParser.Parse` | Parse a `TemplateLexerResult` into a `TemplateDocumentSyntax`. |
| `TemplateDiagnostic` | Represents parser, analyzer, graph, and audit diagnostics. |
| `ITemplateDiagnosticSink`, `TemplateDiagnosticCollectionSink` | Receive and collect diagnostics from parsing, linting, and rendering. |
| `TemplateLinter.Lint` | Analyze templates for evaluation, composition, security, flow, reuse, deprecation, and compatibility issues. |
| `TemplateDiagnosticFormatter.Format` | Format diagnostics with source line and marker output. |
| `TemplateRenderer.GetLastSourceMap` | Map rendered output ranges to expression, template, partial, macro, and inheritance source spans. |
| `ITemplateSourceMapCollector`, `TemplateSourceMapCollector` | Collect source mappings and look them up by rendered-output offset. |

Models and values:

| API | Purpose |
| --- | --- |
| `TemplateValueFactory.CreateString`, `CreateNumber`, `CreateBoolean` | Create scalar template values. |
| `TemplateValueFactory.CreateList`, `CreateMap`, `CreateObject` | Create collection and object values. |
| `TemplateValueFactory.CreateFunction` | Expose a function to templates. |
| `TemplateValueFactory.Convert` | Convert CLR objects into template values. |

Registry and loading:

| API | Purpose |
| --- | --- |
| `TemplateRegistry.RegisterTemplate` | Register named templates in memory. |
| `TemplateRegistry.RegisterPartial` | Register named partials in memory. |
| `ITemplateLoader` | Supply templates from an application-defined source. |
| `TemplateRegistry.AddTemplateLoader` | Append a loader to the ordered fallback chain. |
| `MemoryTemplateLoader` | In-memory named template loader. |
| `FileTemplateLoader` | File-backed loader rooted at a configured directory. |

Customization:

| API | Purpose |
| --- | --- |
| `ITemplateMemberResolver` | Define member and index lookup without subclassing engine internals. |
| `TemplateRenderer.Configure` | Configure options, a registry, and a custom resolver. |
| `new TemplateRenderContext(..., ITemplateMemberResolver)` | Supply a resolver for a manually managed render context. |

`MemoryTemplateLoader` and `FileTemplateLoader` implement `ITemplateLoader`, and
`TemplateMemberResolver` implements `ITemplateMemberResolver`. Custom loaders
return `null` when a name is absent, allowing the registry to continue through
the chain. Directly registered templates always have highest priority.

Tooling:

| API | Purpose |
| --- | --- |
| `TemplateSyntaxDumper` | Dump parsed syntax trees. |
| `TemplateDependencyGraphBuilder` | Build a static graph of literal template and partial dependencies. |
| `TemplateFormatter` | Format native syntax with stable indentation. |
| `TemplateWhitespaceNormalizer` | Normalize line endings, trailing whitespace, and repeated blank lines. |
| `TemplateSecurityAuditor` | Audit security-sensitive configuration and raw output. |
| `TemplateCompatibilityAnalyzer` | Analyze migration risks between syntax profiles. |
