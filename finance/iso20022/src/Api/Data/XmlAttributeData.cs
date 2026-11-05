namespace iso20022;

/// <summary>Represents one qualified XML attribute.</summary>
public class XmlAttributeData
{
    /// <summary>The qualified attribute name.</summary>
    public QualifiedNameData Name = new QualifiedNameData();
    /// <summary>The decoded attribute value.</summary>
    public string Value = string.Empty;
    /// <summary>Whether the attribute was explicitly present in the source.</summary>
    public bool IsSpecified;
    /// <summary>The source location of the attribute.</summary>
    public SourceLocation Location = new SourceLocation();
}
