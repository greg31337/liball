using System.Collections.Generic;

namespace iso20022;

/// <summary>Preserves an XML fragment together with the namespace closure needed to reuse it.</summary>
public class RawXmlFragment
{
    /// <summary>The fragment as XML text when available.</summary>
    public string Xml = string.Empty;
    /// <summary>The original UTF-8 bytes when captured from a byte input.</summary>
    public byte[] Utf8Bytes = [];
    /// <summary>The qualified root name when the fragment has one element root.</summary>
    public QualifiedNameData? RootName;
    /// <summary>The in-scope namespace declarations required by the fragment.</summary>
    public List<NamespaceDeclarationData> NamespaceDeclarations = new List<NamespaceDeclarationData>();
    /// <summary>The source location of the fragment.</summary>
    public SourceLocation Location = new SourceLocation();
}
