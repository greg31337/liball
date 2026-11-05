namespace svg;

/// <summary>Defines tagged XML node kinds retained by an SVG document.</summary>
public enum SvgNodeKind
{
    /// <summary>The synthetic document container.</summary>
    Document = 0,
    /// <summary>An XML element.</summary>
    Element = 1,
    /// <summary>Ordinary character data.</summary>
    Text = 2,
    /// <summary>A CDATA section.</summary>
    CData = 3,
    /// <summary>An XML comment.</summary>
    Comment = 4,
    /// <summary>An XML processing instruction.</summary>
    ProcessingInstruction = 5,
    /// <summary>A document type declaration.</summary>
    Doctype = 6
}
