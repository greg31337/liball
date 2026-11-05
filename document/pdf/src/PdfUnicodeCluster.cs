namespace pdf;

internal class PdfUnicodeCluster
{
    internal string Text;
    internal PdfTrueTypeFont? Font;
    internal double FontSize;
    internal double BaselineShift;
    internal double Width;
    internal int Direction;
    internal int Level;
    internal bool BreakAfter;
    internal bool AddHyphenAtBreak;
    internal bool TrimAtLineEdge;
    internal bool MandatoryBreak;
    internal bool FootnoteMarker;

    internal PdfUnicodeCluster(string text)
    {
        Text = text;
        Font = null;
        FontSize = 0;
        BaselineShift = 0;
        Width = 0;
        Direction = 0;
        Level = 0;
        BreakAfter = false;
        AddHyphenAtBreak = false;
        TrimAtLineEdge = false;
        MandatoryBreak = false;
        FootnoteMarker = false;
    }
}
