# Security posture and deployment guide

This library is designed to parse, inspect, sanitize, resolve, and render
untrusted SVG under explicit work and memory bounds. Its default loading mode is
`SvgProcessingMode.SecureStatic`. The portable core does not execute script and
does not implicitly open network resources or referenced files.

Those guarantees do not turn SVG into a harmless file format by themselves.
Source-preserving documents can retain active content for later serialization,
caller callbacks run as trusted application code, and generous general-purpose
limits may still be too large for a multi-tenant service. For untrusted input,
tighten limits for the workload, sanitize before publishing, and isolate the
process when a hard CPU or memory boundary is required.

## Security model

The library provides these boundaries:

- Input, XML tree, CSS, reference traversal, resources, geometry, fonts,
  filters, raster surfaces, and animation work have positive configurable
  limits.
- DTDs are prohibited by default. The optional preserve-only policy retains a
  declaration as metadata; the library does not expand its entities.
- Script elements, event-handler attributes, and script-capable URLs are never
  executed by the library.
- Nested network and file resources must pass `SvgResourcePolicy` and are
  acquired only through a caller-supplied callback. A missing callback produces
  a diagnostic rather than I/O.
- Gzip, data URL, font-container, image, filter, and raster work is checked
  against dedicated limits.
- Long-running synchronous stages accept cancellation tokens and check them at
  bounded points.

The following remain host responsibilities:

- HTTP clients, filesystem access, redirects, authentication, DNS and address
  filtering, proxy behavior, and transport timeouts.
- The safety and availability of resource, shaping, image, text-outline, and
  foreign-content integrations.
- Browser navigation, HTML layout, script engines, media playback, and browser
  security headers.
- Per-request CPU, wall-clock, and process memory enforcement. Library limits
  bound modeled work; they are not an operating-system sandbox.
- Safe configuration of a caller-created `XmlReader`. Prefer byte, stream, or
  text loading for untrusted input. An `XmlReader` can perform work before the
  library sees a node, so disable its DTD processing and resolver in host code.

`SvgLoader.LoadPath` is an explicit request by the caller to open a local path;
it is not an implicit reference fetch. Do not expose an unvalidated user path to
that entry point. `SvgLoader.LoadUri` performs no I/O, but the caller-supplied
root URI is an explicit trust decision: `AllowCallerResolvedRootUri` gates the
request and the callback must enforce its own root URI allowlist.

## Secure load defaults

`new SvgLoadOptions()` has these security-relevant defaults:

| Field | Default | Meaning |
| --- | --- | --- |
| `Mode` | `SecureStatic` | Static, caller-mediated processing; script still never executes in any mode. |
| `Limits` | `new SvgSecurityLimits()` | Applies the defaults listed below. |
| `ResourcePolicy` | `new SvgResourcePolicy()` | Applies the URI, origin, redirect, and media policy listed below. |
| `ResourceProvider` | `null` | No external resource can be acquired through the loader. |
| `CancellationToken` | `None` | The caller must supply cancellation when required. |
| `DiagnosticBehavior` | `Strict` | Stops on the first conformance error. |
| `PreserveSource` | `true` | Retains source text and locations. |
| `PreserveUnknown` | `true` | Retains unknown well-formed content; this is not sanitization. |
| `CopyInputData` | `true` | Takes a snapshot of caller and callback byte arrays. |
| `DtdPolicy` | `Prohibit` | Rejects every document type declaration. |

All fields in `SvgSecurityLimits` must be positive. Call `Validate()` after
constructing a shared limits object and reject a configuration whose `Success`
field is false.

## Default security limits

The defaults are intended as finite general-purpose ceilings, not recommended
per-request quotas. Values shown as MiB use 1,048,576 bytes.

