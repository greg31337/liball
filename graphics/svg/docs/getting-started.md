# Getting started

`svg` is a synchronous, managed .NET 10 library. Add a reference to the `svg`
package (or to `src/svg.csproj` while working in this repository), then import
the namespace:

```csharp
using svg;
```

Most operations return a named result containing a `Success` field, diagnostics,
and operation-specific output. Check `Success` before consuming the output. A
successful validation operation can still report that a document is not valid;
for validation, check both `Success` and `Valid`.

## Load untrusted SVG securely

`SvgLoadOptions` starts in `SecureStatic` mode. Its defaults prohibit DTDs, do
not execute scripts, preserve source locations, copy caller-provided bytes, and
apply bounded input, tree, CSS, resource, geometry, font, filter, raster, and
timeline limits.

```csharp
SvgLoadOptions loadOptions = new SvgLoadOptions();
loadOptions.BaseUri = "https://assets.example/diagram.svg";

// Tighten defaults for the workload when practical.
loadOptions.Limits.Input.MaxInputBytes = 4 * 1024 * 1024;
loadOptions.Limits.Tree.MaxNodes = 50_000;
loadOptions.Limits.Resources.MaxCount = 32;

// This example does not need external files or network resources.
loadOptions.ResourcePolicy.AllowFileUris = false;
loadOptions.ResourcePolicy.AllowHttpUris = false;
loadOptions.ResourcePolicy.AllowHttpsUris = false;

SvgLoadResult loaded = SvgLoader.Load(svgText, loadOptions);
if (!loaded.Success)
{
    foreach (SvgDiagnostic diagnostic in loaded.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.Message);
    }
    return;
}

SvgDocument document = loaded.Document;
```

Use `SvgLoader.Load` for strings, character or byte spans, byte arrays, streams,
text readers, and XML readers. Use `LoadPath` only when local-file access is an
intentional host decision. `LoadUri` never performs I/O itself: it requires the
resource provider described below.

Set `BaseUri` whenever relative references should have meaning. Security limits
and cancellation tokens are part of the operation options, so use tighter values
for small icons and preserve the secure defaults for general input.

## Query and edit the DOM

The editable DOM uses tagged `SvgNode` objects rather than an element subclass
for every SVG element. `SvgQuery` returns document-ordered snapshots. Mutate the
tree through `SvgNodeApi`, `SvgDocumentApi`, or `SvgBuilder` so ownership,
versions, indexes, and source-dirty state remain consistent.

```csharp
SvgQuery query = new SvgQuery();
SvgNode hero = query.GetById(document, "hero");
if (hero == null)
{
    Console.Error.WriteLine("The #hero element was not found.");
    return;
}

// SvgBuilder can edit attributes and append content in an existing document.
SvgBuilder editor = SvgBuilder.Create(document);
editor.Select(hero)
    .SetAttribute("fill", "#3366ff")
    .BeginElement(SvgElementKind.Title)
    .AddText("Primary illustration")
    .EndElement();

if (!editor.Succeeded())
{
    Console.Error.WriteLine(editor.Message);
    return;
}

// SvgNodeApi exposes explicit structural mutation results.
SvgDocumentApi factory = new SvgDocumentApi();
SvgNodeApi nodes = new SvgNodeApi();
SvgNode comment = factory.CreateComment("edited by the host");
SvgMutationResult mutation = nodes.AppendChild(document.DocumentElement, comment);
if (!mutation.Success)
{
    Console.Error.WriteLine(mutation.Message);
}
```

For selectors, pass explicit parser and matching bounds and check the query
result. Class, ID, qualified-name, namespace, source-span, predicate, and visitor
queries are also available.

```csharp
SvgSelectorQueryResult selected = new SvgQuery().QuerySelectorAll(
    document,
    ".accent",
    new SvgCssLimits(),
    new SvgSelectorMatchOptions());

if (!selected.Success)
{
    foreach (SvgDiagnostic diagnostic in selected.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.Message);
    }
    return;
}

foreach (SvgNode node in selected.Nodes)
{
    Console.WriteLine(node.QualifiedName);
}
```

