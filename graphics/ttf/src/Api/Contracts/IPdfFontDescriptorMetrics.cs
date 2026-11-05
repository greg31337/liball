namespace ttf;

/// <summary>Defines the instance operations exposed by <see cref="PdfFontDescriptorMetrics"/>.</summary>
public interface IPdfFontDescriptorMetrics
{
    /// <summary>Gets the PDF FontBBox minimum x coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    int GetBoundingBoxXMinimum();

    /// <summary>Gets the PDF FontBBox minimum y coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    int GetBoundingBoxYMinimum();

    /// <summary>Gets the PDF FontBBox maximum x coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    int GetBoundingBoxXMaximum();

    /// <summary>Gets the PDF FontBBox maximum y coordinate.</summary>
    /// <returns>The outward-rounded coordinate in 1000-unit space.</returns>
    int GetBoundingBoxYMaximum();

    /// <summary>Gets the source used for FontBBox.</summary>
    /// <returns>The bounding-box metric source.</returns>
    PdfFontDescriptorMetricSource GetBoundingBoxSource();

    /// <summary>Gets the dominant-stem angle in counter-clockwise degrees.</summary>
    /// <returns>The PDF ItalicAngle value.</returns>
    double GetItalicAngle();

    /// <summary>Gets the source used for ItalicAngle.</summary>
    /// <returns>The italic-angle metric source.</returns>
    PdfFontDescriptorMetricSource GetItalicAngleSource();

    /// <summary>Gets the PDF ascent.</summary>
    /// <returns>The selected ascent in 1000-unit space.</returns>
    int GetAscent();

    /// <summary>Gets the PDF descent.</summary>
    /// <returns>The selected descent, normally negative, in 1000-unit space.</returns>
    int GetDescent();

    /// <summary>Gets the underlying OpenType line-metric source.</summary>
    /// <returns>The recommended line-metric selection.</returns>
    FontLineMetricSource GetLineMetricSource();

    /// <summary>Gets the source category used for ascent and descent.</summary>
    /// <returns>The recommended line-metric source category.</returns>
    PdfFontDescriptorMetricSource GetLineMetricsSource();

    /// <summary>Gets the PDF cap height.</summary>
    /// <returns>The cap height in 1000-unit space.</returns>
    int GetCapHeight();

    /// <summary>Gets the source used for cap height.</summary>
    /// <returns>The cap-height metric source.</returns>
    PdfFontDescriptorMetricSource GetCapHeightSource();

    /// <summary>Reports whether an x-height could be derived.</summary>
    /// <returns>True when OS/2 or a cmap-selected lowercase x supplied a value.</returns>
    bool HasXHeight();

    /// <summary>Gets the PDF x-height.</summary>
    /// <returns>The x-height in 1000-unit space.</returns>
    /// <exception cref="System.InvalidOperationException">X-height is unavailable.</exception>
    int GetXHeight();

    /// <summary>Gets the source used for x-height.</summary>
    /// <returns>The x-height metric source, or Unavailable.</returns>
    PdfFontDescriptorMetricSource GetXHeightSource();

    /// <summary>Gets the width used when a character code has no explicit width.</summary>
    /// <returns>The embedded .notdef advance in 1000-unit space.</returns>
    int GetMissingWidth();

    /// <summary>Gets the source used for MissingWidth.</summary>
    /// <returns>The .notdef advance source.</returns>
    PdfFontDescriptorMetricSource GetMissingWidthSource();

    /// <summary>Gets the estimated dominant vertical stem width.</summary>
    /// <returns>The PDF StemV estimate in 1000-unit space.</returns>
    int GetStemVertical();

    /// <summary>Gets the source used for the vertical-stem estimate.</summary>
    /// <returns>The stem-estimate metric source.</returns>
    PdfFontDescriptorMetricSource GetStemVerticalSource();

    /// <summary>Gets the FontDescriptor font-file entry kind.</summary>
    /// <returns>FontFile2 or FontFile3.</returns>
    PdfEmbeddedFontFileKind GetFontFileKind();

    /// <summary>Gets the PDF dictionary key for the embedded program.</summary>
    /// <returns><c>FontFile2</c> or <c>FontFile3</c>.</returns>
    string GetFontFileKey();

    /// <summary>Reports whether the font stream requires a Subtype entry.</summary>
    /// <returns>True for FontFile3.</returns>
    bool HasFontFileSubtype();

    /// <summary>Gets the FontFile3 stream subtype.</summary>
    /// <returns>The subtype, or null for FontFile2.</returns>
    string? GetFontFileSubtype();

    /// <summary>Gets the unfiltered embedded-program byte length.</summary>
    /// <returns>The byte length before any PDF stream filter is applied.</returns>
    int GetFontFileLength();

    /// <summary>Reports whether the stream dictionary requires Length1.</summary>
    /// <returns>True for a TrueType FontFile2 stream.</returns>
    bool HasLength1();

    /// <summary>Gets the decoded FontFile2 Length1 value.</summary>
    /// <returns>The unfiltered TrueType program length.</returns>
    /// <exception cref="System.InvalidOperationException">
    /// The program is stored in FontFile3.
    /// </exception>
    int GetLength1();
}