| Group | Default fields |
| --- | --- |
| Input | `MaxInputBytes` 33,554,432 (32 MiB); `MaxInputCharacters` 33,554,432; `MaxGzipOutputBytes` 67,108,864 (64 MiB); `MaxGzipExpansionRatio` 100; `MaxXmlTokenCharacters` 16,777,216. |
| Tree | `MaxDepth` 128; `MaxNodes` 250,000; `MaxAttributesPerElement` 256; `MaxTotalAttributes` 500,000; `MaxTextCharacters` 16,777,216; `MaxNameCharacters` 1,024. |
| CSS | `MaxCharacters` 8,388,608; `MaxTokens` 1,000,000; `MaxRules` 50,000; `MaxDeclarations` 250,000; `MaxSelectorTokens` 4,096; `MaxBlockDepth` 64; `MaxImportDepth` 16; `MaxVariableExpansionTokens` 100,000. |
| References | `MaxDepth` 64; `MaxCount` 100,000; `MaxUseInstances` 100,000; `MaxPaintServerDepth` 32; `MaxEffectDepth` 32; `MaxConditionalChecks` 1,000,000. |
| Resources | `MaxCount` 256; `MaxBytesPerResource` 33,554,432 (32 MiB); `MaxTotalBytes` 134,217,728 (128 MiB); `MaxDataUrlBytes` 16,777,216 (16 MiB); `MaxRedirects` 5; `MaxUriCharacters` 8,192; `MaxCallbackMilliseconds` 30,000. |
| Geometry | `MaxPathSegments` 1,000,000; `MaxPoints` 1,000,000; `MaxFlattenedSegments` 2,000,000; `MaxSubdivisionDepth` 32; `MaxIntersections` 1,000,000; `MaxBooleanEdges` 2,000,000; `MaxHitTestNodes` 1,000,000. |
| Fonts | `MaxFonts` 128; `MaxBytesPerFont` 33,554,432 (32 MiB); `MaxTotalBytes` 134,217,728 (128 MiB); `MaxTablesPerFont` 4,096; `MaxGlyphsPerFont` 65,535; `MaxPointsPerGlyph` 1,000,000; `MaxShapingGlyphs` 100,000; `MaxVariationAxes` 64. |
| Filters | `MaxPrimitives` 256; `MaxAreaPixels` 16,777,216; `MaxKernelElements` 65,536; `MaxBlurStandardDeviation` 4,096; `MaxTurbulenceOctaves` 10; `MaxOffscreenSurfaces` 64. |
| Raster | `MaxWidth` 32,768; `MaxHeight` 32,768; `MaxSurfacePixels` 100,000,000; `MaxPixelBytes` 400,000,000; `MaxPngChunks` 100,000; `MaxPngDecodedBytes` 400,000,000. |
| Timeline | `MaxAnimations` 100,000; `MaxInstances` 1,000,000; `MaxKeyframes` 100,000; `MaxEventTimes` 100,000; `MaxRepeatCount` 1,000,000; `MaxSampleWork` 10,000,000. |

Individual APIs also have operation-local ceilings. For example,
`SvgRenderOptions` defaults to `MaximumNodes == 100_000` and
`MaximumPixels == 1_000_000_000`, and `SvgSanitizationOptions` defaults to one
million inspected nodes and 100,000 retained diagnostics. Tighten both the
aggregate limits and the operation-local fields used by the selected pipeline.

## Default resource policy

`SvgResourcePolicy` is an allow/deny decision engine. An allowed URI does not
cause I/O: non-data resources still require an explicit callback. `data:` URLs
can be decoded inside the library and therefore deserve explicit consideration
even when no callback is installed.

| Policy area | Default |
| --- | --- |
| Caller-resolved root URI | Allowed. The callback must independently allowlist a root passed to `LoadUri`. |
| Imported stylesheets | Allowed, including relative imports. Data and file imports are denied. Cross-origin imports are denied. |
| Same-document references | Allowed. |
| Other relative references | Allowed. |
| Other data URLs | Allowed. |
| Local and remote file URLs | Denied. |
| HTTP and HTTPS URLs | Allowed by policy, but never fetched without a callback. |
| Cross-origin HTTP resources | Denied. |
| Redirects | Allowed up to `Resources.MaxRedirects`; cross-origin redirects are denied. |
| Custom schemes | Denied; `AllowedSchemes` is empty. |
| Unknown media types | Denied; `AllowedMediaTypes` is empty. |
| File root | Empty. Set `FileRootUri` whenever enabling file URLs. |

