namespace pdf;

internal class PdfEmbeddedTrueTypeFont
{
    internal PdfTrueTypeFontUsage Usage;
    internal string BaseFontName;
    internal byte[] FontProgram;
    internal int Type0ObjectId;
    internal int DescendantObjectId;
    internal int DescriptorObjectId;
    internal int FontFileObjectId;
    internal int ToUnicodeObjectId;

    internal PdfEmbeddedTrueTypeFont(PdfTrueTypeFontUsage usage)
    {
        Usage = usage;
        BaseFontName = PdfTrueTypeSubsetter.CreateEmbeddedFontName(usage.Font);
        FontProgram = new byte[0];
        Type0ObjectId = 0;
        DescendantObjectId = 0;
        DescriptorObjectId = 0;
        FontFileObjectId = 0;
        ToUnicodeObjectId = 0;
    }
}
