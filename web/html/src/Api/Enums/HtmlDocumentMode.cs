namespace html;
/// <summary>Identifies the standards, limited-quirks, or quirks mode selected from the document type.</summary>
public enum HtmlDocumentMode
{
    /// <summary>Uses standards document-mode behavior.</summary>
    NoQuirks = 0,
    /// <summary>Uses limited-quirks document-mode behavior.</summary>
    LimitedQuirks = 1,
    /// <summary>Uses legacy quirks document-mode behavior.</summary>
    Quirks = 2
}