Query results are snapshots. Editing the document after a query does not update
an existing result list; query again when current results are required.

## Validate before use

Loading establishes a bounded, well-formed editable document. Validation is a
separate pass that checks SVG structure, typed values, CSS, references, security,
and accessibility.

```csharp
SvgValidationOptions validationOptions = new SvgValidationOptions();
validationOptions.Profile = SvgValidationProfile.Svg2;
validationOptions.MaximumNodes = 50_000;
validationOptions.ResourcePolicy = loadOptions.ResourcePolicy;

SvgValidationResult validation = document.Validate(validationOptions);
if (!validation.Success)
{
    Console.Error.WriteLine("Validation could not complete: " + validation.Error);
    return;
}

foreach (SvgValidationIssue issue in validation.Issues)
{
    Console.Error.WriteLine(issue.Rule + ": " + issue.Diagnostic.Message);
}

if (!validation.Valid)
{
    return;
}
```

`Success` means the bounded validation operation completed. `Valid` means it
completed without conformance errors. Warnings such as callback-required
external resources can therefore accompany a valid document.

## Sanitize untrusted content

Sanitization removes script-bearing and policy-disallowed content and returns an
independent document; it does not mutate the source document.

```csharp
SvgSanitizationOptions sanitizationOptions = new SvgSanitizationOptions();
sanitizationOptions.AllowExternalLinks = false;
sanitizationOptions.ResourcePolicy.AllowFileUris = false;
sanitizationOptions.ResourcePolicy.AllowHttpUris = false;
sanitizationOptions.ResourcePolicy.AllowHttpsUris = false;

SvgSanitizationResult sanitized = document.Sanitize(sanitizationOptions);
if (!sanitized.Success)
{
    foreach (SvgDiagnostic diagnostic in sanitized.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.Message);
    }
    return;
}

SvgDocument safeDocument = sanitized.Document;
Console.WriteLine(
    $"Removed {sanitized.RemovedNodes} nodes and " +
    $"{sanitized.RemovedAttributes} attributes.");
```

Sanitization is not a substitute for the load limits or the host's resource
policy. Load securely first, sanitize, then use the returned document for output
or rendering.

## Optimize and serialize

Processing presets mutate the supplied editable document and report every
change. `SafeDefault`, `Canonicalize`, `Sprite`, and `Sanitize` preserve
accessibility and view-box semantics by default. `Aggressive` explicitly permits
lossy operations; review its change log and error bounds before using it.

```csharp
SvgProcessOptions processOptions = SvgProcessPresets.SafeDefault();
processOptions.MaximumPasses = 8;
processOptions.MaximumChanges = 20_000;

SvgProcessResult processed = safeDocument.Process(processOptions);
if (!processed.Success)
{
    Console.Error.WriteLine("Processing failed: " + processed.Error);
    return;
}

SvgWriteOptions writeOptions = new SvgWriteOptions();
writeOptions.Mode = SvgWriteMode.Compact;
writeOptions.DoctypePolicy = SvgWriteDoctypePolicy.Omit;

SvgWriteResult written = SvgWriter.Write(processed.Document, writeOptions);
if (!written.Success)
{
    Console.Error.WriteLine(written.Message);
    return;
}

string optimizedSvg = written.Text;
```

Use `SvgProcessPresets.Canonicalize()` for deterministic normalized output. Use
`SvgProcessOptions.DryRun` to inspect a proposed change set without retaining the
mutations. `SvgWriter` also writes to text writers, streams, UTF-8 arrays and
spans, SVGZ, and data URLs.

## Generate a document

`SvgBuilder` creates a namespace-correct SVG root automatically. Builder methods
retain a sticky error, so check `Succeeded()` after the chain.