The policy recognizes SVG/XML documents, CSS, common image media types, TTF,
OTF, WOFF, WOFF2, gzip, and binary data according to resource kind. Nested
document and image acquisition evaluates policy before the callback and again
against the callback's final URI, redirect count, and effective media type.
Stylesheet imports also recheck the final URI and require `text/css`.

For an operation that needs no external content, disable root URI loading,
imports, relative references, data URLs, file URLs, HTTP(S), redirects, and
custom schemes. Keep `AllowSameDocumentUris` enabled when local paint servers,
clips, masks, filters, or `use` references are needed.

## Script non-execution and safe output

The loader preserves source by default. As a result, a loaded document can still
contain `script`, `onload` and other event attributes, `javascript:` or
`vbscript:` URLs, CSS URLs with script-capable schemes, and foreign content.
`Dynamic` mode does not change this rule: it preserves dynamic metadata but does
not provide a script engine.

Validation reports script content and event handlers as preserved inert content,
reports script-capable resource URLs as denied, and reports `foreignObject` or
foreign namespaces as requiring host handling. Rendering never invokes script.
However, writing the original or merely validated document can emit the authored
active content again. A browser or another consumer may then execute or navigate
it according to that consumer's rules.

Use `SvgSanitizer` when output will cross a trust boundary. Its secure-static
defaults return an independent document, remove script and foreign-content
subtrees, remove event handlers and script-bearing URLs, reject policy-disallowed
resource content, and disable external navigation. Serialize only the returned
`SvgSanitizationResult.Document`. Validation alone is not sanitization.

For browser delivery, also set host-controlled response headers appropriate to
the embedding context, including the correct SVG content type and
`X-Content-Type-Options: nosniff`. Consider attachment delivery or a restrictive
sandbox/CSP when inline active content is not required. HTTP headers and browser
embedding behavior are outside this library.

## Provider and callback trust boundaries

Every provider or callback executes application code with the application's
privileges. Treat its implementation and every codec or service it invokes as
part of the trusted computing base.

Resource providers are synchronous. The library normalizes and bounds the URI,
passes a cancellation token, accepts exactly one of `Bytes` or a readable
`Stream`, bounds the bytes it copies or reads, and validates response metadata.
Callback streams remain caller-owned; returned arrays are copied when
`CopyInputData` is true, which is the default.

Important limitations:

- `MaxCallbackMilliseconds` is measured after a resource provider returns. It
  turns an over-duration response into a failure but cannot interrupt a blocked
  callback.
- Cancellation is cooperative. A callback, stream, HTTP client, font shaper, or
  image codec that ignores cancellation can continue blocking host resources.
- Resource-provider and shaping-callback exceptions are application failures and should
  not be used for ordinary denial. Return `null` or a declined response instead.
- A resource provider controls the claimed final URI, redirect count, media type, and
  bytes. Higher-level resource stages validate these values, but the host must
  still authenticate its asset source and prevent SSRF, local-file disclosure,
  credential forwarding, and cross-tenant cache leaks.
- `ShapingCallback` and `TextPathCallback` receive parsed library objects and
  return glyph or geometry data. Their outputs are validated or later bounded,
  but their own CPU and memory use is not preempted.
- `ForeignContentCallbackAvailable` and `ImageContentAvailable` are capability
  declarations, not sandboxed decoders. Foreign HTML/layout and non-PNG pixels
  remain host work.

Use a host client with its own connection, response, and total deadlines. Check
`request.CancellationToken`, return only immutable allowlisted assets, cap data
before returning it, and partition caches by authorization context. For a hard
deadline around untrusted plugins or codecs, run them in a separately constrained
process.

## Decompression, filter, raster, and font risks

### SVGZ and compressed resources

Gzip is detected from its magic bytes, independent of a filename. Encoded input
is first bounded by `MaxInputBytes`; decompression is then bounded by the smaller
of `MaxGzipOutputBytes` and compressed size multiplied by
`MaxGzipExpansionRatio`. Cancellation is checked while reading. These controls
bound output allocation, but decompression still consumes CPU. Services that do
not need SVGZ should reject gzip magic before loading, and services that do need
it should choose a lower expansion ratio and output ceiling for their payloads.

