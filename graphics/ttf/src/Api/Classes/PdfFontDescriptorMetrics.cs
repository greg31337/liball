namespace ttf;

/// <summary>Contains normalized PDF FontDescriptor metrics and font-file metadata.</summary>
/// <remarks>
/// Design metrics are converted to PDF's 1000-unit glyph space. Bounding-box
/// minima round down and maxima round up so the converted box still encloses
/// the source box. Other integral metrics round to nearest, with exact halves
/// away from zero.
/// </remarks>
public class PdfFontDescriptorMetrics : IPdfFontDescriptorMetrics
{
    int boundingBoxXMinimum;
    int boundingBoxYMinimum;
    int boundingBoxXMaximum;
    int boundingBoxYMaximum;
    double italicAngle;
    int ascent;
    int descent;
    int capHeight;
    bool hasXHeight;
    int xHeight;
    int missingWidth;
    int stemVertical;
    FontLineMetricSource lineMetricSource;
    PdfFontDescriptorMetricSource boundingBoxSource;
    PdfFontDescriptorMetricSource italicAngleSource;
    PdfFontDescriptorMetricSource capHeightSource;
    PdfFontDescriptorMetricSource xHeightSource;
    PdfFontDescriptorMetricSource stemVerticalSource;
    PdfEmbeddedFontFileKind fontFileKind;
    string? fontFileSubtype;
    int fontFileLength;

    internal PdfFontDescriptorMetrics(
        int boundingBoxXMinimum,
        int boundingBoxYMinimum,
        int boundingBoxXMaximum,
        int boundingBoxYMaximum,
        double italicAngle,
        int ascent,
        int descent,
        int capHeight,
        bool hasXHeight,
        int xHeight,
        int missingWidth,
        int stemVertical,
        FontLineMetricSource lineMetricSource,
        PdfFontDescriptorMetricSource boundingBoxSource,
        PdfFontDescriptorMetricSource italicAngleSource,
        PdfFontDescriptorMetricSource capHeightSource,
        PdfFontDescriptorMetricSource xHeightSource,
        PdfFontDescriptorMetricSource stemVerticalSource,
        PdfEmbeddedFontFileKind fontFileKind,
        string? fontFileSubtype,
        int fontFileLength)
    {
        this.boundingBoxXMinimum = boundingBoxXMinimum;
        this.boundingBoxYMinimum = boundingBoxYMinimum;
        this.boundingBoxXMaximum = boundingBoxXMaximum;
        this.boundingBoxYMaximum = boundingBoxYMaximum;
        this.italicAngle = italicAngle;
        this.ascent = ascent;
        this.descent = descent;
        this.capHeight = capHeight;
        this.hasXHeight = hasXHeight;
        this.xHeight = xHeight;
        this.missingWidth = missingWidth;
        this.stemVertical = stemVertical;
        this.lineMetricSource = lineMetricSource;
        this.boundingBoxSource = boundingBoxSource;
        this.italicAngleSource = italicAngleSource;
        this.capHeightSource = capHeightSource;
        this.xHeightSource = xHeightSource;
        this.stemVerticalSource = stemVerticalSource;
        this.fontFileKind = fontFileKind;
        this.fontFileSubtype = fontFileSubtype;
        this.fontFileLength = fontFileLength;
    }

    /// <summary>Gets the PDF FontBBox minimum x coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    public int GetBoundingBoxXMinimum() { return boundingBoxXMinimum; }

    /// <summary>Gets the PDF FontBBox minimum y coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    public int GetBoundingBoxYMinimum() { return boundingBoxYMinimum; }

    /// <summary>Gets the PDF FontBBox maximum x coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    public int GetBoundingBoxXMaximum() { return boundingBoxXMaximum; }

    /// <summary>Gets the PDF FontBBox maximum y coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    public int GetBoundingBoxYMaximum() { return boundingBoxYMaximum; }

    /// <summary>Gets the source used for FontBBox.</summary>
    /// <returns>The bounding-box metric source.</returns>
    public PdfFontDescriptorMetricSource GetBoundingBoxSource()
    {
        return boundingBoxSource;
    }

    /// <summary>Gets the dominant-stem angle in counter-clockwise degrees.</summary>
    /// <returns>The PDF ItalicAngle value.</returns>
    public double GetItalicAngle() { return italicAngle; }

    /// <summary>Gets the source used for ItalicAngle.</summary>
    /// <returns>The italic-angle metric source.</returns>
    public PdfFontDescriptorMetricSource GetItalicAngleSource()
    {
        return italicAngleSource;
    }

    /// <summary>Gets the PDF ascent.</summary>
    /// <returns>The selected ascent in 1000-unit space.</returns>
    public int GetAscent() { return ascent; }

