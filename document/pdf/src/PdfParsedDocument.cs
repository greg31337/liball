using System.Collections.Generic;

namespace pdf;

internal class PdfParsedDocument
{
    internal string Text;
    internal int XrefOffset;
    internal int ObjectCount;
    internal int RootObject;
    internal int[] Offsets;
    internal string[] Bodies;
    internal List<int>[] References;

    internal PdfParsedDocument(string text)
    {
        Text = text;
        XrefOffset = 0;
        ObjectCount = 0;
        RootObject = 0;
        Offsets = new int[0];
        Bodies = new string[0];
        References = new List<int>[0];
    }
}
