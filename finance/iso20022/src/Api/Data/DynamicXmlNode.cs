using System.Collections.Generic;

namespace iso20022;

/// <summary>Represents one node in an ordered, prefix-aware generic XML tree.</summary>
public class DynamicXmlNode
{
    /// <summary>The node kind.</summary>
    public XmlNodeKind Kind;
    /// <summary>The qualified element or processing-instruction name.</summary>
    public QualifiedNameData Name = new QualifiedNameData();
    /// <summary>The text, CDATA, comment, or processing-instruction value.</summary>
    public string Value = string.Empty;
    /// <summary>The ordered attributes of an element.</summary>
    public List<XmlAttributeData> Attributes = new List<XmlAttributeData>();
    /// <summary>The namespace declarations made by an element.</summary>
    public List<NamespaceDeclarationData> NamespaceDeclarations = new List<NamespaceDeclarationData>();
    /// <summary>The ordered child nodes, including text and comments when preserved.</summary>
    public List<DynamicXmlNode> Children = new List<DynamicXmlNode>();
    /// <summary>Whether the element carries an effective <c>xsi:nil</c> value.</summary>
    public bool IsNil;
    /// <summary>Whether the source used empty-element syntax.</summary>
    public bool IsEmptyElement;
    /// <summary>The source location of the node.</summary>
    public SourceLocation Location = new SourceLocation();
}
