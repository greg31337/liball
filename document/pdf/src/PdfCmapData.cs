namespace pdf;

internal class PdfCmapData
{
    internal int PlatformId;
    internal int EncodingId;
    internal int Format;
    internal int[] CodePoints;
    internal int[] GlyphIndices;

    internal PdfCmapData(int platformId, int encodingId, int format, int[] codePoints, int[] glyphIndices)
    {
        PlatformId = platformId;
        EncodingId = encodingId;
        Format = format;
        CodePoints = codePoints;
        GlyphIndices = glyphIndices;
    }
}