Data URLs are decoded internally up to `MaxDataUrlBytes` and also consume the
resource count and total-byte budgets. Disable `AllowDataUris` when embedded
payloads are unnecessary.

### Filters and rasterization

Filter graphs can multiply memory and CPU through large regions, blur radii,
convolution kernels, turbulence octaves, and intermediate surfaces. The filter
limits bound each of those dimensions, while raster limits bound dimensions,
surface pixels, pixel bytes, PNG chunks, and decoded PNG bytes. Rendering adds a
cumulative `MaximumPixels` work limit.

Set an explicit viewport and scale, calculate an application maximum output
area before rendering, and lower `MaximumPixels`, `MaxAreaPixels`,
`MaxOffscreenSurfaces`, `MaxSurfacePixels`, and `MaxPixelBytes` together. A small
final image does not by itself guarantee a small filter region or intermediate
surface.

### Fonts and text

Font resources are untrusted binary input. The managed readers bound font count,
individual and total bytes, tables, glyphs, outline points, shaping glyphs, and
variation axes. WOFF and WOFF2 reconstruction is also checked against
`MaxBytesPerFont`. Keep font URLs behind the same URI and media allowlist as
other resources, and reduce font limits when an application uses only a small
known set.

Advanced shaping and text-outline callbacks can call native or third-party
libraries outside these parsing bounds. Give those integrations independent
input, time, memory, and process limits. Prefer pre-registered, host-owned font
bytes to accepting arbitrary remote fonts in a multi-tenant renderer.

## Safe server-side recipes

### Sanitize and serialize without external resources

This pipeline uses one policy for loading and validation, then relies on the
sanitizer's stricter no-external defaults. It retains same-document references
but disables embedded data payloads.

```csharp
SvgResourcePolicy policy = new SvgResourcePolicy();
policy.AllowCallerResolvedRootUri = false;
policy.AllowImportedStyleSheets = false;
policy.AllowRelativeImportUris = false;
policy.AllowDataImportUris = false;
policy.AllowFileImportUris = false;
policy.AllowCrossOriginImports = false;
policy.AllowSameDocumentUris = true;
policy.AllowRelativeUris = false;
policy.AllowDataUris = false;
policy.AllowFileUris = false;
policy.AllowRemoteFileUris = false;
policy.AllowHttpUris = false;
policy.AllowHttpsUris = false;
policy.AllowCrossOriginUris = false;
policy.AllowRedirects = false;
policy.AllowCrossOriginRedirects = false;
policy.AllowCustomSchemes = false;

SvgSecurityLimits limits = new SvgSecurityLimits();
limits.Input.MaxInputBytes = 4 * 1024 * 1024;
limits.Input.MaxInputCharacters = 4 * 1024 * 1024;
limits.Tree.MaxNodes = 50_000;
limits.Resources.MaxCount = 16;
limits.Resources.MaxTotalBytes = 8 * 1024 * 1024;

SvgLoadOptions loadOptions = new SvgLoadOptions();
loadOptions.Limits = limits;
loadOptions.ResourcePolicy = policy;
loadOptions.CancellationToken = requestCancellation;

SvgLoadResult loaded = SvgLoader.Load(untrustedSvgBytes, loadOptions);
if (!loaded.Success)
{
    return;
}

SvgValidationOptions validationOptions = new SvgValidationOptions();
validationOptions.MaximumNodes = limits.Tree.MaxNodes;
validationOptions.ResourcePolicy = policy;
SvgValidationResult validation = SvgValidator.Validate(
    loaded.Document,
    validationOptions);
if (!validation.Success || !validation.Valid)
{
    return;
}

SvgSanitizationOptions sanitizationOptions = new SvgSanitizationOptions();
sanitizationOptions.MaximumNodes = limits.Tree.MaxNodes;
sanitizationOptions.AllowList.DataUrls = false;
SvgSanitizationResult sanitized = SvgSanitizer.Sanitize(
    loaded.Document,
    sanitizationOptions);
if (!sanitized.Success)
{
    return;
}

SvgWriteOptions writeOptions = new SvgWriteOptions();
writeOptions.DoctypePolicy = SvgWriteDoctypePolicy.Omit;
writeOptions.CancellationToken = requestCancellation;
SvgWriteResult written = SvgWriter.Write(sanitized.Document, writeOptions);
if (!written.Success)
{
    return;
}

string safeSvg = written.Text;
```

