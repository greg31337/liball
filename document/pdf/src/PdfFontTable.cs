namespace pdf;

internal class PdfFontTable
{
    internal string Tag;
    internal int Offset;
    internal int Length;

    internal PdfFontTable(string tag, int offset, int length)
    {
        Tag = tag;
        Offset = offset;
        Length = length;
    }
}