    /// <summary>Gets the PDF descent.</summary>
    /// <returns>The selected descent, normally negative, in 1000-unit space.</returns>
    public int GetDescent() { return descent; }

    /// <summary>Gets the underlying OpenType line-metric source.</summary>
    /// <returns>The recommended line-metric selection.</returns>
    public FontLineMetricSource GetLineMetricSource()
    {
        return lineMetricSource;
    }

    /// <summary>Gets the source category used for ascent and descent.</summary>
    /// <returns>The recommended line-metric source category.</returns>
    public PdfFontDescriptorMetricSource GetLineMetricsSource()
    {
        return PdfFontDescriptorMetricSource.RecommendedLineMetrics;
    }

    /// <summary>Gets the PDF cap height.</summary>
    /// <returns>The cap height in 1000-unit space.</returns>
    public int GetCapHeight() { return capHeight; }

    /// <summary>Gets the source used for cap height.</summary>
    /// <returns>The cap-height metric source.</returns>
    public PdfFontDescriptorMetricSource GetCapHeightSource()
    {
        return capHeightSource;
    }

    /// <summary>Reports whether an x-height could be derived.</summary>
    /// <returns>True when OS/2 or a cmap-selected lowercase x supplied a value.</returns>
    public bool HasXHeight() { return hasXHeight; }

    /// <summary>Gets the PDF x-height.</summary>
    /// <returns>The x-height in 1000-unit space.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    public int GetXHeight()
    {
        if (!hasXHeight)
        {
            throw new System.InvalidOperationException(
                "PDF x-height metadata is unavailable.");
        }

        return xHeight;
    }

    /// <summary>Gets the source used for x-height.</summary>
    /// <returns>The x-height metric source, or Unavailable.</returns>
    public PdfFontDescriptorMetricSource GetXHeightSource()
    {
        return xHeightSource;
    }

    /// <summary>Gets the width used when a character code has no explicit width.</summary>
    /// <returns>The embedded .notdef advance in 1000-unit space.</returns>
    public int GetMissingWidth() { return missingWidth; }

    /// <summary>Gets the source used for MissingWidth.</summary>
    /// <returns>The .notdef advance source.</returns>
    public PdfFontDescriptorMetricSource GetMissingWidthSource()
    {
        return PdfFontDescriptorMetricSource.NotdefAdvance;
    }

    /// <summary>Gets the estimated dominant vertical stem width.</summary>
    /// <returns>The PDF StemV estimate in 1000-unit space.</returns>
    public int GetStemVertical() { return stemVertical; }

    /// <summary>Gets the source used for the vertical-stem estimate.</summary>
    /// <returns>The stem-estimate metric source.</returns>
    public PdfFontDescriptorMetricSource GetStemVerticalSource()
    {
        return stemVerticalSource;
    }

    /// <summary>Gets the FontDescriptor font-file entry kind.</summary>
    /// <returns>FontFile2 or FontFile3.</returns>
    public PdfEmbeddedFontFileKind GetFontFileKind() { return fontFileKind; }

    /// <summary>Gets the PDF dictionary key for the embedded program.</summary>
    /// <returns><c>FontFile2</c> or <c>FontFile3</c>.</returns>
    public string GetFontFileKey()
    {
        return fontFileKind == PdfEmbeddedFontFileKind.FontFile2 ?
            "FontFile2" : "FontFile3";
    }

    /// <summary>Reports whether the font stream requires a Subtype entry.</summary>
    /// <returns>True for FontFile3.</returns>
    public bool HasFontFileSubtype()
    {
        return fontFileSubtype != null;
    }

    /// <summary>Gets the FontFile3 stream subtype.</summary>
    /// <returns>The subtype, or null for FontFile2.</returns>
    public string? GetFontFileSubtype() { return fontFileSubtype; }

    /// <summary>Gets the unfiltered embedded-program byte length.</summary>
    /// <returns>The byte length before any PDF stream filter is applied.</returns>
    public int GetFontFileLength() { return fontFileLength; }

    /// <summary>Reports whether the stream dictionary requires Length1.</summary>
    /// <returns>True for a TrueType FontFile2 stream.</returns>
    public bool HasLength1()
    {
        return fontFileKind == PdfEmbeddedFontFileKind.FontFile2;
    }

    /// <summary>Gets the decoded FontFile2 Length1 value.</summary>
    /// <returns>The unfiltered TrueType program length.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The program is stored in FontFile3.
    /// </exception>
    public int GetLength1()
    {
        if (!HasLength1())
        {
            throw new System.InvalidOperationException(
                "A FontFile3 stream does not use Length1 metadata.");
        }

        return fontFileLength;
    }
}