`requestCancellation` and `untrustedSvgBytes` are supplied by the host request.
Do not substitute the original `loaded.Document` in the final write.

### Render a bounded sanitized document

Reuse the sanitized document, limits, cancellation token, and no-external policy.
Set both render and resolve limits because document rendering includes resolution.

```csharp
SvgRenderOptions renderOptions = new SvgRenderOptions();
renderOptions.ViewportWidth = 800;
renderOptions.ViewportHeight = 600;
renderOptions.Scale = 1;
renderOptions.EncodePng = true;
renderOptions.Limits = limits;
renderOptions.MaximumNodes = 50_000;
renderOptions.MaximumPixels = 800L * 600L * 8L;
renderOptions.CancellationToken = requestCancellation;
renderOptions.ResolveOptions.Limits = limits;
renderOptions.ResolveOptions.ResourcePolicy = policy;
renderOptions.ResolveOptions.CancellationToken = requestCancellation;

SvgRenderResult rendered = SvgRenderer.Render(
    sanitized.Document,
    renderOptions);
if (!rendered.Success)
{
    return;
}

byte[] png = rendered.PngBytes;
```

The multiplier in `MaximumPixels` is an application work allowance, not an
output-size calculation. Measure representative documents and choose a lower
ceiling when possible.

### Allow one controlled external asset

Policy is necessary but not sufficient. The callback below also matches an
exact normalized URI and resource kind. `trustedPngBytes` must already be a
bounded, immutable host-owned snapshot; a real HTTP-backed implementation also
needs host-side timeouts and address filtering.

```csharp
SvgResolveOptions resolveOptions = new SvgResolveOptions();
resolveOptions.ResourcePolicy.AllowRelativeUris = false;
resolveOptions.ResourcePolicy.AllowDataUris = false;
resolveOptions.ResourcePolicy.AllowFileUris = false;
resolveOptions.ResourcePolicy.AllowHttpUris = false;
resolveOptions.ResourcePolicy.AllowHttpsUris = true;
resolveOptions.ResourcePolicy.AllowCrossOriginUris = false;
resolveOptions.ResourcePolicy.AllowRedirects = false;
resolveOptions.ResourceProvider = new SvgResourceProvider(request =>
{
    if (request.CancellationToken.IsCancellationRequested ||
        request.ResourceKind != SvgResourceKind.Image ||
        request.NormalizedUri != "https://assets.example/logo.png")
    {
        return null;
    }

    SvgResourceResponse response = new SvgResourceResponse();
    response.Bytes = trustedPngBytes;
    response.ActualMediaType = "image/png";
    response.FinalUri = request.NormalizedUri;
    response.RedirectCount = 0;
    response.Cacheable = true;
    return response;
});
```

Set the document base URI to the expected HTTPS origin so the same-origin policy
has context. Avoid providers that map an authored URI directly to a path, pass
ambient credentials to arbitrary origins, follow unrestricted redirects, or
share authenticated responses across tenants.

## Deployment checklist

- Accept only the input and output formats the service actually needs.
- Create fresh or immutable options per request; do not let tenants mutate a
  shared `SvgSecurityLimits`, policy, font database, or cache.
- Set request-specific cancellation plus a host wall-clock deadline.
- Tighten input, node, CSS, reference, resource, geometry, font, filter, raster,
  timeline, and operation-local limits from measured workloads.
- Disable every unused URI class, especially data, file, HTTP(S), redirects, and
  imports. Independently allowlist inside callbacks.
- Validate response media types from bytes where practical; do not trust a URL
  extension or remote `Content-Type` alone.
- Sanitize before serialization or browser delivery, and use only the independent
  sanitized document afterward.
- Keep diagnostics in trusted logs; avoid returning paths, internal URIs, or
  authorization details to an untrusted client.
- Isolate optional native codecs and hard-to-bound integrations in a constrained
  worker process.

See [Getting started](getting-started.md) for the general API flow and the
[standards and support matrix](support-matrix.md) for feature-level behavior and
known limitations.
