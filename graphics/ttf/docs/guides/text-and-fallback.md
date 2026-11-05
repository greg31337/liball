# Shaping, fallback, and vertical text

## Shape before measuring or embedding

The managed text pipeline decodes UTF-16, itemizes script and bidi runs,
normalizes when requested, maps characters, applies OpenType GSUB/GPOS/GDEF,
and preserves original source clusters. Rendering and PDF generation must use
the resulting glyph order, advances, offsets, and cluster mappings; mapping one
Unicode scalar at a time is not an equivalent substitute.

The shaping and line-layout implementations are currently internal assembly
services. The public assembly exposes immutable `ShapedRun` consumption points
for subsetting and PDF encoding, but it does not yet expose a public method that
creates a `ShapedRun`. External callers must not use reflection to invoke
`TextShaper` or `TextLayout`. Until a public shaping facade is released, obtain
shaped runs only through an integration compiled with the library, or use a
separate shaping engine and the glyph-level APIs. This boundary is deliberate
and should be checked when upgrading.

When a `ShapedRun` is available, retain it as the common source of truth:

```csharp
FontSubsetBuilder subsetBuilder = new FontSubsetBuilder(
    instance,
    new FontSubsetOptions());
subsetBuilder.AddShapedRun(shapedRun);

PdfFontBuilder pdfBuilder = new PdfFontBuilder(
    instance,
    new PdfFontOptions());
PdfEncodedRun encoded = pdfBuilder.AddShapedRun(shapedRun);
```

This preserves ligature and combining-mark closure, original Unicode sequences,
positioning, and `ActualText` recommendations. Do not rebuild these relationships
from glyph IDs after shaping.

## Configure cluster-safe fallback

`FontSet` is application-owned and never consults operating-system font state.
Load and add the exact faces your application is licensed and prepared to use,
then configure fallback order explicitly.

```csharp
FontSet fonts = new FontSet();
fonts.AddFile("Primary.ttf", new FontLoadOptions());
fonts.AddFile("ArabicFallback.ttf", new FontLoadOptions());
fonts.AddFile("Symbols.ttf", new FontLoadOptions());

fonts.AddScriptFallbackFamily("Arab", "Arabic Fallback");
fonts.AddFallbackFamily("Symbols");

FontRequest request = new FontRequest("Primary");
request.SetWeight(400);
FontFallbackList candidates = fonts.GetFallbacks(request, "Arab");
FontMatch match = fonts.ResolveCluster(request, "Arab", "\u0644\u0651");
```

Family strings must match names in the loaded faces. Script identifiers are
four-letter ISO 15924 codes. `GetFallbacks` returns primary, script-specific,
then set-wide matches with duplicates removed.

Fallback is a cluster operation. Pass an entire grapheme or shaping-unsafe span
to `ResolveCluster`; never split a base from its combining marks, a variation
selector from its base, or a ligature candidate merely because different faces
cover individual scalars. When no face covers every required scalar, the method
returns the candidate with the greatest coverage and uses configured order to
break ties. A null result means no configured family matched.

## Vertical text

Vertical text is more than rotating a horizontal line. A correct pipeline uses
vertical orientation data, vertical OpenType substitutions, vertical metrics,
vertical origins and advances, and vertical line progression. The internal
layout pipeline selects these together through `TextWritingMode`; external
callers should observe the same public-facade limitation described above.

For a vertical PDF resource, make the writing mode explicit and retain vertical
data in any separately generated subset:

```csharp
PdfFontOptions pdfOptions = new PdfFontOptions();
pdfOptions.SetWritingMode(PdfWritingMode.Vertical);

FontSubsetOptions subsetOptions = new FontSubsetOptions();
subsetOptions.SetVerticalPolicy(FontSubsetVerticalPolicy.Retain);
```

Add vertically shaped runs to a `PdfFontBuilder` configured with these options.
The finalized Type 0 artifact uses Identity-V metadata and exposes `/DW2` and
`/W2` models through its vertical-width artifact. The PDF consumer is still
responsible for text matrices, line progression, marked content, and page
content syntax. If the source font has no usable vertical data, use the
library's reported fallback metrics; do not silently mix horizontal advances
with a vertical encoding.
