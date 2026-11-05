using System.Xml;

namespace pptx;

internal class PptxTextSpan
{
    internal XmlElement TextElement;
    internal string OriginalText;
    internal int Start;
    internal int Length;

    internal PptxTextSpan(XmlElement textElement, string originalText, int start)
    {
        if (textElement == null)
        {
            throw new System.ArgumentNullException("textElement");
        }

        TextElement = textElement;
        OriginalText = PptxDiagnostic.CleanText(originalText);
        Start = start;
        Length = OriginalText.Length;
    }
}
