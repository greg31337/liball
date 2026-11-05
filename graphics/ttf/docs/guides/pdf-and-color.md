# PDF artifacts and color appearance

## Accumulate document-local font usage

`ttf` builds neutral artifacts; it does not create PDF objects, cross-reference
tables, page content, graphics state, images, or tags. Create one builder per
font instance, writing mode, and policy combination used by a document.

```csharp
FontInstance instance = face.CreateDefaultInstance();
PdfFontOptions options = new PdfFontOptions();
options.SetVersion(PdfVersion.Pdf1_7);
options.SetAccessibilityProfile(PdfAccessibilityProfile.PdfUa1);

PdfFontBuilder builder = new PdfFontBuilder(instance, options);
builder.AddText("Hello");
builder.AddGlyph(42, new int[] { 0x41 });
PdfFontResource resource = builder.FinalizeFont();
```

`AddText` is appropriate for direct cmap usage. For shaped text, pass the
original `ShapedRun` to `AddShapedRun` and retain the returned `PdfEncodedRun`
for the content stream. It contains character codes, positioned glyph data,
advances, offsets, and `TJ` adjustment suggestions.

Finalization succeeds once and freezes allocation. Accumulate all use first;
subsequent additions and a second finalization are rejected. Use
`GetDeduplicationKey()` to share byte-identical resources within your PDF
writer.

## Assemble the PDF resource

The finalized kind determines which strongly typed artifact to consume:

- `Type0CidFontType2` supplies a TrueType `FontFile2`, CID-to-GID decision,
  widths, descriptor facts, encoding metadata, and optional vertical widths.
- `Type0CidFontType0` supplies a CID-keyed CFF `FontFile3` and charset/CID maps.
- `Type0OpenType` supplies an OpenType `FontFile3` where the selected PDF version
  permits it.
- `SimpleTrueType` supplies the explicitly requested, restricted one-byte
  encoding path.

`CopyEmbeddedFont()` and `GetBaseFontName()` cover shared data. Select the typed
artifact with `GetCidFontType2Artifact()`, `GetCidFontType0Artifact()`,
`GetOpenTypeArtifact()`, or `GetSimpleEncoding()` only after checking
`GetFontKind()`.

Always emit the supplied ToUnicode CMap. Review every
`GetActualTextRecommendation(...)` and wrap the corresponding content with
`ActualText` when requested; font-level mapping cannot represent every ambiguous
ligature or reused glyph. A compatibility report describes the minimum PDF
version and resource-level PDF/A or PDF/UA concerns, but cannot establish whole
document conformance.

For vertical resources, set `PdfWritingMode.Vertical`, consume Identity-V
encoding metadata and vertical widths, and use vertically shaped positioned
runs. See [Shaping, fallback, and vertical text](text-and-fallback.md).

## Add a color appearance without losing text

Keep ordinary embedded text as the semantic layer. Resolve the same instance,
glyph ID, palette, and foreground color used during layout:

```csharp
int glyphId = 42;
ColorGlyphOptions colorOptions = new ColorGlyphOptions();
colorOptions.SetPaletteIndex(0);
colorOptions.SetForegroundColor(new ColorGlyphColor(20, 20, 20, 255));

ColorGlyph color = new ColorGlyphResolver().ResolveGlyphWithOptions(
    instance,
    glyphId,
    colorOptions);
PdfColorGlyphAppearance guidance = color.GetPdfAppearanceGuidance();
```

Translate `DrawVectorAppearance` from the normalized COLR paint graph,
`DrawSvgAppearance` from the sanitized SVG document, and image guidance from
the selected bitmap strike. `DrawImageAndOutlineAppearance` also requires the
ordinary outline. `UseTextGlyph` needs no separate appearance.

Align the appearance to the positioned text glyph's origin, scale, offsets,
direction, and transforms. When separate art is visible, keep the text object
for extraction and accessibility but normally make its monochrome paint
invisible. Treat the appearance as an artifact of that text in tagged output,
not as a second reading-order item. If the PDF target cannot represent the
selected paint safely, fall back to the ordinary text glyph.

Color tables inside the embedded font are not a portable PDF color-rendering
contract. The consuming PDF library owns clipping, blending, transparency,
image decoding, SVG conversion, and profile restrictions.
