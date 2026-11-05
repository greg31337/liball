namespace fake;

/// <summary>Selects the shape of generated XML text.</summary>
public enum XmlTextKind
{
    /// <summary>Generates one XML element.</summary>
    Element = 0,

    /// <summary>Generates an XML document with a declaration and root element.</summary>
    Document = 1
}
