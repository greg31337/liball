namespace ttf;

internal class PdfToUnicodeRange
{
    PdfToUnicodeMapping[] mappings;

    internal PdfToUnicodeRange(PdfToUnicodeMapping[] mappings)
    {
        this.mappings = mappings;
    }

    internal int GetCount() { return mappings.Length; }

    internal PdfToUnicodeMapping GetMapping(int index)
    {
        return mappings[index];
    }
}
