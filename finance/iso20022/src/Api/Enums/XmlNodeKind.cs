namespace iso20022;

/// <summary>Identifies the kind of an ordered generic XML node.</summary>
public enum XmlNodeKind
{
    /// <summary>An XML element.</summary>
    Element = 0,
    /// <summary>A text node.</summary>
    Text = 1,
    /// <summary>A CDATA section.</summary>
    CData = 2,
    /// <summary>An XML comment.</summary>
    Comment = 3,
    /// <summary>An XML processing instruction.</summary>
    ProcessingInstruction = 4,
    /// <summary>The physical XML document or fragment container.</summary>
    Document = 5
}