```csharp
SvgBuilder builder = new SvgBuilder();
builder.SetAttribute("viewBox", "0 0 100 100")
    .BeginElement(SvgElementKind.Rect)
    .SetAttribute("x", "10")
    .SetAttribute("y", "10")
    .SetAttribute("width", "80")
    .SetAttribute("height", "80")
    .SetAttribute("rx", "8")
    .SetAttribute("fill", "#3366ff")
    .EndElement();

if (!builder.Succeeded())
{
    Console.Error.WriteLine(builder.Message);
    return;
}

SvgDocument generated = builder.Build();
SvgWriteResult generatedXml = SvgWriter.Write(generated, new SvgWriteOptions());
```

For geometry-heavy output, use `SvgPathBuilder`. `SvgPaintBuilder` and
`SvgFilterBuilder` create definitions and wire deterministic IDs. `SvgCanvas`
offers save/restore, transform, path, fill, stroke, clip, image, text, and layer
operations while still producing an editable `SvgDocument`.

## Render to pixels or PNG

Rendering resolves styles, references, resources, animation state, scene
commands, and pixels under the supplied limits. Set a viewport explicitly for
server-side work and enable PNG encoding only when PNG bytes are needed.

```csharp
SvgRenderOptions renderOptions = new SvgRenderOptions();
renderOptions.ViewportWidth = 800;
renderOptions.ViewportHeight = 600;
renderOptions.Scale = 1;
renderOptions.AnimationTime = 0;
renderOptions.EncodePng = true;
renderOptions.MaximumPixels = 800L * 600L;

SvgRenderResult rendered = SvgRenderer.Render(safeDocument, renderOptions);
if (!rendered.Success)
{
    foreach (SvgDiagnostic diagnostic in rendered.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.Message);
    }
    return;
}

byte[] png = rendered.PngBytes;
SvgPixelBuffer pixels = rendered.Pixels;
```

The result also contains the resolved document, scene, render-command list,
drawing bounds, feature report, resource accounting, and work counts. Resolved
documents and scenes are versioned snapshots; resolve or render again after DOM
edits.

## Sample declarative animation

The timeline evaluator reports SMIL timing state at an exact time without
mutating the DOM. The same time can be passed to the renderer.

```csharp
double sampleTime = 1.25;
SvgAnimationTimelineOptions timelineOptions = new SvgAnimationTimelineOptions();
SvgAnimationTimelineResult timeline = SvgAnimationTimeline.Evaluate(
    safeDocument,
    sampleTime,
    timelineOptions);

if (!timeline.Success)
{
    foreach (SvgDiagnostic diagnostic in timeline.Diagnostics)
    {
        Console.Error.WriteLine(diagnostic.Message);
    }
    return;
}

foreach (SvgAnimationTimelineEntry entry in timeline.Animations)
{
    if (entry.Active || entry.Frozen)
    {
        Console.WriteLine(
            $"progress={entry.SimpleProgress}, repeat={entry.RepeatIteration}");
    }
}

renderOptions.AnimationTime = sampleTime;
SvgRenderResult frame = SvgRenderer.Render(safeDocument, renderOptions);
```

Event-based timing uses an explicit `SvgAnimationEventQueue`. Inject events,
access keys, or wall-clock labels into `timelineOptions.Events` before evaluation.
No browser event loop or wall clock is consulted implicitly.

## Provide resources, fonts, and images

The core never opens a URI on behalf of a callback-enabled operation. It first
normalizes the URI and applies `SvgResourcePolicy`, then calls the synchronous
`ISvgResourceProvider`. Route requests by `SvgResourceRequest.ResourceKind` and
use a host-owned allowlist or asset store. Do not turn an arbitrary URI directly
into a filesystem path. Implement the interface for a reusable stateful service,
or use `SvgResourceProvider` to adapt a one-off lambda.

