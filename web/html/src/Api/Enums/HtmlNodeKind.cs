namespace html;
/// <summary>Identifies the kind of node stored in an HTML tree.</summary>
public enum HtmlNodeKind
{
    /// <summary>Identifies a complete document root.</summary>
    Document = 0,
    /// <summary>Identifies a detached document-fragment root.</summary>
    Fragment = 1,
    /// <summary>Identifies a document-type declaration.</summary>
    DocumentType = 2,
    /// <summary>Identifies an element node.</summary>
    Element = 3,
    /// <summary>Identifies a text node.</summary>
    Text = 4,
    /// <summary>Identifies a comment node.</summary>
    Comment = 5
}
