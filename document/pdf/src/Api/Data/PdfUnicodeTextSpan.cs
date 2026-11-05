namespace pdf;

/// <summary>
/// Contains one logical Unicode text span and its script and hyphenation metadata.
/// </summary>
public class PdfUnicodeTextSpan
{
    /// <summary>
    /// Stores the logical Unicode text.
    /// </summary>
    public string Text;
    /// <summary>
    /// Stores the script presentation applied to the span.
    /// </summary>
    public PdfTextScript Script;
    /// <summary>
    /// Indicates whether the span is a superscript footnote marker.
    /// </summary>
    public bool FootnoteMarker;
    /// <summary>
    /// Stores optional UTF-16 positions before which discretionary hyphen breaks are allowed.
    /// </summary>
    public int[] HyphenationPoints;

    /// <summary>
    /// Initializes a normal Unicode text span.
    /// </summary>
    /// <param name="text">The logical Unicode text.</param>
    public PdfUnicodeTextSpan(string text)
    {
        Text = text;
        Script = PdfTextScript.Normal;
        FootnoteMarker = false;
        HyphenationPoints = new int[0];
    }
}