```csharp
Dictionary<string, byte[]> assets = new Dictionary<string, byte[]>();
assets.Add("https://assets.example/picture.png", picturePngBytes);
assets.Add("https://assets.example/typeface.ttf", typefaceBytes);
assets.Add("https://assets.example/document.svg", documentSvgBytes);

SvgResolveOptions resolveOptions = new SvgResolveOptions();
resolveOptions.ResourcePolicy.AllowFileUris = false;
resolveOptions.ResourcePolicy.AllowCrossOriginUris = false;
resolveOptions.ResourceProvider = new SvgResourceProvider(request =>
{
    if (!assets.ContainsKey(request.NormalizedUri))
    {
        return null;
    }

    SvgResourceResponse response = new SvgResourceResponse();
    response.Bytes = assets[request.NormalizedUri];
    response.FinalUri = request.NormalizedUri;
    response.Cacheable = true;

    if (request.ResourceKind == SvgResourceKind.Image)
    {
        response.ActualMediaType = "image/png";
        response.ImageMetadata = new SvgImageMetadata();
        response.ImageMetadata.IntrinsicSize = SvgIntrinsicSize.Create(640, 480);
    }
    else if (request.ResourceKind == SvgResourceKind.Font)
    {
        response.ActualMediaType = "font/ttf";
    }
    else
    {
        response.ActualMediaType = request.MediaKind;
    }

    return response;
});

SvgResolvedDocument resolved = new SvgDocumentResolver().Resolve(
    safeDocument,
    resolveOptions);
```

The provider may return either `Bytes` or a readable `Stream`, never both. Set
the actual or declared media type and final URI. Returned arrays are copied by
default; caller streams remain caller-owned. The resource count, byte, redirect,
URI, and callback-duration limits still apply.

For a root URI, put the provider on `SvgLoadOptions` and call `LoadUri`:

```csharp
SvgLoadOptions uriOptions = new SvgLoadOptions();
uriOptions.ResourceProvider = resolveOptions.ResourceProvider;
SvgLoadResult remote = SvgLoader.LoadUri(
    "https://assets.example/document.svg",
    uriOptions);
```

### Font and text callbacks

Font URLs arrive through the same resource provider with
`ResourceKind == SvgResourceKind.Font`. Hosts can also register already-owned
font bytes with `SvgFontDatabase.AddFont`. When built-in shaping encounters an
unsupported script or OpenType layout feature, `SvgShapingOptions.ShapingCallback`
can return a validated glyph list; if it is absent or declines, the result uses
the documented deterministic fallback.

The renderer's `SvgRenderOptions.TextPathCallback` is the final host seam for
turning a resolved text scene node into outline geometry:

```csharp
renderOptions.TextPathCallback = textNode =>
{
    // Look up the host font and shape textNode.Text, then return path geometry.
    return BuildTextOutline(textNode);
};
```

The returned `SvgPathData` must be finite and within the configured geometry and
raster limits. Treat font parsers, shapers, and outline providers as trusted host
components and bound their work independently.

### Image callbacks

Image URLs arrive with `ResourceKind == SvgResourceKind.Image`. The managed core
probes and decodes PNG. For another host-supported format, the resource response
can provide `SvgImageMetadata`, including intrinsic size and whether decoded
content is available to the host. Set `SvgResolveOptions.ImageContentAvailable`
only when the host really has usable decoded content. Metadata does not itself
provide pixels to the managed rasterizer. The current `SvgRenderer` consumes
managed PNG data, so transcode another image format to PNG before returning it
when the library must produce the final pixels.

Callbacks are synchronous and may receive a cancelled token. Check
`request.CancellationToken`, return bounded data, keep policy decisions separate
from acquisition, and avoid re-entering the same resolver with the same URI.

## A safe server-side sequence

For untrusted input, a typical pipeline is:

1. Load with secure defaults, tighter workload-specific limits, and no callback
   unless external resources are required.
2. Check load diagnostics and `Success`.
3. Validate and decide whether warnings are acceptable for the application.
4. Sanitize and continue with the returned independent document.
5. Optionally apply a semantics-preserving processing preset.
6. Render or serialize with explicit output and cancellation limits.

Keep URI access, font shaping, image codecs, foreign content, and navigation in
host code. Supplying a callback makes that callback part of the application's
trusted computing boundary; it does not make the referenced content safe by
itself.
