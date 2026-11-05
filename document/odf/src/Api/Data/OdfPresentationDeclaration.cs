namespace odf;

/// <summary>Describes reusable presentation header, footer, or date-and-time text.</summary>
public class OdfPresentationDeclaration
{
    /// <summary>The declaration kind.</summary>
    public OdfPresentationDeclarationKind Kind;
    /// <summary>The declaration name referenced by slides and masters.</summary>
    public string Name = "";
    /// <summary>The date-and-time source, such as <c>fixed</c> or <c>current-date</c>.</summary>
    public string Source = "";
    /// <summary>The data-style name used by a date-and-time declaration.</summary>
    public string DataStyleName = "";
    /// <summary>The preserved declaration text.</summary>
    public string Text = "";
    /// <summary>The underlying declaration element.</summary>
    public OdfNode? Node;
    internal string OriginalName = "";
    internal string OriginalSource = "";
    internal string OriginalDataStyleName = "";
    internal string OriginalText = "";
}
